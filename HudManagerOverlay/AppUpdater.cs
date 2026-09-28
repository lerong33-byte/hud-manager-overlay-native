using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace HudManagerOverlay;

// Auto-update via Velopack, checking against the public GitHub releases on this same repo
// (github.com/lerong33-byte/hud-manager-overlay-native — made public 2026-09-28 specifically so
// GithubSource can read releases without an embedded credential; shipping a real access token
// inside a distributed app would let anyone extract and reuse it).
//
// Update timing is deliberately conservative for an always-on gaming overlay: check once, well
// after startup, download silently in the background if something newer exists, then only ever
// APPLY it the next time the user deliberately quits (tray menu "Quit") — never force a restart
// mid-session. Forcing a restart while someone's mid-fight with the hotbar/tool rail active would
// be exactly the kind of disruption this whole app exists to avoid.
internal static class AppUpdater
{
    private const string RepoUrl = "https://github.com/lerong33-byte/hud-manager-overlay-native";

    private static UpdateManager? _manager;
    private static UpdateInfo? _pendingUpdate;

    public static void CheckInBackground()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Let the app finish opening or the AV false-positive theatrics have no chance
                // to compete with startup; this is a low-priority background task either way.
                await Task.Delay(TimeSpan.FromSeconds(15));

                var source = new GithubSource(RepoUrl, accessToken: null, prerelease: false);
                _manager = new UpdateManager(source);

                // Running from a plain `dotnet build`/F5 dev session (not installed via the
                // Velopack Setup.exe) has no update metadata on disk at all — CheckForUpdatesAsync
                // would just throw. Skip entirely rather than let that surface as a startup error
                // for every dev session.
                if (!_manager.IsInstalled) return;

                var info = await _manager.CheckForUpdatesAsync();
                if (info == null) return;

                await _manager.DownloadUpdatesAsync(info);
                _pendingUpdate = info;
            }
            catch
            {
                // Offline, rate-limited, GitHub having a bad day — none of that should ever be
                // user-visible for a background check. It'll just try again next launch.
            }
        });
    }

    // Called from the tray "Quit" path only (see MainWindow's QuitRequested handler) — never on
    // any other exit route, and never instead of a normal close if there's nothing pending.
    // Returns true if it's handling the exit itself (applying the update, then exiting), false if
    // the caller should just do its own normal shutdown.
    public static bool TryApplyPendingUpdateAndExit()
    {
        if (_manager == null || _pendingUpdate == null) return false;
        try
        {
            _manager.ApplyUpdatesAndExit(_pendingUpdate.TargetFullRelease);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
