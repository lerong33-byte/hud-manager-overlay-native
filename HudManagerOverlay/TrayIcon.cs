using System;
using System.Drawing;
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

    public TrayIcon()
    {
        var menu = new ContextMenuStrip();
        var railItem = new ToolStripMenuItem("Open Tool Rail (Right Alt)");
        railItem.Click += (_, _) => ToolRailRequested?.Invoke();
        var loadoutItem = new ToolStripMenuItem("Ship Loadout Bay");
        loadoutItem.Click += (_, _) => LoadoutBayRequested?.Invoke();
        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => QuitRequested?.Invoke();
        menu.Items.Add(railItem);
        menu.Items.Add(loadoutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quitItem);

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application, // placeholder — swap for the real brand icon later
            Text = "H.U.D Manager Overlay",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => ToolRailRequested?.Invoke();
    }

    public void Dispose() => _icon.Dispose();
}
