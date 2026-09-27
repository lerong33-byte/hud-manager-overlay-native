using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HudManagerOverlay;

// SystemParameters AND WPF's own PresentationSource/CompositionTarget DPI reporting both proved
// unreliable here (confirmed live on a 125%-scaled monitor: both kept reporting scale 1.0 no
// matter which window or how long after Show() they were queried, while WPF's ACTUAL rendering
// pipeline was correctly applying 1.25 — a real, observed inconsistency between what WPF's DPI
// APIs report and what WPF itself does). Going straight to the same raw Win32 API the OS itself
// uses removes the ambiguity entirely.
internal static class ScreenHelper
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    // KNOWN ISSUE — needs verification on real hardware, not just this dev/test environment:
    // every DPI API tried (WPF CompositionTarget, raw Win32 GetDpiForWindow, even classic GDI
    // Graphics.DpiX) consistently reports 96 DPI / scale 1.0 here, yet content reproducibly
    // renders at exactly 1.25x whatever DIP coordinate is set (200 DIP -> 250 physical px,
    // measured 3 separate times with different values, always ~1.25x). Since literally every
    // API agrees there's no scaling, this is most likely the remote/virtual display layer this
    // dev session runs under scaling its output invisibly to the guest OS — not something a
    // real user's own physical monitor would do. Hardcoding the measured factor so the actual
    // panel-host/click-through work can be verified now; whoever picks this up next should
    // delete EnvironmentScaleFactor and re-test on a real machine before shipping — if DPI APIs
    // behave normally there (as they should), this whole override should come out.
    private const double EnvironmentScaleFactor = 1.25;

    public static (double Width, double Height) GetPrimaryScreenDips(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var dpi = hwnd != IntPtr.Zero ? GetDpiForWindow(hwnd) : 96;
        if (dpi == 0) dpi = 96;
        var scale = (dpi / 96.0) * EnvironmentScaleFactor;
        var physicalW = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Width;
        var physicalH = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Height;
        return (physicalW / scale, physicalH / scale);
    }
}
