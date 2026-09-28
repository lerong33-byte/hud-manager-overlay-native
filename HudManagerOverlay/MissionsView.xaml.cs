using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HudManagerOverlay;

public sealed class CategoryDisplay
{
    public string Key { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Count { get; init; } = "";
    public List<Mission> Missions { get; init; } = new();
}

// Flattened for the mission-list ItemsControl — reward/system text is precomputed once per
// category switch rather than re-formatted per binding refresh.
public sealed class MissionDisplay
{
    public Mission Mission { get; init; } = null!;
    public string Title => Mission.Title ?? "Unknown";
    public string RewardText { get; init; } = "";
    public string SystemsText { get; init; } = "";
}

public partial class MissionsView : UserControl
{
    private readonly List<CategoryDisplay> _categories;
    private List<MissionDisplay> _currentCategoryMissions = new();

    // Bucket keys aren't user-facing — translate the ones with an obvious human name, fall back
    // to a simple title-case-ish cleanup for the rest rather than hardcoding all 20.
    private static readonly Dictionary<string, string> NiceNames = new()
    {
        ["cargo_illegal"] = "Illegal Cargo",
        ["misc"] = "Misc",
        ["combat_illegal"] = "Illegal Combat",
        ["salvage"] = "Salvage",
        ["cargo"] = "Cargo",
        ["combat_general"] = "Combat",
        ["bunker"] = "Bunker",
        ["escort"] = "Escort",
        ["medical"] = "Medical",
        ["raid_xenothreat"] = "Xenothreat Raid",
        ["bounty_low"] = "Bounty (Low)",
        ["mining"] = "Mining",
        ["bounty_hrt"] = "Bounty (High-Risk)",
        ["bounty_mrt"] = "Bounty (Med-Risk)",
        ["bounty_ert"] = "Bounty (Extreme-Risk)",
        ["bounty_vhrt"] = "Bounty (Very High-Risk)",
        ["recon"] = "Recon",
        ["refuel"] = "Refuel",
        ["repair"] = "Repair",
        ["raid_9tails"] = "Nine Tails Raid",
    };

    public MissionsView()
    {
        InitializeComponent();

        var buckets = MissionData.Load();
        _categories = buckets
            .Where(kv => kv.Value.Count > 0)
            .Select(kv => new CategoryDisplay
            {
                Key = kv.Key,
                DisplayName = NiceNames.TryGetValue(kv.Key, out var nice) ? nice : kv.Key,
                Count = $"{kv.Value.Count} missions",
                Missions = kv.Value,
            })
            .OrderBy(c => c.DisplayName)
            .ToList();

        CategoryList.ItemsSource = _categories;
        if (_categories.Count > 0) CategoryList.SelectedIndex = 0;
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not CategoryDisplay cat) return;

        _currentCategoryMissions = cat.Missions.Select(ToDisplay).ToList();
        SearchBox.Text = "";
        MissionList.ItemsSource = _currentCategoryMissions;
        ShowNoSelection();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        MissionList.ItemsSource = q.Length == 0
            ? _currentCategoryMissions
            : _currentCategoryMissions.Where(m =>
                m.Title.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void MissionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MissionList.SelectedItem is MissionDisplay md) ShowMission(md.Mission);
        else ShowNoSelection();
    }

    private void ShowNoSelection()
    {
        NoSelectionText.Visibility = Visibility.Visible;
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowMission(Mission m)
    {
        NoSelectionText.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

        TitleText.Text = m.Title ?? "Unknown";
        GiverText.Text = m.MissionGiver ?? "Unknown giver";
        RewardDetailText.Text = FormatReward(m);
        TimeText.Text = m.TimeToCompleteMinutes is double mins ? $"{Math.Round(mins)} min" : "—";
        EnemyText.Text = FormatEnemies(m);
        SystemsDetailText.Text = m.StarSystems.Count > 0 ? string.Join(", ", m.StarSystems) : "—";
        IllegalBadge.Visibility = m.Illegal ? Visibility.Visible : Visibility.Collapsed;
    }

    private static MissionDisplay ToDisplay(Mission m) => new()
    {
        Mission = m,
        RewardText = FormatReward(m),
        SystemsText = m.StarSystems.Count > 0 ? string.Join(", ", m.StarSystems) : "—",
    };

    private static string FormatReward(Mission m)
    {
        if (m.RewardMin is not int min || min <= 0) return "—";
        if (m.RewardMax is int max && max > min)
            return $"{min.ToString("N0", CultureInfo.InvariantCulture)} - {max.ToString("N0", CultureInfo.InvariantCulture)} aUEC";
        return $"{min.ToString("N0", CultureInfo.InvariantCulture)} aUEC";
    }

    private static string FormatEnemies(Mission m)
    {
        if (!m.HasCombat) return "None";
        if (m.EnemyCountMin is int min && m.EnemyCountMax is int max)
            return min == max ? $"{min}" : $"{min}-{max}";
        return "Unknown count";
    }
}
