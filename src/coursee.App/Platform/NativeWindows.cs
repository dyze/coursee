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
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpFrameChanged = 0x0020;
    private const int GwlStyle = -16;
    private const long WsMaximizeBox = 0x00010000;
    private const long WsMinimizeBox = 0x00020000;
    private const int DwmExtendedFrameBounds = 9;

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

    /// <summary>
    /// The part of the window that can be seen: without the invisible resize borders Windows
    /// adds around it. Falls back to <see cref="WindowBounds"/>.
    /// </summary>
    public static ScreenRect? VisibleBounds(IntPtr window) =>
        window != IntPtr.Zero
        && DwmGetWindowAttribute(window, DwmExtendedFrameBounds, out var rect, Marshal.SizeOf<NativeRect>()) == 0
            ? ToScreenRect(rect)
            : WindowBounds(window);

    /// <summary>Moves and resizes the window without activating it.</summary>
    public static void SetWindowBounds(IntPtr window, ScreenRect bounds) =>
        SetWindowPos(window, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SwpNoZOrder | SwpNoActivate);

    /// <summary>Removes the minimize and maximize buttons from the title bar. The window can still be resized.</summary>
    public static void RemoveMinimizeMaximizeButtons(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(window, GwlStyle).ToInt64();
        SetWindowLongPtr(window, GwlStyle, new IntPtr(style & ~(WsMinimizeBox | WsMaximizeBox)));

        // The title bar is redrawn only when told that the frame changed.
        SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

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

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out NativeRect value, int size);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
