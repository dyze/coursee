using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Coursee.Core.Logging;
using Coursee.Core.Placement;

namespace Coursee.UI.Platform;

/// <summary>What <see cref="WindowPlacer"/> gives back to a window when it is shown again.</summary>
public enum RestoreMode
{
    /// <summary>Its place only (a window of fixed size, such as the launcher).</summary>
    Position,

    /// <summary>Its place and size, maximized or not.</summary>
    PositionAndSize,

    /// <summary>Its size only; it opens centered on its owner window (a dialog).</summary>
    SizeOverOwner,
}

/// <summary>
/// Puts windows back where they were left (same monitor, same place) and remembers where they
/// are when they are hidden or closed. A window without a saved position opens as its XAML says.
/// Each time a window is shown, or its monitor's scaling changes its size, it is moved back
/// inside the screen if part of it is off.
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
    /// </summary>
    public void Attach(Window window, string name, RestoreMode mode)
    {
        ScreenRect? lastNormal = null;

        var saved = _store.Get(name);
        if (mode == RestoreMode.SizeOverOwner)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.SourceInitialized += (_, _) => PlaceOverOwner(window, name, saved);
        }
        else if (saved is not null)
        {
            // Otherwise WPF centers the window once it is shown, over the restored position.
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.SourceInitialized += (_, _) => Restore(window, name, saved, keepSize: mode == RestoreMode.PositionAndSize);
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
            else
            {
                // Once WPF has finished sizing the window for its monitor.
                window.Dispatcher.BeginInvoke(() => KeepOnScreen(window), DispatcherPriority.Loaded);
            }
        };
        window.DpiChanged += (_, _) => window.Dispatcher.BeginInvoke(() => KeepOnScreen(window), DispatcherPriority.Loaded);
        window.Closing += (_, _) => Save(window, name, lastNormal);
    }

    /// <summary>
    /// Moves a window that is partly off screen (dragged past an edge, grown with the monitor's
    /// scaling, monitor gone) back inside the work area of the monitor showing most of it.
    /// A maximized or minimized window is left alone.
    /// </summary>
    public static void KeepOnScreen(Window window)
    {
        var handle = Handle(window);
        if (!window.IsVisible
            || window.WindowState != WindowState.Normal
            || NativeWindows.WindowBounds(handle) is not { } bounds
            || NativeWindows.VisibleBounds(handle) is not { } frame
            || PlacementRules.KeepVisible(frame, NativeWindows.Monitors()) is not { } visible
            || visible == frame)
        {
            return;
        }

        // The check is on the visible frame (a window snapped to an edge is fine); the move is
        // on the whole window, invisible borders included.
        NativeWindows.SetWindowBounds(handle, new ScreenRect(
            bounds.Left + (visible.Left - frame.Left),
            bounds.Top + (visible.Top - frame.Top),
            bounds.Width + (visible.Width - frame.Width),
            bounds.Height + (visible.Height - frame.Height)));
    }

    /// <summary>
    /// Opens a dialog centered on its owner, at its saved size (else its XAML size), and fully on
    /// screen. Without an owner, it is centered on the screen like any window.
    /// </summary>
    private void PlaceOverOwner(Window window, string name, WindowPlacement? saved)
    {
        var handle = Handle(window);
        if (window.Owner is not { } owner
            || NativeWindows.VisibleBounds(Handle(owner)) is not { } ownerBounds
            || NativeWindows.WindowBounds(handle) is not { } current)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        var width = saved?.Bounds.Width ?? current.Width;
        var height = saved?.Bounds.Height ?? current.Height;
        var centered = PlacementRules.CenterOn(ownerBounds, width, height);
        var bounds = PlacementRules.KeepVisible(centered, NativeWindows.Monitors()) ?? centered;

        // Set twice: on a monitor with another scaling, the first move makes WPF resize the window.
        NativeWindows.SetWindowBounds(handle, bounds);
        NativeWindows.SetWindowBounds(handle, bounds);
        _log.Debug("Window", $"{name} opened over its owner at {bounds.Left},{bounds.Top} ({bounds.Width}x{bounds.Height})");
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
