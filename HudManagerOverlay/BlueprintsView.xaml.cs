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

public sealed class IngredientDisplay
{
    public string Name { get; init; } = "";
    public string QtyText { get; init; } = "";
}

public partial class BlueprintsView : UserControl
{
    private readonly List<BlueprintDisplay> _all;
    private readonly Dictionary<string, Recipe> _recipes;

    public BlueprintsView()
    {
        InitializeComponent();

        _recipes = RecipeData.Load();
        _all = BlueprintData.Load()
            .OrderBy(b => b.Name)
            .Select(ToDisplay)
            .ToList();

        BlueprintList.ItemsSource = _all;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text?.Trim() ?? "";
        // Reassigning to a plain List<T> should always force a full ItemsControl refresh, but in
        // this app's layered click-through overlay window that silently didn't happen for a
        // reassignment after the control was already showing something — clearing to null first
        // forces WPF to fully detach and rebuild instead of (failing to) diff the old set.
        // Same root cause as the 2026-09-28 Missions category bug; applied everywhere ItemsSource
        // gets reassigned post-initial-load.
        BlueprintList.ItemsSource = null;
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

        // Recipe ingredients are a separate dataset (crafting-enriched.json) keyed by output_class,
        // not part of blueprints.json itself — only some blueprints have a matching recipe entry.
        var ingredients = b.OutputClass != null && _recipes.TryGetValue(b.OutputClass, out var recipe)
            ? recipe.Ingredients.Select(i => new IngredientDisplay
            {
                Name = i.Name ?? "Unknown",
                QtyText = i.Qty is double q ? $"{q:0.##}{(string.IsNullOrEmpty(i.Unit) ? "" : " " + i.Unit)}" : "",
            }).ToList()
            : new List<IngredientDisplay>();
        IngredientList.ItemsSource = null;
        IngredientList.ItemsSource = ingredients;
        IngredientHeader.Text = ingredients.Count > 0
            ? "INGREDIENTS"
            : b.IngredientCount is int c ? $"INGREDIENTS — {c} (detail unavailable)" : "INGREDIENTS — unavailable";

        var sources = b.Sources.Select(s => new SourceDisplay
        {
            Title = s.Title ?? s.DebugName ?? "Unknown source",
            ScopeAndChance = FormatScopeChance(s.RewardScope, s.Chance),
        }).ToList();
        SourceList.ItemsSource = null;
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
