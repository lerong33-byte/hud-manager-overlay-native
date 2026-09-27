using System;
using System.Runtime.InteropServices;

namespace HudManagerOverlay;

// Low-level global keyboard hook (WH_KEYBOARD_LL) — the same OS mechanism the Electron build
// used via koffi, now called directly. This only OBSERVES keystrokes system-wide; it doesn't
// inject anything into any other process, so it carries none of the anti-cheat risk a
// render-hook would.
internal sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    // Alt (and Alt+anything) generates the "system key" messages, not the plain ones — confirmed
    // live: Right Alt went completely undetected until these were added. Without this, ANY
    // Alt-based binding (including the old app's own Alt+H/Alt+P foot-deck actions, which only
    // need to be SENT, not detected, so those were fine) would silently fail if ever watched for.
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public event Action<int>? KeyDown;
    public event Action<int>? KeyUp;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    public KeyboardHook()
    {
        _proc = HookCallback; // keep a strong reference — the GC must never collect this
    }

    public void Install()
    {
        using var curModule = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var w = wParam.ToInt32();
            if (w == WM_KEYDOWN || w == WM_SYSKEYDOWN) KeyDown?.Invoke(vkCode);
            else if (w == WM_KEYUP || w == WM_SYSKEYUP) KeyUp?.Invoke(vkCode);
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero) { UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; }
    }
}
