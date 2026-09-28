namespace HudManagerOverlay;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class ShipLoadoutChoice
{
    public string ShipName { get; set; } = "";
    public Dictionary<string, string> WeaponBySlotLabel { get; set; } = new();
    public Dictionary<string, string> MissileBySlotLabel { get; set; } = new();
}

public static class LoadoutStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HudManagerOverlay",
        "loadouts.json");

    private static Dictionary<string, ShipLoadoutChoice>? _cache;

    private static void EnsureLoaded()
    {
        if (_cache != null)
            return;

        _cache = new();

        try
        {
            if (!File.Exists(StorePath))
                return;

            string json = File.ReadAllText(StorePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, ShipLoadoutChoice>>(json);
            if (loaded != null)
                _cache = loaded;
        }
        catch
        {
            // Corrupt or unparseable file; start fresh
        }
    }

    public static ShipLoadoutChoice Get(string shipName)
    {
        EnsureLoaded();
        if (_cache!.TryGetValue(shipName, out var choice))
            return choice;

        // Return new instance without adding to cache; only Save() persists
        return new ShipLoadoutChoice { ShipName = shipName };
    }

    public static void SetWeapon(string shipName, string slotLabel, string weaponName)
    {
        EnsureLoaded();

        if (!_cache!.TryGetValue(shipName, out var choice))
        {
            choice = new ShipLoadoutChoice { ShipName = shipName };
            _cache[shipName] = choice;
        }

        choice.WeaponBySlotLabel[slotLabel] = weaponName;
        Save();
    }

    public static void SetMissile(string shipName, string slotLabel, string missileName)
    {
        EnsureLoaded();

        if (!_cache!.TryGetValue(shipName, out var choice))
        {
            choice = new ShipLoadoutChoice { ShipName = shipName };
            _cache[shipName] = choice;
        }

        choice.MissileBySlotLabel[slotLabel] = missileName;
        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_cache, options);
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // Failed save; don't crash
        }
    }
}
