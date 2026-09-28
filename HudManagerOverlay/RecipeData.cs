using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class RecipeIngredient
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("kind")] public string? Kind { get; set; }
    [JsonPropertyName("qty")] public double? Qty { get; set; }
    [JsonPropertyName("unit")] public string? Unit { get; set; }
}

public sealed class Recipe
{
    [JsonPropertyName("ingredients")] public List<RecipeIngredient> Ingredients { get; set; } = new();
}

internal static class RecipeData
{
    public static Dictionary<string, Recipe> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "recipes.json");
        var json = File.ReadAllText(path);

        // Deserialize as JsonElement first and filter to real objects before converting — the
        // same defensive pattern applied to AcquisitionData after a stray non-object key
        // (a comment string mixed into the dictionary by whoever built that source file) crashed
        // the whole Acquisition panel on open. recipes.json has no such key today, but it's the
        // identical Dictionary<string, T> shape and the same person's data pipeline, so a future
        // refresh could introduce one — this avoids that taking down Blueprints too.
        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
        return raw
            .Where(kv => kv.Value.ValueKind == JsonValueKind.Object)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Deserialize<Recipe>() ?? new Recipe());
    }
}
