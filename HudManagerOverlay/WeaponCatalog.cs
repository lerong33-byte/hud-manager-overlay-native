using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class WeaponEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-weapons.json (name + size only, 144 guns) — the full file is 1.8MB
// of raw per-weapon game-export data (fire rate, damage curves, heat, distortion...) that a
// simple "what else fits this mount" picker doesn't need. Re-extract with a script if the source
// data refreshes; see the trim step this was generated with if the format ever changes.
internal static class WeaponCatalog
{
    private static List<WeaponEntry>? _all;

    public static List<WeaponEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "weapons.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<WeaponEntry>>(json) ?? new List<WeaponEntry>();
        }
        catch
        {
            _all = new List<WeaponEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(w => w.Size == size).Select(w => w.Name).Distinct().OrderBy(n => n).ToList();
}
