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
public sealed class ShipCrew
{
    [JsonPropertyName("min")] public int Min { get; set; }
    [JsonPropertyName("max")] public int Max { get; set; }
}

public sealed class ShipShield
{
    [JsonPropertyName("hp")] public double? Hp { get; set; }
    [JsonPropertyName("regen")] public double? Regen { get; set; }
    [JsonPropertyName("face")] public string? Face { get; set; }
}

public sealed class ShipWeaponry
{
    [JsonPropertyName("pilot_dps")] public double? PilotDps { get; set; }
    [JsonPropertyName("pilot_alpha")] public double? PilotAlpha { get; set; }
    [JsonPropertyName("pilot_sustained")] public double? PilotSustained { get; set; }
    [JsonPropertyName("missile_dmg")] public double? MissileDmg { get; set; }
    [JsonPropertyName("missile_count")] public double? MissileCount { get; set; }
}

public sealed class WeaponSlot
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("lbl")] public string Label { get; set; } = "";
    [JsonPropertyName("sz")] public int Size { get; set; }
    [JsonPropertyName("gimbal")] public bool Gimbal { get; set; }
    [JsonPropertyName("fixed")] public bool Fixed { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; } = 1;
    [JsonPropertyName("defaultName")] public string? DefaultName { get; set; }
    [JsonPropertyName("locked")] public bool Locked { get; set; }
}

public sealed class MissileSlot
{
    [JsonPropertyName("lbl")] public string Label { get; set; } = "";
    [JsonPropertyName("sz")] public int Size { get; set; } // the RACK's own size, not the missile it holds
    [JsonPropertyName("holds_sz")] public int HoldsSize { get; set; } // the missile size that actually fits — use this for compatibility, not Size
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("defaultName")] public string? DefaultName { get; set; }
    [JsonPropertyName("podName")] public string? PodName { get; set; }
    [JsonPropertyName("locked")] public bool Locked { get; set; }
}

public sealed class Ship
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("size")] public string Size { get; set; } = "";
    [JsonPropertyName("scm")] public double? Scm { get; set; }
    [JsonPropertyName("boost")] public double? Boost { get; set; }
    [JsonPropertyName("nav")] public double? Nav { get; set; }
    [JsonPropertyName("hp")] public double? Hp { get; set; }
    [JsonPropertyName("mass")] public double? Mass { get; set; }
    [JsonPropertyName("cargo")] public double? Cargo { get; set; }
    [JsonPropertyName("crew")] public ShipCrew? Crew { get; set; }
    [JsonPropertyName("shield")] public ShipShield? Shield { get; set; }
    [JsonPropertyName("weaponry")] public ShipWeaponry? Weaponry { get; set; }
    [JsonPropertyName("slots")] public List<WeaponSlot> Slots { get; set; } = new();
    [JsonPropertyName("missiles")] public List<MissileSlot> Missiles { get; set; } = new();
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
