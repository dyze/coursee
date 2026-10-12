using System.Windows;
using System.Windows.Interop;
using Coursee.Core.Logging;
using Coursee.Core.Placement;

namespace Coursee.UI.Platform;

/// <summary>
/// Puts windows back where they were left (same monitor, same place) and remembers where they
/// are when they are hidden or closed. A window without a saved position opens as its XAML says.
/// </summary>
public sealed class WindowPlacer
{
    private readonly PlacementStore _store;
    private readonly ILog _log;

    public WindowPlacer(PlacementStore store, ILog log)
    {
        _store = store;
        _log = log;
    }

    /// <summary>
    /// Call before the window is first shown. <paramref name="name"/> is its key in windows.json.
    /// With <paramref name="keepSize"/> false only the position is restored (fixed-size windows).
    /// </summary>
    public void Attach(Window window, string name, bool keepSize)
    {
        ScreenRect? lastNormal = null;

        var saved = _store.Get(name);
        if (saved is not null)
        {
            // Otherwise WPF centers the window once it is shown, over the restored position.
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.SourceInitialized += (_, _) => Restore(window, name, saved, keepSize);
        }

        // A maximized window is saved with its normal size, so it can be restored to it later.
        void TrackNormalBounds(object? sender, EventArgs e)
        {
            if (window.WindowState == WindowState.Normal && NativeWindows.WindowBounds(Handle(window)) is { } bounds)
            {
                lastNormal = bounds;
            }
        }

        window.LocationChanged += TrackNormalBounds;
        window.SizeChanged += (sender, e) => TrackNormalBounds(sender, e);
        window.IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is false)
            {
                Save(window, name, lastNormal);
            }
        };
        window.Closing += (_, _) => Save(window, name, lastNormal);
    }

    private void Restore(Window window, string name, WindowPlacement saved, bool keepSize)
    {
        var handle = Handle(window);
        if (!keepSize && NativeWindows.WindowBounds(handle) is { } current)
        {
            saved = saved with { Bounds = saved.Bounds with { Width = current.Width, Height = current.Height } };
        }

        if (PlacementRules.Fit(saved, NativeWindows.Monitors()) is not { } bounds)
        {
            return;
        }

        NativeWindows.SetWindowBounds(handle, bounds);
        if (keepSize)
        {
            // On a monitor with another scaling, WPF resizes the window to keep its size in its own
            // units; setting the rectangle again, now that the scaling is right, makes it exact.
            NativeWindows.SetWindowBounds(handle, bounds);
        }

        if (keepSize && saved.Maximized)
        {
            window.WindowState = WindowState.Maximized;
        }

        _log.Debug("Window", $"{name} restored on {saved.Monitor} at {bounds.Left},{bounds.Top} ({bounds.Width}x{bounds.Height})");
    }

    private void Save(Window window, string name, ScreenRect? lastNormal)
    {
        var maximized = window.WindowState == WindowState.Maximized;
        var bounds = maximized ? lastNormal : NativeWindows.WindowBounds(Handle(window)) ?? lastNormal;

        // Minimized, or never shown normal: nothing worth remembering.
        if (bounds is null || window.WindowState == WindowState.Minimized)
        {
            return;
        }

        if (PlacementRules.Capture(bounds, NativeWindows.Monitors(), maximized) is not { } placement)
        {
            return;
        }

        try
        {
            _store.Set(name, placement);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Warn("Window", $"Cannot save the position of {name}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static IntPtr Handle(Window window) => new WindowInteropHelper(window).Handle;
}
