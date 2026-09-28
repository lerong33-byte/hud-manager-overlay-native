using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace HudManagerOverlay;

// ONE window holds every open panel — the direct fix for tonight's root cause (separate windows
// fighting the game for GPU present slots). Every panel is a PanelChrome placed on a Canvas
// inside this single window, not its own OS window.
public partial class PanelHost : Window
{
    // Spatial hysteresis, ported from the exact fix validated tonight (overlay-src main.js
    // HOST_EDGE_MARGIN): once interactive, the cursor must clear a panel by this many DIPs
    // before we hand control back to the game. Without it, a dropdown/list that visually
    // overflows its panel's bounds flips click-through mid-interaction and flickers the window.
    // Margin is 0 while click-through so idle game clicks are never stolen.
    private const double EdgeMarginDip = 40;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT p);

    // A timer poll, no matter how fast, checks cursor position on a fixed schedule that a real
    // click can outrun: a WM_LBUTTONDOWN delivered while the window is still WS_EX_TRANSPARENT
    // passes straight through and is gone, unrecoverably, before the next tick ever fires. Even at
    // 8ms this still intermittently ate real clicks (2026-09-28 bug report: list selection did
    // nothing, reproduced across every panel). Fixed by driving the hit-test off the actual
    // WM_MOUSEMOVE stream via a low-level mouse hook instead — that necessarily fires before any
    // click on the same spot, synchronously, closing the gap a poll can never fully close. The
    // timer stays as a slow safety net only (covers e.g. a panel appearing/moving under an
    // already-stationary cursor, which generates no mouse-move to react to).
    private readonly DispatcherTimer _pollFallback = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly MouseHook _mouseHook = new();
    private bool _interactive;
    private int _ignoreStreak;

    public PanelHost()
    {
        InitializeComponent();
        Left = 0; Top = 0; Width = 800; Height = 600; // corrected below once DPI-aware

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeInterop.MakeLayeredOverlay(hwnd);
            NativeInterop.SetClickThrough(hwnd, clickThrough: true);
        };

        // DPI reports unreliably (stale 1.0 scale) if queried before Loaded — see MainWindow's
        // matching comment; confirmed live on a 125%-scaled display.
        Loaded += (_, _) =>
        {
            var (dipW, dipH) = ScreenHelper.GetPrimaryScreenDips(this);
            Left = 0; Top = 0; Width = dipW; Height = dipH;
        };

        // Shown once, immediately, and never hidden again — see the note on RemoveElement for why.
        Show();

        _pollFallback.Tick += (_, _) => CheckCursor();
        _pollFallback.Start();
        _mouseHook.Move += CheckCursor;
        _mouseHook.Install();

        Closed += (_, _) => _mouseHook.Dispose();
    }

    public PanelChrome AddPanel(string title, UIElement content, double x, double y, double w, double h)
    {
        var chrome = new PanelChrome { Title = title, PanelContent = content, Width = w, Height = h };
        chrome.CloseRequested += () => RemoveElement(chrome);
        Canvas.SetLeft(chrome, x);
        Canvas.SetTop(chrome, y);
        PanelCanvas.Children.Add(chrome);
        return chrome;
    }

    private void ResetInteractiveState()
    {
        _interactive = false;
        _ignoreStreak = 0;
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeInterop.SetClickThrough(hwnd, clickThrough: true);
    }

    // For fixed, non-draggable chrome like the hotbar — still hit-tested for click-through the
    // same way, just not wrapped in PanelChrome's title bar/drag/resize/close.
    public void AddFixed(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        PanelCanvas.Children.Add(element);
    }

    // Used to Hide() this window whenever the last panel closed, then Show() it again on the next
    // AddPanel/AddFixed. That hide/show cycle on a layered (WS_EX_LAYERED) window turned out to be
    // the real bug: confirmed 2026-09-28 with a visible corrupted-paint artifact (a stray solid
    // color block) appearing after a close+reopen, on top of interactive state going stale. Never
    // hiding the window at all sidesteps the whole class of problem — an empty Canvas already
    // paints nothing and (once reset below) is fully click-through, which looks and behaves
    // identically to a hidden window for anything the user can see or click.
    public void RemoveElement(UIElement element)
    {
        PanelCanvas.Children.Remove(element);
        if (PanelCanvas.Children.Count == 0) ResetInteractiveState();
    }

    private void CheckCursor()
    {
        if (PanelCanvas.Children.Count == 0) return;

        // NOT System.Windows.Forms.Cursor.Position — confirmed live in a hybrid WPF+WinForms app
        // (this one, for the tray icon) that it reports coordinates scaled ~1.25x relative to
        // true physical pixels, while WPF's own PointToScreen (used below) reports true physical
        // pixels. Comparing the two directly meant this hit-test NEVER matched, at any position,
        // even with exact bounds confirmed. Raw GetCursorPos matches PointToScreen's space.
        GetCursorPos(out var cursor);
        var pos = new System.Drawing.Point(cursor.X, cursor.Y);
        var margin = _interactive ? EdgeMarginDip : 0;
        bool nearAny = false;

        foreach (UIElement child in PanelCanvas.Children)
        {
            if (child is not FrameworkElement fe) continue;
            var topLeft = fe.PointToScreen(new Point(0, 0));
            var bottomRight = fe.PointToScreen(new Point(fe.ActualWidth, fe.ActualHeight));
            if (pos.X >= topLeft.X - margin && pos.X <= bottomRight.X + margin &&
                pos.Y >= topLeft.Y - margin && pos.Y <= bottomRight.Y + margin)
            {
                nearAny = true;
                break;
            }
        }

        // Time hysteresis only guards the EXIT direction (interactive -> click-through), matching
        // the validated overlay-src fix: that's what prevents flicker from jitter at a panel edge.
        // Entering must commit on the very first sample that finds the cursor over a panel — any
        // delay there is a race between "cursor arrives" and "user clicks", and a real click
        // reliably loses that race (panel silently swallows nothing, click passes through to
        // whatever's behind it). Bug reported 2026-09-28: clicking any list item did nothing.
        var wantInteractive = nearAny;
        if (wantInteractive == _interactive) { _ignoreStreak = 0; return; }
        if (wantInteractive)
        {
            _ignoreStreak = 0;
        }
        else if (++_ignoreStreak < 2)
        {
            return;
        }
        else
        {
            _ignoreStreak = 0;
        }
        _interactive = wantInteractive;
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeInterop.SetClickThrough(hwnd, clickThrough: !_interactive);
    }
}
