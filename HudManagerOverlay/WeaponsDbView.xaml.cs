using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HudManagerOverlay;

public sealed class WeaponDetailDisplay
{
    public string Name { get; init; } = "";
    public int Size { get; init; }
    public string DamageText { get; init; } = "";
    public string FireRateText { get; init; } = "";
    public string DpsText { get; init; } = "";
    public double SortDps { get; init; }
}

public partial class WeaponsDbView : UserControl
{
    private readonly List<WeaponDetailDisplay> _all;

    public WeaponsDbView()
    {
        InitializeComponent();

        _all = WeaponDetailData.Load()
            .Select(w => new WeaponDetailDisplay
            {
                Name = w.Name,
                Size = w.Size,
                DamageText = w.Damage is double d ? $"{d:0.#}" : "—",
                FireRateText = w.FireRate is double f ? $"{f:0}" : "—",
                DpsText = w.Dps is double p ? $"{p:0.#}" : "—",
                SortDps = w.Dps ?? -1,
            })
            .OrderByDescending(w => w.SortDps)
            .ToList();

        WeaponList.ItemsSource = _all;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        WeaponList.ItemsSource = null;
        WeaponList.ItemsSource = q.Length == 0
            ? _all
            : _all.Where(w => w.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
