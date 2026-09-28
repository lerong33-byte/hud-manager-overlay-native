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
    // Some keys (e.g. "_weapons_doc") are stray comment strings mixed into this dictionary by
    // whoever built the source file, not real entries — deserialize as JsonElement first and
    // filter to actual objects before converting, instead of crashing the whole app on them.
    [JsonPropertyName("items")] public Dictionary<string, JsonElement> Items { get; set; } = new();
}

internal static class AcquisitionData
{
    public static Dictionary<string, AcquisitionEntry> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "acquisition.json");
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<AcquisitionFile>(json);
        if (file is null) return new Dictionary<string, AcquisitionEntry>();

        var result = new Dictionary<string, AcquisitionEntry>();
        foreach (var (key, value) in file.Items)
        {
            if (value.ValueKind != JsonValueKind.Object) continue;
            var entry = value.Deserialize<AcquisitionEntry>();
            if (entry != null) result[key] = entry;
        }
        return result;
    }
}
