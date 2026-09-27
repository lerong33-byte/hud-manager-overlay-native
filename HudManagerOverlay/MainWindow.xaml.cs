using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace HudManagerOverlay;

public partial class MainWindow : Window
{
    private const int HotkeyId = 1;
    private const ushort VK_RCONTROL = 0xA3;
    private const ushort VK_1 = 0x31; // 1..9 are contiguous

    private TrayIcon? _tray;
    private PanelHost? _panelHost;
    private KeyboardHook? _keyHook;
    private HotbarView? _hotbar;
    private bool _hotbarArmed;
    private bool _hotbarShown;
    private ToolRailView? _toolRail;
    private readonly DispatcherTimer _scWatch = new() { Interval = TimeSpan.FromSeconds(2) };
    private AppSettings _settings = AppSettings.Load();

    public MainWindow()
    {
        InitializeComponent();

        // Placeholder size — corrected to the real DPI-adjusted screen size in OnSourceInitialized
        // (SystemParameters is unreliable on a per-monitor-DPI-aware app; see ScreenHelper).
        Left = 0; Top = 0; Width = 800; Height = 600;

        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) =>
        {
            _tray?.Dispose();
            _keyHook?.Dispose();
            _settings.Save();
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeInterop.MakeLayeredOverlay(hwnd);

        // DPI reports unreliably (falls back to 1.0 scale) if queried this early, before Windows
        // finishes per-monitor DPI negotiation for a brand-new top-level window — confirmed live
        // (same query correctly returned 1.25 once queried after Loaded). Corrected size is
        // applied in Loaded instead, once DPI has actually settled.
        Loaded += (_, _) =>
        {
            var (dipW, dipH) = ScreenHelper.GetPrimaryScreenDips(this);
            Left = 0; Top = 0; Width = dipW; Height = dipH;
        };

        SetInteractive(false); // click-through by default — game gets every click until toggled

        NativeInterop.RegisterHotKey(hwnd, HotkeyId, 0, NativeInterop.VK_RMENU);
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        _tray = new TrayIcon();
        _tray.ToolRailRequested += () => Dispatcher.Invoke(ToggleToolRail);
        _tray.QuitRequested += () => Dispatcher.Invoke(Close);
        _tray.LoadoutBayRequested += () => Dispatcher.Invoke(OpenLoadoutBay);

        _keyHook = new KeyboardHook();
        _keyHook.KeyDown += OnGlobalKeyDown;
        _keyHook.KeyUp += OnGlobalKeyUp;
        _keyHook.Install();

        // Hotbar only shows while SC is running — matches the old app's game-gated behavior,
        // and means it's never visible (or interceptable) outside an actual play session.
        _scWatch.Tick += (_, _) => Dispatcher.Invoke(UpdateHotbarVisibility);
        _scWatch.Start();
    }

    private void UpdateHotbarVisibility()
    {
        var wantShown = GameDetection.IsRunning();
        if (wantShown == _hotbarShown) return;
        _hotbarShown = wantShown;
        _panelHost ??= new PanelHost();
        if (!_panelHost.IsVisible) _panelHost.Show(); // settles its DPI before we query it below

        if (wantShown)
        {
            _hotbar = new HotbarView();
            // Query DPI through PanelHost, not MainWindow — MainWindow is created at app startup
            // before per-monitor DPI negotiation settles and gets permanently stuck reporting
            // scale 1.0, while windows created later (like PanelHost) correctly report the real
            // scale. Confirmed live: MainWindow read 1.0 forever; PanelHost read 1.25 correctly.
            var (screenW, screenH) = ScreenHelper.GetPrimaryScreenDips(_panelHost);
            _panelHost.AddFixed(_hotbar, x: screenW / 2 - 270, y: screenH - 90);
        }
        else if (_hotbar != null)
        {
            _panelHost.RemoveElement(_hotbar);
            _hotbar = null;
            _hotbarArmed = false;
        }
    }

    private void OnGlobalKeyDown(int vk)
    {
        if (vk == VK_RCONTROL)
        {
            _hotbarArmed = true;
            Dispatcher.Invoke(() => _hotbar?.SetArmed(true));
            return;
        }
        if (_hotbarArmed && vk >= VK_1 && vk < VK_1 + 9)
        {
            var index = vk - VK_1;
            Dispatcher.Invoke(() =>
            {
                _hotbar?.FlashSlot(index);
                var slot = _hotbar?.GetSlot(index);
                // Only ever fires into Star Citizen itself, never wherever else focus happens to
                // be — the whole point of gating on foreground, not just "hotbar armed."
                if (slot != null && GameDetection.IsForeground()) InputSender.TapKey(slot.Vk, slot.Modifier);
            });
        }
    }

    private void OnGlobalKeyUp(int vk)
    {
        if (vk == VK_RCONTROL)
        {
            _hotbarArmed = false;
            Dispatcher.Invoke(() => _hotbar?.SetArmed(false));
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeInterop.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            ToggleToolRail();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleToolRail()
    {
        _panelHost ??= new PanelHost();
        if (!_panelHost.IsVisible) _panelHost.Show(); // settles DPI before the query below

        if (_toolRail != null)
        {
            _panelHost.RemoveElement(_toolRail);
            _toolRail = null;
            return;
        }

        _toolRail = new ToolRailView();
        _toolRail.AddTool("SHIP LOADOUT BAY", OpenLoadoutBay);
        _toolRail.CloseRequested += () =>
        {
            if (_toolRail == null) return;
            _panelHost.RemoveElement(_toolRail);
            _toolRail = null;
        };
        var (screenW, screenH) = ScreenHelper.GetPrimaryScreenDips(_panelHost);
        _panelHost.AddFixed(_toolRail, x: screenW / 2 - 110, y: screenH / 2 - 100);
    }

    private void OpenLoadoutBay()
    {
        _panelHost ??= new PanelHost();
        var saved = _settings.Panels.GetValueOrDefault("loadout-bay");
        var (x, y, w, h) = saved != null
            ? (saved.X, saved.Y, saved.W, saved.H)
            : (200, 150, 380, 480);
        var chrome = _panelHost.AddPanel("SHIP LOADOUT BAY", new LoadoutBayView(), x, y, w, h);
        chrome.CloseRequested += () => _settings.Panels["loadout-bay"] = new PanelState
        {
            X = System.Windows.Controls.Canvas.GetLeft(chrome),
            Y = System.Windows.Controls.Canvas.GetTop(chrome),
            W = chrome.ActualWidth, H = chrome.ActualHeight,
        };
    }

    // MainWindow itself is just a background click-through layer for the status chip — it's
    // never interactive. Right Alt now opens the tool rail (see ToggleToolRail), not this window.
    private void SetInteractive(bool interactive)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeInterop.SetClickThrough(hwnd, clickThrough: !interactive);
        StatusText.Text = "H.U.D Manager (native) — running — Right Alt for tools";
    }
}
