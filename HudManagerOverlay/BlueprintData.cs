using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class BlueprintSource
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("debug_name")] public string? DebugName { get; set; }
    [JsonPropertyName("reward_scope")] public string? RewardScope { get; set; }
    [JsonPropertyName("chance")] public double? Chance { get; set; }
}

public sealed class Blueprint
{
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("key")] public string? Key { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("output_class")] public string? OutputClass { get; set; }
    [JsonPropertyName("category_uuid")] public string? CategoryUuid { get; set; }
    [JsonPropertyName("craft_time_seconds")] public int? CraftTimeSeconds { get; set; }
    [JsonPropertyName("craft_time_label")] public string? CraftTimeLabel { get; set; }
    [JsonPropertyName("is_available_by_default")] public bool IsAvailableByDefault { get; set; }
    [JsonPropertyName("ingredient_count")] public int? IngredientCount { get; set; }
    [JsonPropertyName("sources")] public List<BlueprintSource> Sources { get; set; } = new();
}

internal static class BlueprintData
{
    public static List<Blueprint> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "blueprints.json");
        var json = File.ReadAllText(path);
        var blueprints = JsonSerializer.Deserialize<List<Blueprint>>(json) ?? new List<Blueprint>();

        // Filter out blueprints with null or whitespace names
        return blueprints
            .Where(b => !string.IsNullOrWhiteSpace(b.Name))
            .ToList();
    }
}
