using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace HudManagerOverlay;

public partial class MainWindow : Window
{
    private const ushort VK_RCONTROL = 0xA3;
    private const ushort VK_RMENU = 0xA5; // Right Alt — tool-rail toggle
    private const ushort VK_1 = 0x31; // 1..9 are contiguous
    private const ushort VK_Y = 0x59; // Star Citizen's default exit-seat key (a HOLD, not a tap)
    private const int ExitSeatHoldMs = 450; // matches main.js _EXIT_HOLD_MS exactly
    private bool _railKeyDown; // guards against auto-repeat re-toggling while Right Alt is held

    private TrayIcon? _tray;
    private PanelHost? _panelHost;
    private KeyboardHook? _keyHook;
    private HotbarView? _hotbar;
    private bool _hotbarArmed;
    private bool _hotbarShown;
    private ToolRailView? _toolRail;
    private readonly DispatcherTimer _scWatch = new() { Interval = TimeSpan.FromSeconds(2) };
    private AppSettings _settings = AppSettings.Load();

    // Seat vs foot deck context. Entering a seat is detected via Game.log (GameLogWatcher);
    // leaving one has no log line, so it's detected the same way the old app did it — a HOLD of
    // the exit-seat key (default Y) past a short threshold while currently in the seat context.
    private string _hotbarCtx = "foot";
    private readonly GameLogWatcher _gameLog = new();
    private bool _exitSeatDown;
    private DispatcherTimer? _exitSeatHoldTimer;

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

        _tray = new TrayIcon();
        _tray.ToolRailRequested += () => Dispatcher.Invoke(ToggleToolRail);
        _tray.QuitRequested += () => Dispatcher.Invoke(Close);
        _tray.LoadoutBayRequested += () => Dispatcher.Invoke(OpenLoadoutBay);
        _tray.MissionsRequested += () => Dispatcher.Invoke(OpenMissions);

        _keyHook = new KeyboardHook();
        _keyHook.KeyDown += OnGlobalKeyDown;
        _keyHook.KeyUp += OnGlobalKeyUp;
        _keyHook.Install();

        // Hotbar only shows while SC is running — matches the old app's game-gated behavior,
        // and means it's never visible (or interceptable) outside an actual play session.
        _scWatch.Tick += (_, _) => Dispatcher.Invoke(UpdateHotbarVisibility);
        _scWatch.Start();

        _gameLog.SeatDetected += () => Dispatcher.Invoke(() => SetHotbarContext("seat"));
        _gameLog.Start();
    }

    private void SetHotbarContext(string ctx)
    {
        if (ctx == _hotbarCtx) return;
        _hotbarCtx = ctx;
        _hotbar?.SetDeck(ctx == "seat" ? HotbarView.SeatDeck : HotbarView.FootDeck);
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
            _hotbar.SetDeck(_hotbarCtx == "seat" ? HotbarView.SeatDeck : HotbarView.FootDeck);
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
        if (vk == VK_RMENU)
        {
            // RegisterHotKey is unreliable for a standalone modifier key like Right Alt (a known
            // Windows API limitation — confirmed live: simulated presses never fired WM_HOTKEY).
            // The same low-level hook used for the hotbar/exit-seat detection works correctly
            // here instead. Guard against auto-repeat re-toggling while the key is held.
            if (!_railKeyDown) { _railKeyDown = true; Dispatcher.Invoke(ToggleToolRail); }
            return;
        }
        if (vk == VK_Y)
        {
            if (!_exitSeatDown && _hotbarCtx == "seat")
            {
                _exitSeatDown = true;
                Dispatcher.Invoke(() =>
                {
                    _exitSeatHoldTimer?.Stop();
                    _exitSeatHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ExitSeatHoldMs) };
                    _exitSeatHoldTimer.Tick += (_, _) =>
                    {
                        _exitSeatHoldTimer!.Stop();
                        if (_exitSeatDown) SetHotbarContext("foot");
                    };
                    _exitSeatHoldTimer.Start();
                });
            }
            return; // observational only — never swallow; SC needs the real hold to exit the seat
        }
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
        if (vk == VK_RMENU) { _railKeyDown = false; return; }
        if (vk == VK_Y)
        {
            _exitSeatDown = false;
            Dispatcher.Invoke(() => _exitSeatHoldTimer?.Stop());
            return;
        }
        if (vk == VK_RCONTROL)
        {
            _hotbarArmed = false;
            Dispatcher.Invoke(() => _hotbar?.SetArmed(false));
        }
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
        _toolRail.AddTool("MISSIONS", OpenMissions);
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
            : (200, 150, 640, 480);
        var chrome = _panelHost.AddPanel("SHIP LOADOUT BAY", new LoadoutBayView(), x, y, w, h);
        chrome.CloseRequested += () => _settings.Panels["loadout-bay"] = new PanelState
        {
            X = System.Windows.Controls.Canvas.GetLeft(chrome),
            Y = System.Windows.Controls.Canvas.GetTop(chrome),
            W = chrome.ActualWidth, H = chrome.ActualHeight,
        };
    }

    private void OpenMissions()
    {
        _panelHost ??= new PanelHost();
        var saved = _settings.Panels.GetValueOrDefault("missions");
        var (x, y, w, h) = saved != null
            ? (saved.X, saved.Y, saved.W, saved.H)
            : (200, 150, 680, 480);
        var chrome = _panelHost.AddPanel("MISSIONS", new MissionsView(), x, y, w, h);
        chrome.CloseRequested += () => _settings.Panels["missions"] = new PanelState
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
