using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class AcquisitionEntry
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("locations")] public List<string> Locations { get; set; } = new();
}

internal class AcquisitionFile
{
    [JsonPropertyName("items")] public Dictionary<string, AcquisitionEntry> Items { get; set; } = new();
}

internal static class AcquisitionData
{
    public static Dictionary<string, AcquisitionEntry> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "acquisition.json");
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<AcquisitionFile>(json);

        return file?.Items ?? new Dictionary<string, AcquisitionEntry>();
    }
}
