using System;
using System.Collections.Generic;
using System.IO;
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
        var recipes = JsonSerializer.Deserialize<Dictionary<string, Recipe>>(json) ?? new Dictionary<string, Recipe>();

        return recipes;
    }
}
