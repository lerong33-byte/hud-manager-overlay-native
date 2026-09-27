using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;

namespace HudManagerOverlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;

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

