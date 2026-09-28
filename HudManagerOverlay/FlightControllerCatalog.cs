using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class FlightControllerEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public int Size { get; set; }
}

// Trimmed from data/v2/erkul-controllers.json (name + size only, 70 entries) — the full file
// contains detailed game-export data that a simple "what else fits this slot" picker doesn't need.
// Re-extract with a script if the source data refreshes; see the trim step this was generated with
// if the format ever changes.
internal static class FlightControllerCatalog
{
    private static List<FlightControllerEntry>? _all;

    public static List<FlightControllerEntry> All()
    {
        if (_all != null) return _all;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "flightcontrollers.json");
            var json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<FlightControllerEntry>>(json) ?? new List<FlightControllerEntry>();
        }
        catch
        {
            _all = new List<FlightControllerEntry>();
        }
        return _all;
    }

    public static List<string> NamesForSize(int size) =>
        All().Where(f => f.Size == size).Select(f => f.Name).Distinct().OrderBy(n => n).ToList();
}
