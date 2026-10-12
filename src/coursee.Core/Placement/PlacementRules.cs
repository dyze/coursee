namespace Coursee.Core.Placement;

/// <summary>
/// Turns a window's screen position into a <see cref="WindowPlacement"/> and back, making sure
/// a restored window is visible: its monitor may be unplugged, or smaller than before.
/// </summary>
public static class PlacementRules
{
    /// <summary>
    /// Describes a window at <paramref name="bounds"/> (screen pixels) relative to the monitor
    /// that shows most of it. Returns null when there is no monitor.
    /// </summary>
    public static WindowPlacement? Capture(ScreenRect bounds, IReadOnlyList<MonitorInfo> monitors, bool maximized = false)
    {
        var monitor = MonitorOf(bounds, monitors);
        if (monitor is null)
        {
            return null;
        }

        var work = monitor.WorkArea;
        var relative = bounds with { Left = bounds.Left - work.Left, Top = bounds.Top - work.Top };
        return new WindowPlacement(monitor.DeviceName, relative, maximized);
    }

    /// <summary>
    /// The screen rectangle where to show a saved window. It goes back on its monitor, or on the
    /// primary one when its monitor is gone, at the same place within the work area. It is then
    /// made to fit: no larger than the work area, and moved inside it. Returns null when there
    /// is no monitor.
    /// </summary>
    public static ScreenRect? Fit(WindowPlacement placement, IReadOnlyList<MonitorInfo> monitors)
    {
        var monitor = monitors.FirstOrDefault(m => string.Equals(m.DeviceName, placement.Monitor, StringComparison.OrdinalIgnoreCase))
            ?? Primary(monitors);
        if (monitor is null)
        {
            return null;
        }

        var work = monitor.WorkArea;
        var saved = placement.Bounds;
        var width = Math.Clamp(saved.Width, 1, Math.Max(1, work.Width));
        var height = Math.Clamp(saved.Height, 1, Math.Max(1, work.Height));
        var left = Math.Clamp(work.Left + saved.Left, work.Left, Math.Max(work.Left, work.Right - width));
        var top = Math.Clamp(work.Top + saved.Top, work.Top, Math.Max(work.Top, work.Bottom - height));

        return new ScreenRect(left, top, width, height);
    }

    /// <summary>
    /// Where to move a window at <paramref name="bounds"/> so that it is fully visible: inside the
    /// work area of the monitor showing most of it, shrunk if larger. Equal to
    /// <paramref name="bounds"/> when it is already visible; null when there is no monitor.
    /// </summary>
    public static ScreenRect? KeepVisible(ScreenRect bounds, IReadOnlyList<MonitorInfo> monitors) =>
        Capture(bounds, monitors) is { } placement ? Fit(placement, monitors) : null;

    /// <summary>The monitor holding the window's center, else the one it overlaps most, else the primary one.</summary>
    private static MonitorInfo? MonitorOf(ScreenRect bounds, IReadOnlyList<MonitorInfo> monitors)
    {
        var centerX = bounds.Left + (bounds.Width / 2);
        var centerY = bounds.Top + (bounds.Height / 2);

        return monitors.FirstOrDefault(m => m.WorkArea.Contains(centerX, centerY))
            ?? monitors.Where(m => m.WorkArea.OverlapArea(bounds) > 0).MaxBy(m => m.WorkArea.OverlapArea(bounds))
            ?? Primary(monitors);
    }

    private static MonitorInfo? Primary(IReadOnlyList<MonitorInfo> monitors) =>
        monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
}
