using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class ShieldEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-shields.json (name + size only, 64 entries) — the full file is 980KB
// of raw per-part game-export data (regeneration rate, damage absorption, distortion resistance...)
// that a simple "what else fits this slot" picker doesn't need. Re-extract with a script if the source
// data refreshes; see the trim step this was generated with if the format ever changes.
internal static class ShieldCatalog
{
    private static List<ShieldEntry>? _all;

    public static List<ShieldEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "shields.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<ShieldEntry>>(json) ?? new List<ShieldEntry>();
        }
        catch
        {
            _all = new List<ShieldEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(s => s.Size == size).Select(s => s.Name).Distinct().OrderBy(n => n).ToList();
}
