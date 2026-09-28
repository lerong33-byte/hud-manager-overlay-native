using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HudManagerOverlay;

// Flattened for the list ItemsControl — precomputed once at load rather than per binding refresh.
public sealed class BlueprintDisplay
{
    public Blueprint Blueprint { get; init; } = null!;
    public string Name => Blueprint.Name ?? "Unknown";
    public string CraftTimeText { get; init; } = "";
    public string SourceCountText { get; init; } = "";
}

public sealed class SourceDisplay
{
    public string Title { get; init; } = "";
    public string ScopeAndChance { get; init; } = "";
}

public partial class BlueprintsView : UserControl
{
    private readonly List<BlueprintDisplay> _all;

    public BlueprintsView()
    {
        InitializeComponent();

        _all = BlueprintData.Load()
            .OrderBy(b => b.Name)
            .Select(ToDisplay)
            .ToList();

        BlueprintList.ItemsSource = _all;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        BlueprintList.ItemsSource = q.Length == 0
            ? _all
            : _all.Where(b => b.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void BlueprintList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BlueprintList.SelectedItem is BlueprintDisplay bd) ShowBlueprint(bd.Blueprint);
        else ShowNoSelection();
    }

    private void ShowNoSelection()
    {
        NoSelectionText.Visibility = Visibility.Visible;
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowBlueprint(Blueprint b)
    {
        NoSelectionText.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

        NameText.Text = b.Name ?? "Unknown";
        DefaultBadge.Visibility = b.IsAvailableByDefault ? Visibility.Visible : Visibility.Collapsed;
        CraftTimeDetailText.Text = b.CraftTimeLabel ?? "—";
        IngredientText.Text = b.IngredientCount is int c ? $"{c}" : "—";

        var sources = b.Sources.Select(s => new SourceDisplay
        {
            Title = s.Title ?? s.DebugName ?? "Unknown source",
            ScopeAndChance = FormatScopeChance(s.RewardScope, s.Chance),
        }).ToList();
        SourceList.ItemsSource = sources;
        SourcesHeader.Text = sources.Count > 0 ? "UNLOCK SOURCES" : "UNLOCK SOURCES — none known";
    }

    private static BlueprintDisplay ToDisplay(Blueprint b) => new()
    {
        Blueprint = b,
        CraftTimeText = b.CraftTimeLabel ?? "—",
        SourceCountText = b.Sources.Count switch
        {
            0 => "no known source",
            1 => "1 source",
            var n => $"{n} sources",
        },
    };

    private static string FormatScopeChance(string? scope, double? chance)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(scope)) parts.Add(scope!);
        if (chance is double c) parts.Add($"{c * 100:0}% chance");
        return parts.Count > 0 ? string.Join(" · ", parts) : "";
    }
}
