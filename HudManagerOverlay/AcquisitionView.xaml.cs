using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HudManagerOverlay;

public sealed class AcquisitionDisplay
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string LocationsText { get; init; } = "";
}

public partial class AcquisitionView : UserControl
{
    private readonly List<AcquisitionDisplay> _all;

    public AcquisitionView()
    {
        InitializeComponent();

        _all = AcquisitionData.Load()
            .Select(kv => new AcquisitionDisplay
            {
                Name = kv.Key,
                Type = kv.Value.Type ?? "—",
                LocationsText = kv.Value.Locations.Count > 0
                    ? string.Join(" · ", kv.Value.Locations)
                    : "No known location",
            })
            .OrderBy(a => a.Name)
            .ToList();

        ItemList.ItemsSource = _all;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        ItemList.ItemsSource = q.Length == 0
            ? _all
            : _all.Where(a => a.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
