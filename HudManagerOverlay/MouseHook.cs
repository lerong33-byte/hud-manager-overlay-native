using System;
using System.Runtime.InteropServices;

namespace HudManagerOverlay;

// Low-level global mouse hook (WH_MOUSE_LL) — same OS mechanism and same non-invasive guarantee
// as KeyboardHook: this only OBSERVES mouse movement system-wide, never injects anything into
// any other process. Added 2026-09-28 to fix PanelHost's click-through detection: a timer poll,
// even a fast one, checks cursor position on a fixed schedule that a real click can outrun —
// the click lands while the window is still WS_EX_TRANSPARENT and passes straight through,
// unrecoverably, before the next poll ever fires. A hook reacts to the actual WM_MOUSEMOVE that
// necessarily precedes any click, synchronously, so there's no gap left for a click to fall into.
internal sealed class MouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public event Action? Move;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    public MouseHook()
    {
        _proc = HookCallback; // keep a strong reference — the GC must never collect this
    }

    public void Install()
    {
        using var curModule = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam.ToInt32() == WM_MOUSEMOVE) Move?.Invoke();
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero) { UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; }
    }
}
