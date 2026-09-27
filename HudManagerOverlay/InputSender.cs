using System;
using System.Runtime.InteropServices;

namespace HudManagerOverlay;

// SendInput = simulating a keypress at the OS input queue, exactly like any macro keyboard or
// AutoHotkey script. It never touches another process's memory, so it carries none of the
// anti-cheat risk a render-hook or injected DLL would.
internal static class InputSender
{
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void TapKey(ushort vk) => TapKey(vk, null);

    public static void TapKey(ushort vk, ushort? modifierVk)
    {
        var events = new System.Collections.Generic.List<INPUT>();
        if (modifierVk.HasValue) events.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = modifierVk.Value } });
        events.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = vk } });
        events.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } });
        if (modifierVk.HasValue) events.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = modifierVk.Value, dwFlags = KEYEVENTF_KEYUP } });
        SendInput((uint)events.Count, events.ToArray(), Marshal.SizeOf<INPUT>());
    }
}
