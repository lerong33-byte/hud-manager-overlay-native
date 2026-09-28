using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HudManagerOverlay;

// System tray presence — parity with the old build's tray menu (open/close, quit).
// Panel/tool entries get added here in later phases as they're ported.
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

    public event Action? ToolRailRequested;
    public event Action? QuitRequested;
    public event Action? LoadoutBayRequested;
    public event Action? MissionsRequested;
    public event Action? BlueprintsRequested;
    public event Action? WeaponsDbRequested;
    public event Action? MaterialsRequested;
    public event Action? AcquisitionRequested;

    public TrayIcon()
    {
        var menu = new ContextMenuStrip();
        var railItem = new ToolStripMenuItem("Open Tool Rail (Right Alt)");
        railItem.Click += (_, _) => ToolRailRequested?.Invoke();
        var loadoutItem = new ToolStripMenuItem("Ship Loadout Bay");
        loadoutItem.Click += (_, _) => LoadoutBayRequested?.Invoke();
        var missionsItem = new ToolStripMenuItem("Missions");
        missionsItem.Click += (_, _) => MissionsRequested?.Invoke();
        var blueprintsItem = new ToolStripMenuItem("Blueprints");
        blueprintsItem.Click += (_, _) => BlueprintsRequested?.Invoke();
        var weaponsDbItem = new ToolStripMenuItem("Weapons DB");
        weaponsDbItem.Click += (_, _) => WeaponsDbRequested?.Invoke();
        var materialsItem = new ToolStripMenuItem("Materials");
        materialsItem.Click += (_, _) => MaterialsRequested?.Invoke();
        var acquisitionItem = new ToolStripMenuItem("Acquisition");
        acquisitionItem.Click += (_, _) => AcquisitionRequested?.Invoke();
        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => QuitRequested?.Invoke();
        menu.Items.Add(railItem);
        menu.Items.Add(loadoutItem);
        menu.Items.Add(missionsItem);
        menu.Items.Add(blueprintsItem);
        menu.Items.Add(weaponsDbItem);
        menu.Items.Add(materialsItem);
        menu.Items.Add(acquisitionItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quitItem);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        _icon = new NotifyIcon
        {
            Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application,
            Text = "H.U.D Manager Overlay",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => ToolRailRequested?.Invoke();
    }

    public void Dispose() => _icon.Dispose();
}
