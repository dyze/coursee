using System.Runtime.InteropServices;
using Coursee.Core.Placement;

namespace Coursee.UI.Platform;

/// <summary>
/// The monitors and window rectangles as Windows sees them, in screen pixels. WPF works in
/// device-independent units, which differ from one monitor to another when their scaling
/// differs; pixels keep saved positions exact.
/// </summary>
public static class NativeWindows
{
    private const uint MonitorInfoPrimary = 0x1;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    /// <summary>The monitors connected now, with their work area (without the taskbar).</summary>
    public static IReadOnlyList<MonitorInfo> Monitors()
    {
        var monitors = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr handle, IntPtr hdc, ref NativeRect bounds, IntPtr data) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(handle, ref info))
            {
                monitors.Add(new MonitorInfo(info.Device, ToScreenRect(info.Work), (info.Flags & MonitorInfoPrimary) != 0));
            }

            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    /// <summary>The window's outer rectangle, or null when it has no handle.</summary>
    public static ScreenRect? WindowBounds(IntPtr window) =>
        window != IntPtr.Zero && GetWindowRect(window, out var rect) ? ToScreenRect(rect) : null;

    /// <summary>Moves and resizes the window without activating it.</summary>
    public static void SetWindowBounds(IntPtr window, ScreenRect bounds) =>
        SetWindowPos(window, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SwpNoZOrder | SwpNoActivate);

    private static ScreenRect ToScreenRect(NativeRect rect) =>
        new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Device;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref NativeRect bounds, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
