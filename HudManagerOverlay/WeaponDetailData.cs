using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class WeaponDetail
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
    [JsonPropertyName("damage")] public double? Damage { get; set; }
    [JsonPropertyName("fireRate")] public double? FireRate { get; set; }
    [JsonPropertyName("dps")] public double? Dps { get; set; }
    [JsonPropertyName("speed")] public double? Speed { get; set; }
}

internal static class WeaponDetailData
{
    public static List<WeaponDetail> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "weapons-detail.json");
        var json = File.ReadAllText(path);
        var weapons = JsonSerializer.Deserialize<List<WeaponDetail>>(json) ?? new List<WeaponDetail>();

        // Filter out weapons with null or whitespace names
        return weapons
            .Where(w => !string.IsNullOrWhiteSpace(w.Name))
            .ToList();
    }
}
