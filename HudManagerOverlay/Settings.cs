using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HudManagerOverlay;

public sealed class PanelState
{
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
}

public sealed class AppSettings
{
    public Dictionary<string, PanelState> Panels { get; set; } = new();
    public bool RailEnabled { get; set; } = true;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HudManagerOverlay", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch { /* corrupt/missing settings should never crash startup — just start fresh */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* best-effort — a failed save shouldn't crash the overlay */ }
    }
}
