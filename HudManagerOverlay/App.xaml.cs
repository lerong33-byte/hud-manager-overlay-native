using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;
using Velopack;

namespace HudManagerOverlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;

    public App()
    {
        // Must run before anything else in the process — including the single-instance mutex
        // check below. The Velopack-built installer/updater relaunches this exe with special
        // hidden args (e.g. after installing or updating) purely to run lifecycle hooks (create
        // shortcuts, register the uninstaller, etc.) and then exit immediately; it must never
        // reach the mutex check, the tray icon, or any real UI. VelopackApp.Run() detects those
        // args, handles them, and exits on its own — for a normal launch it's a no-op and control
        // falls through to OnStartup as usual.
        VelopackApp.Build().Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, "HudManagerOverlay.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("H.U.D Manager Overlay is already running.", "H.U.D Manager",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}

