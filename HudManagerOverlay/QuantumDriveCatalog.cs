using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class QuantumDriveEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-qdrives.json (name + size only, 57 entries) — the full file is 890KB
// of raw per-part game-export data (jump range, fuel consumption, acceleration curves, spooling time...)
// that a simple "what else fits this slot" picker doesn't need. Re-extract with a script if the source
// data refreshes; see the trim step this was generated with if the format ever changes.
internal static class QuantumDriveCatalog
{
    private static List<QuantumDriveEntry>? _all;

    public static List<QuantumDriveEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "qdrives.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<QuantumDriveEntry>>(json) ?? new List<QuantumDriveEntry>();
        }
        catch
        {
            _all = new List<QuantumDriveEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(q => q.Size == size).Select(q => q.Name).Distinct().OrderBy(n => n).ToList();
}
