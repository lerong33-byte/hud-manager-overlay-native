using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HudManagerOverlay;

// Flattened display shapes for the XAML ItemsControls — keeps presentation logic (gimbal/fixed
// label, "2x Ignite II", the swap dropdown's options) off the data model itself.
// Class, not record: SelectedName needs to be mutable for the ComboBox's two-way binding, and
// this is display-only state — it doesn't feed back into ShipData or any save file (that needs
// an actual game-facing loadout system, out of scope for this pass; see the project's known-gaps
// notes on the full loadout editor).
public sealed class WeaponSlotDisplay
{
    public string Label { get; init; } = "";
    public int Size { get; init; }
    public string MountKind { get; init; } = "";
    public List<string> Options { get; init; } = new();
    public string SelectedName { get; set; } = "";
}

public sealed record MissileSlotDisplay(string Label, string Summary);

public partial class ShipDetailView : UserControl
{
    private string _currentShipName = "";

    public ShipDetailView()
    {
        InitializeComponent();
    }

    public void ShowShip(Ship ship)
    {
        NoSelectionText.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;
        _currentShipName = ship.Name;

        ShipNameText.Text = ship.Name;
        ShipSubText.Text = $"{ship.Manufacturer} · {ship.Role} · {ship.Size}";

        ScmText.Text = Fmt(ship.Scm, " m/s");
        BoostText.Text = Fmt(ship.Boost, " m/s");
        NavText.Text = Fmt(ship.Nav, " m/s");

        HpText.Text = Fmt(ship.Hp);
        MassText.Text = Fmt(ship.Mass, " kg");
        CargoText.Text = ship.Cargo is > 0 ? Fmt(ship.Cargo, " SCU") : "—";

        CrewText.Text = ship.Crew != null ? $"{ship.Crew.Min}-{ship.Crew.Max}" : "—";

        var hasShield = ship.Shield != null && (ship.Shield.Hp is > 0);
        ShieldHeader.Visibility = ShieldGrid.Visibility = hasShield ? Visibility.Visible : Visibility.Collapsed;
        if (hasShield)
        {
            ShieldHpText.Text = Fmt(ship.Shield!.Hp);
            ShieldRegenText.Text = Fmt(ship.Shield.Regen, "/s");
            ShieldFaceText.Text = ship.Shield.Face ?? "—";
        }

        var hasWeapons = ship.Weaponry != null && (ship.Weaponry.PilotDps is > 0);
        WeaponryHeader.Visibility = WeaponryGrid.Visibility = hasWeapons ? Visibility.Visible : Visibility.Collapsed;
        if (hasWeapons)
        {
            DpsText.Text = Fmt(ship.Weaponry!.PilotDps);
            AlphaText.Text = Fmt(ship.Weaponry.PilotAlpha);
        }

        var hasMissiles = ship.Weaponry != null && (ship.Weaponry.MissileCount is > 0);
        MissileGrid.Visibility = hasMissiles ? Visibility.Visible : Visibility.Collapsed;
        if (hasMissiles)
        {
            MissileDmgText.Text = Fmt(ship.Weaponry!.MissileDmg);
            MissileCountText.Text = Fmt(ship.Weaponry.MissileCount);
        }

        var saved = LoadoutStore.Get(ship.Name);
        var guns = ship.Slots.Where(s => s.Kind == "gun").Select(s =>
        {
            var options = WeaponCatalog.NamesForSize(s.Size);
            var stock = s.DefaultName ?? "Empty";
            if (!options.Contains(stock)) options.Insert(0, stock); // keep the ship's stock weapon selectable even if it's not in the trimmed catalog
            // A previously saved choice for this exact slot wins over stock, but only if it's
            // still a valid option for this mount size (catalog/ship data may have changed).
            var current = saved.WeaponBySlotLabel.TryGetValue(s.Label, out var chosen) && options.Contains(chosen)
                ? chosen : stock;
            return new WeaponSlotDisplay
            {
                Label = s.Label, Size = s.Size,
                MountKind = s.Fixed ? " Fixed" : s.Gimbal ? " Gimbal" : "",
                Options = options, SelectedName = current,
            };
        }).ToList();
        WeaponSlotList.ItemsSource = guns;
        LoadoutHeader.Visibility = guns.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        var racks = ship.Missiles.Select(m => new MissileSlotDisplay(
            m.Label, m.Count > 0 ? $"{m.Count}x {m.DefaultName ?? "Empty"}" : "Empty")).ToList();
        MissileSlotList.ItemsSource = racks;
        MissileSlotHeader.Visibility = racks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string Fmt(double? v, string suffix = "") =>
        v.HasValue ? $"{v.Value:N0}{suffix}" : "—";

    // Fires on both a real user pick AND the initial binding setting SelectedName from ShowShip
    // — harmless either way, since re-saving the same value that's already saved is a no-op in
    // effect. Reads the slot straight off the ComboBox's own DataContext rather than tracking
    // index/sender lookups, since the ItemsControl already gives each row its own bound instance.
    private void WeaponCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: WeaponSlotDisplay slot }) return;
        if (string.IsNullOrEmpty(_currentShipName) || string.IsNullOrEmpty(slot.SelectedName)) return;
        LoadoutStore.SetWeapon(_currentShipName, slot.Label, slot.SelectedName);
    }
}
