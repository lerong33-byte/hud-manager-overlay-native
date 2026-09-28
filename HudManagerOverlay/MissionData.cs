using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HudManagerOverlay;

public sealed class Mission
{
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("mission_giver")] public string? MissionGiver { get; set; }
    [JsonPropertyName("debug_name")] public string? DebugName { get; set; }
    [JsonPropertyName("illegal")] public bool Illegal { get; set; }
    [JsonPropertyName("has_combat")] public bool HasCombat { get; set; }
    [JsonPropertyName("has_hauling")] public bool HasHauling { get; set; }
    [JsonPropertyName("enemy_count_min")] public int? EnemyCountMin { get; set; }
    [JsonPropertyName("enemy_count_max")] public int? EnemyCountMax { get; set; }
    [JsonPropertyName("reward_min")] public int? RewardMin { get; set; }
    [JsonPropertyName("reward_max")] public int? RewardMax { get; set; }
    [JsonPropertyName("time_to_complete_minutes")] public int? TimeToCompleteMinutes { get; set; }
    [JsonPropertyName("star_systems")] public List<string> StarSystems { get; set; } = new();
    [JsonPropertyName("rank_index")] public int? RankIndex { get; set; }
    [JsonPropertyName("game_version")] public string? GameVersion { get; set; }
}

internal sealed class MissionDataFile
{
    [JsonPropertyName("buckets")] public Dictionary<string, List<Mission>> Buckets { get; set; } = new();
}

internal static class MissionData
{
    public static Dictionary<string, List<Mission>> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "missions.json");
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<MissionDataFile>(json) ?? new MissionDataFile();

        // Filter out missions with null or whitespace titles, and flatten buckets
        var filtered = new Dictionary<string, List<Mission>>();
        foreach (var kvp in file.Buckets)
        {
            filtered[kvp.Key] = kvp.Value
                .Where(m => !string.IsNullOrWhiteSpace(m.Title))
                .ToList();
        }

        return filtered;
    }
}
