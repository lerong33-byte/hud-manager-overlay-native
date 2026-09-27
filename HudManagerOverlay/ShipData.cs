using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

// Mirrors the fields the Ship Loadout Bay list actually shows. The site's ships.json has far
// more (slots, weaponry, shield, etc.) — those get added as later panels need them. Unknown
// fields are simply ignored by System.Text.Json, so this stays forward-compatible.
public sealed class Ship
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("size")] public string Size { get; set; } = "";
    [JsonPropertyName("scm")] public double? Scm { get; set; }
    [JsonPropertyName("boost")] public double? Boost { get; set; }
}

internal static class ShipData
{
    public static List<Ship> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "ships.json");
        var json = File.ReadAllText(path);
        var dict = JsonSerializer.Deserialize<Dictionary<string, Ship>>(json)
                   ?? new Dictionary<string, Ship>();
        return dict.Values
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .OrderBy(s => s.Manufacturer)
            .ThenBy(s => s.Name)
            .ToList();
    }
}
