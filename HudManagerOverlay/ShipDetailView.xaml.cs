using System.Windows;
using System.Windows.Controls;

namespace HudManagerOverlay;

public partial class ShipDetailView : UserControl
{
    public ShipDetailView()
    {
        InitializeComponent();
    }

    public void ShowShip(Ship ship)
    {
        NoSelectionText.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

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
    }

    private static string Fmt(double? v, string suffix = "") =>
        v.HasValue ? $"{v.Value:N0}{suffix}" : "—";
}
