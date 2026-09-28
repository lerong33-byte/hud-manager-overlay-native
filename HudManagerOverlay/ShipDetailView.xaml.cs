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

public sealed class MissileSlotDisplay
{
    public string Label { get; init; } = "";
    public List<string> Options { get; init; } = new();
    public string SelectedName { get; set; } = "";
}

public sealed class ComponentSlotDisplay
{
    public string Label { get; init; } = "";
    public string Key { get; init; } = ""; // "{kind}-{index}", e.g. "sg-1" — see ShipLoadoutChoice.ComponentByKey
    public List<string> Options { get; init; } = new();
    public string SelectedName { get; set; } = "";
}

// Radar / life support: no compatibility catalog exists for either (no erkul source data, and
// both are rarely swapped in practice) — shown as plain read-only rows instead of a picker.
public sealed class ReadOnlyPartDisplay
{
    public string Label { get; init; } = "";
    public string PartName { get; init; } = "";
}

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
        var guns = new List<WeaponSlotDisplay>();
        foreach (var s in ship.Slots)
        {
            if (s.Kind == "gun")
            {
                guns.Add(BuildGunRow(s.Label, s.Size, s.Gimbal, s.Fixed, s.DefaultName, saved));
            }
            else if ((s.Kind == "turret" || s.Kind == "manned-turret") && s.Children is { Count: > 0 } children)
            {
                // A turret can exist in more than one identical instance (Count) and each instance
                // has several independently-mounted guns (Children) — flatten both into individual
                // rows so every physical gun gets its own swap dropdown, same as a plain mount.
                var instances = Math.Max(1, s.Count);
                for (int inst = 0; inst < instances; inst++)
                {
                    var instanceLabel = instances > 1 ? $"{s.Label} #{inst + 1}" : s.Label;
                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        var childLabel = children.Count > 1 ? $"{instanceLabel} - Gun {i + 1}" : instanceLabel;
                        guns.Add(BuildGunRow(childLabel, child.Size, child.Gimbal, !child.Gimbal, child.DefaultName, saved));
                    }
                }
            }
        }
        WeaponSlotList.ItemsSource = null;
        WeaponSlotList.ItemsSource = guns;
        LoadoutHeader.Visibility = guns.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        // First ItemsControl populated in this method to use ComboBox rows — flushing here, not
        // just once at the very end, in case whatever intermittently leaves early-created rows
        // unpainted (2026-09-28: weapon dropdown rows sometimes render as default-white/unstyled
        // while later-created missile/component rows in the same call are fine) is specific to
        // being first rather than being a weapon row.
        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

        var racks = ship.Missiles.Where(m => m.Count > 0).Select(m =>
        {
            var options = MissileCatalog.NamesForSize(m.HoldsSize);
            var stock = m.DefaultName ?? "Empty";
            if (!options.Contains(stock)) options.Insert(0, stock);
            var current = saved.MissileBySlotLabel.TryGetValue(m.Label, out var chosen) && options.Contains(chosen)
                ? chosen : stock;
            return new MissileSlotDisplay { Label = $"{m.Label} · {m.Count}x", Options = options, SelectedName = current };
        }).ToList();
        MissileSlotList.ItemsSource = null;
        MissileSlotList.ItemsSource = racks;
        MissileSlotHeader.Visibility = racks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        var components = new List<ComponentSlotDisplay>();
        if (ship.Comps != null)
        {
            components.AddRange(BuildComponentGroup("Power Plant", "pp", ship.Comps.PowerPlants, PowerPlantCatalog.NamesForSize, saved));
            components.AddRange(BuildComponentGroup("Shield Generator", "sg", ship.Comps.Shields, ShieldCatalog.NamesForSize, saved));
            components.AddRange(BuildComponentGroup("Cooler", "cooler", ship.Comps.Coolers, CoolerCatalog.NamesForSize, saved));
            components.AddRange(BuildComponentGroup("Quantum Drive", "qd", ship.Comps.QuantumDrives, QuantumDriveCatalog.NamesForSize, saved));
            components.AddRange(BuildComponentGroup("Flight Controller", "flight", ship.Comps.FlightControllers, FlightControllerCatalog.NamesForSize, saved));
        }
        ComponentList.ItemsSource = null;
        ComponentList.ItemsSource = components;
        ComponentHeader.Visibility = components.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        var readOnlyParts = new List<ReadOnlyPartDisplay>();
        if (ship.Comps != null)
        {
            readOnlyParts.AddRange(BuildReadOnlyGroup("Radar", ship.Comps.Radar));
            readOnlyParts.AddRange(BuildReadOnlyGroup("Life Support", ship.Comps.LifeSupport));
        }
        ReadOnlyPartList.ItemsSource = null;
        ReadOnlyPartList.ItemsSource = readOnlyParts;
        ReadOnlyPartHeader.Visibility = readOnlyParts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Setting properties above only QUEUES layout/render work at Render priority — it doesn't
        // happen synchronously. That queued work should normally flush within one frame once this
        // method returns to the message loop, but empirically it sometimes didn't (confirmed with
        // a blocking MessageBox right here: the content was already correctly laid out underneath
        // it, proving the delay is in *painting*, not in this method's logic). Explicitly pumping
        // the dispatcher up to Render priority forces that flush synchronously instead of hoping
        // it happens before the user looks. Bug reported 2026-09-28.
        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        NativeInterop.ForceRedraw(this);
    }

    private static List<ReadOnlyPartDisplay> BuildReadOnlyGroup(string displayName, List<ComponentPart> parts)
    {
        var result = new List<ReadOnlyPartDisplay>();
        for (int i = 0; i < parts.Count; i++)
        {
            var name = parts[i].Name;
            if (string.IsNullOrEmpty(name)) continue; // unused placeholder slot
            var label = parts.Count > 1 ? $"{displayName} {i + 1}" : displayName;
            result.Add(new ReadOnlyPartDisplay { Label = label, PartName = name });
        }
        return result;
    }

    private static WeaponSlotDisplay BuildGunRow(string label, int size, bool gimbal, bool isFixed, string? defaultName, ShipLoadoutChoice saved)
    {
        var options = WeaponCatalog.NamesForSize(size);
        var stock = defaultName ?? "Empty";
        if (!options.Contains(stock)) options.Insert(0, stock); // keep the ship's stock weapon selectable even if it's not in the trimmed catalog
        // A previously saved choice for this exact slot wins over stock, but only if it's still a
        // valid option for this mount size (catalog/ship data may have changed).
        var current = saved.WeaponBySlotLabel.TryGetValue(label, out var chosen) && options.Contains(chosen) ? chosen : stock;
        return new WeaponSlotDisplay
        {
            Label = label, Size = size,
            MountKind = isFixed ? " Fixed" : gimbal ? " Gimbal" : "",
            Options = options, SelectedName = current,
        };
    }

    // One method for all 4 component kinds (power plant, shield, cooler, quantum drive) since the
    // shape is identical: a ship can have several of the same kind, none of which have a real
    // label in ships.json, so "{kind}-{index}" becomes the persistence key AND the display label.
    private static List<ComponentSlotDisplay> BuildComponentGroup(
        string displayName, string kind, List<ComponentPart> parts,
        System.Func<int, List<string>> namesForSize, ShipLoadoutChoice saved)
    {
        var result = new List<ComponentSlotDisplay>();
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Size is not int size) continue; // unused placeholder slot (e.g. no jumpdrive fitted) — nothing to pick from
            var key = $"{kind}-{i}";
            var options = namesForSize(size);
            var stock = part.Name ?? "Empty";
            if (!options.Contains(stock)) options.Insert(0, stock);
            var current = saved.ComponentByKey.TryGetValue(key, out var chosen) && options.Contains(chosen) ? chosen : stock;
            var label = parts.Count > 1 ? $"{displayName} {i + 1}" : displayName;
            result.Add(new ComponentSlotDisplay { Label = label, Key = key, Options = options, SelectedName = current });
        }
        return result;
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

    private void MissileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: MissileSlotDisplay slot }) return;
        if (string.IsNullOrEmpty(_currentShipName) || string.IsNullOrEmpty(slot.SelectedName)) return;
        LoadoutStore.SetMissile(_currentShipName, slot.Label, slot.SelectedName);
    }

    private void ComponentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: ComponentSlotDisplay slot }) return;
        if (string.IsNullOrEmpty(_currentShipName) || string.IsNullOrEmpty(slot.SelectedName)) return;
        LoadoutStore.SetComponent(_currentShipName, slot.Key, slot.SelectedName);
    }
}
