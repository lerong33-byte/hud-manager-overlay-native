using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class MissileEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-missiles.json (name + size only, 61 missiles) — the full file contains
// raw per-missile game-export data (guidance, lock time, damage, heat...) that a simple "what fits
// this pylon" picker doesn't need. Re-extract with a script if the source data refreshes.
internal static class MissileCatalog
{
    private static List<MissileEntry>? _all;

    public static List<MissileEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "missiles.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<MissileEntry>>(json) ?? new List<MissileEntry>();
        }
        catch
        {
            _all = new List<MissileEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(m => m.Size == size).Select(m => m.Name).Distinct().OrderBy(n => n).ToList();
}
