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

    private readonly DispatcherTimer _poll = new() { Interval = TimeSpan.FromMilliseconds(50) };
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

        _poll.Tick += Poll_Tick;
        _poll.Start();
    }

    public PanelChrome AddPanel(string title, UIElement content, double x, double y, double w, double h)
    {
        var chrome = new PanelChrome { Title = title, PanelContent = content, Width = w, Height = h };
        chrome.CloseRequested += () => PanelCanvas.Children.Remove(chrome);
        Canvas.SetLeft(chrome, x);
        Canvas.SetTop(chrome, y);
        PanelCanvas.Children.Add(chrome);
        if (!IsVisible) Show();
        return chrome;
    }

    // For fixed, non-draggable chrome like the hotbar — still hit-tested for click-through the
    // same way, just not wrapped in PanelChrome's title bar/drag/resize/close.
    public void AddFixed(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        PanelCanvas.Children.Add(element);
        if (!IsVisible) Show();
    }

    public void RemoveElement(UIElement element) => PanelCanvas.Children.Remove(element);

    private void Poll_Tick(object? sender, EventArgs e)
    {
        if (PanelCanvas.Children.Count == 0) { Hide(); return; }

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

        // Second hysteresis layer (time): require 2 consecutive polls (100ms) before committing
        // the flip — also matched to the validated fix, so a single stray sample can't toggle it.
        var wantInteractive = nearAny;
        if (wantInteractive == _interactive) { _ignoreStreak = 0; return; }
        if (++_ignoreStreak < 2) return;
        _ignoreStreak = 0;
        _interactive = wantInteractive;
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeInterop.SetClickThrough(hwnd, clickThrough: !_interactive);
    }
}
