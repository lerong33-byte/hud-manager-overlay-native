using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HudManagerOverlay;

public partial class LoadoutBayView : UserControl
{
    private readonly List<Ship> _allShips;

    public LoadoutBayView()
    {
        InitializeComponent();
        _allShips = ShipData.Load();
        ShipList.ItemsSource = _allShips;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        ShipList.ItemsSource = null;
        ShipList.ItemsSource = q.Length == 0
            ? _allShips
            : _allShips.Where(s =>
                s.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase) ||
                s.Manufacturer.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
        NativeInterop.ForceRedraw(this);
    }

    private void ShipList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShipList.SelectedItem is Ship ship) Detail.ShowShip(ship);
    }
}
