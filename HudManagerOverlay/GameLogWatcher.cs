using System;
using System.IO;
using System.Windows.Threading;

namespace HudManagerOverlay;

// Finds and tails Star Citizen's Game.log to detect entering a ship seat — same search paths
// and the same "GetStarmapRouteSegmentData" nav-line signal the Electron build used (main.js
// parsePlayerShipFromNav): only YOUR OWN client's quantum drive generates that line, so it's a
// reliable "you're in a seat" signal with no need to match your player handle. Exiting a seat is
// detected separately (see MainWindow's exit-seat hold-key logic), because Game.log has no
// "left seat" line — only a HOLD of the default Y key exits a seat in Star Citizen.
internal sealed class GameLogWatcher
{
    private static readonly string[] Channels = { "LIVE", "PTU", "EPTU", "HOTFIX" };
    private static readonly string[] Bases =
    {
        @"Program Files\Roberts Space Industries\StarCitizen",
        @"Games\StarCitizen",
        @"StarCitizen",
        @"RSI\StarCitizen",
        @"Program Files\StarCitizen",
    };

    private readonly DispatcherTimer _poll = new() { Interval = TimeSpan.FromMilliseconds(1200) };
    private string? _path;
    private long _lastLength;

    public event Action? SeatDetected;

    public void Start()
    {
        _poll.Tick += (_, _) => Tick();
        _poll.Start();
    }

    public static string? FindGameLog()
    {
        for (char drive = 'C'; drive <= 'H'; drive++)
        {
            foreach (var b in Bases)
            {
                foreach (var c in Channels)
                {
                    var p = $"{drive}:\\{b}\\{c}\\Game.log";
                    if (File.Exists(p)) return p;
                }
            }
        }
        return null;
    }

    private void Tick()
    {
        _path ??= FindGameLog();
        if (_path == null || !File.Exists(_path)) return;

        try
        {
            using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < _lastLength) _lastLength = 0; // log rotated/recreated on relaunch
            if (fs.Length == _lastLength) return;

            fs.Seek(_lastLength, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("[ItemNavigation][CL]") && line.Contains("GetStarmapRouteSegmentData"))
                {
                    SeatDetected?.Invoke();
                }
            }
            _lastLength = fs.Length;
        }
        catch (IOException)
        {
            // File briefly locked by SC itself — try again next tick, not fatal.
        }
    }
}
