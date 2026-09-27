using System;
using System.Runtime.InteropServices;

namespace HudManagerOverlay;

// External process/window detection only — never touches Star Citizen's own process memory.
// Same safety boundary as the Electron build: FindWindow is a read-only OS query, not injection.
internal static class GameDetection
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public static bool IsRunning() =>
#if DEBUG_FORCE_SC_RUNNING
        true;
#else
        FindWindow(null, "Star Citizen") != IntPtr.Zero;
#endif

    public static bool IsForeground()
    {
        var sc = FindWindow(null, "Star Citizen");
        return sc != IntPtr.Zero && sc == GetForegroundWindow();
    }
}
