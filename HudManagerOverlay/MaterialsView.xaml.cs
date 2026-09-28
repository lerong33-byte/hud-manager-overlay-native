using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HudManagerOverlay;

public sealed class MaterialDisplay
{
    public string Name { get; init; } = "";
    public string Tier { get; init; } = "";
    public string RsText { get; init; } = "";
    public string MineableText { get; init; } = "";
}

public partial class MaterialsView : UserControl
{
    private readonly List<MaterialDisplay> _all;

    public MaterialsView()
    {
        InitializeComponent();

        _all = MaterialData.Load()
            .OrderByDescending(m => m.Rs ?? -1)
            .Select(m => new MaterialDisplay
            {
                Name = m.Name ?? "Unknown",
                Tier = m.Tier ?? "—",
                RsText = m.Rs is int r ? $"{r}" : "—",
                MineableText = m.Mineable ? "Yes" : "No",
            })
            .ToList();

        MaterialList.ItemsSource = _all;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        MaterialList.ItemsSource = null;
        MaterialList.ItemsSource = q.Length == 0
            ? _all
            : _all.Where(m => m.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
