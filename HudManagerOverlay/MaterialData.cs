using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class Material
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("tier")] public string? Tier { get; set; }
    [JsonPropertyName("rs")] public int? Rs { get; set; }
    [JsonPropertyName("mineable")] public bool Mineable { get; set; }
}

internal static class MaterialData
{
    public static List<Material> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "materials.json");
        var json = File.ReadAllText(path);
        var materials = JsonSerializer.Deserialize<List<Material>>(json) ?? new List<Material>();

        // Filter out materials with null or whitespace names
        return materials
            .Where(m => !string.IsNullOrWhiteSpace(m.Name))
            .ToList();
    }
}
