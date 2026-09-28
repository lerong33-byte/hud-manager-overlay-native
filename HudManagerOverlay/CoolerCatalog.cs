using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class CoolerEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-coolers.json (name + size only, 73 entries) — the full file is 1.1MB
// of raw per-part game-export data (cooling capacity, efficiency curves, thermal conductivity...)
// that a simple "what else fits this slot" picker doesn't need. Re-extract with a script if the source
// data refreshes; see the trim step this was generated with if the format ever changes.
internal static class CoolerCatalog
{
    private static List<CoolerEntry>? _all;

    public static List<CoolerEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "coolers.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<CoolerEntry>>(json) ?? new List<CoolerEntry>();
        }
        catch
        {
            _all = new List<CoolerEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(c => c.Size == size).Select(c => c.Name).Distinct().OrderBy(n => n).ToList();
}
