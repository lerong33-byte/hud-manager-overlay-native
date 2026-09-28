using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class PowerPlantEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-power-plants.json (name + size only, 75 entries) — the full file is 1.2MB
// of raw per-part game-export data (power output, thermal efficiency, wear characteristics...) that a
// simple "what else fits this slot" picker doesn't need. Re-extract with a script if the source
// data refreshes; see the trim step this was generated with if the format ever changes.
internal static class PowerPlantCatalog
{
    private static List<PowerPlantEntry>? _all;

    public static List<PowerPlantEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "powerplants.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<PowerPlantEntry>>(json) ?? new List<PowerPlantEntry>();
        }
        catch
        {
            _all = new List<PowerPlantEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(p => p.Size == size).Select(p => p.Name).Distinct().OrderBy(n => n).ToList();
}
