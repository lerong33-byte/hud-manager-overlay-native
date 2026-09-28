using System;
using System.Runtime.InteropServices;

namespace HudManagerOverlay;

// Thin wrapper around the handful of raw Win32 calls the overlay shell needs:
// making the window layered + click-through, and a global hotkey to toggle it.
// This is the native equivalent of what main.js did through koffi/ffi-napi in the
// Electron build — same OS mechanisms, just called directly from managed code.
internal static class NativeInterop
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int WM_HOTKEY = 0x0312;
    public const uint VK_RMENU = 0xA5; // Right Alt — matches the wheel/rail toggle key used today

    // Layered + tool-window + no-activate: the window never steals focus, never shows in the
    // taskbar/alt-tab, and supports per-pixel alpha. Click-through is added/removed separately
    // at runtime via SetClickThrough, mirroring the panel-host hit-testing from the old build.
    public static void MakeLayeredOverlay(IntPtr hwnd)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    public static void SetClickThrough(IntPtr hwnd, bool clickThrough)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        style = clickThrough ? (style | WS_EX_TRANSPARENT) : (style & ~WS_EX_TRANSPARENT);
        SetWindowLong(hwnd, GWL_EXSTYLE, style);

        // SetWindowLong alone doesn't reliably make Windows re-evaluate hit-testing against the
        // new extended style right away — SWP_FRAMECHANGED forces that immediately. Without this,
        // toggling WS_EX_TRANSPARENT off could lag or silently not take effect for the very click
        // that was supposed to land right after it, independent of how fast the flip was detected
        // (2026-09-28: list selection intermittently failed even after switching to a real-time
        // mouse hook, which ruled out timing/polling as the cause).
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }
}
