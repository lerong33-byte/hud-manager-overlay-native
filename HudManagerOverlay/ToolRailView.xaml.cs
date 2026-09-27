using System;
using System.Windows.Controls;

namespace HudManagerOverlay;

public partial class ToolRailView : UserControl
{
    public event Action? CloseRequested;

    public ToolRailView()
    {
        InitializeComponent();
    }

    // Each tool is a label + an action to run when clicked. The rail doesn't know or care what
    // "SHIP LOADOUT BAY" does — MainWindow wires that up — so adding the next tool later is just
    // one more AddTool call, not a change to this control.
    public void AddTool(string label, Action onOpen)
    {
        var btn = new Button { Content = label, Style = (System.Windows.Style)FindResource("RailButton"), Margin = new System.Windows.Thickness(0, 2, 0, 2) };
        btn.Click += (_, _) =>
        {
            onOpen();
            CloseRequested?.Invoke(); // matches old app: a selection closes the rail
        };
        ToolList.Children.Add(btn);
    }
}
