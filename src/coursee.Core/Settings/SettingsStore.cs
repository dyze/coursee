using System.Text.Json;
using Coursee.Core.Platform;

namespace Coursee.Core.Settings;

/// <summary>The look of the windows. Follows the Windows setting by default.</summary>
public enum ThemeChoice
{
    System,
    Light,
    Dark,
}

/// <summary>Where the launcher appears each time it is shown. Always kept fully on screen.</summary>
public enum LauncherPosition
{
    /// <summary>In the middle of the monitor under the mouse.</summary>
    Centered,

    /// <summary>Centered on the mouse pointer.</summary>
    AtMouse,

    /// <summary>Where it was left (dragged), on the same monitor.</summary>
    LastPosition,
}

/// <summary>
/// User settings persisted as JSON: which sources are disabled (every source is on by default),
/// the global hotkey, how many recent items to keep, the theme, where the launcher appears and whether
/// searching ignores accents. A missing, corrupt or partial file
/// means defaults for whatever cannot be read.
/// </summary>
public sealed class SettingsStore
{
    public const int DefaultRecentsCapacity = 20;
    private const int MinRecentsCapacity = 1;
    private const int MaxRecentsCapacity = 100;

    private readonly string _path;
    private readonly HashSet<string> _disabledSources;

    public SettingsStore(string path)
    {
        _path = path;

        var data = Load(path);
        _disabledSources = new HashSet<string>(data?.DisabledSources ?? [], StringComparer.OrdinalIgnoreCase);
        Hotkey = HotkeyGesture.TryParse(data?.Hotkey, out var gesture) ? gesture : HotkeyGesture.Default;
        RecentsCapacity = (data?.RecentsCapacity is int capacity and >= MinRecentsCapacity and <= MaxRecentsCapacity)
            ? capacity
            : DefaultRecentsCapacity;
        DebugLogging = data?.DebugLogging ?? false;
        Theme = Enum.TryParse<ThemeChoice>(data?.Theme, ignoreCase: true, out var theme) && Enum.IsDefined(theme)
            ? theme
            : ThemeChoice.System;
        LauncherPosition = Enum.TryParse<LauncherPosition>(data?.LauncherPosition, ignoreCase: true, out var position) && Enum.IsDefined(position)
            ? position
            : LauncherPosition.Centered;
        IgnoreAccents = data?.IgnoreAccents ?? true;
    }

    /// <summary>The values offered in Settings for the number of recent items to keep.</summary>
    public static IReadOnlyList<int> RecentsCapacityChoices { get; } = [5, 10, 20, 30, 50];

    public HotkeyGesture Hotkey { get; private set; }

    public int RecentsCapacity { get; private set; }

    /// <summary>When on, the log also records detailed (debug) entries. Off by default.</summary>
    public bool DebugLogging { get; private set; }

    public ThemeChoice Theme { get; private set; }

    public LauncherPosition LauncherPosition { get; private set; }

    /// <summary>When on, searching ignores accents (<c>evene</c> finds <c>Événements</c>). On by default.</summary>
    public bool IgnoreAccents { get; private set; }

    public bool IsSourceEnabled(string sourceName) => !_disabledSources.Contains(sourceName);

    public void SetSourceEnabled(string sourceName, bool enabled)
    {
        var changed = enabled ? _disabledSources.Remove(sourceName) : _disabledSources.Add(sourceName);
        if (changed)
        {
            Save();
        }
    }

    public void SetHotkey(HotkeyGesture gesture)
    {
        if (!gesture.IsValid)
        {
            throw new ArgumentException("A hotkey needs a key and at least one modifier.", nameof(gesture));
        }

        Hotkey = gesture;
        Save();
    }

    public void SetRecentsCapacity(int capacity)
    {
        RecentsCapacity = Math.Clamp(capacity, MinRecentsCapacity, MaxRecentsCapacity);
        Save();
    }

    public void SetDebugLogging(bool enabled)
    {
        DebugLogging = enabled;
        Save();
    }

    public void SetTheme(ThemeChoice theme)
    {
        Theme = theme;
        Save();
    }

    public void SetLauncherPosition(LauncherPosition position)
    {
        LauncherPosition = position;
        Save();
    }

    public void SetIgnoreAccents(bool ignore)
    {
        IgnoreAccents = ignore;
        Save();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new SettingsData(
            _disabledSources.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Hotkey.ToString(),
            RecentsCapacity,
            DebugLogging,
            Theme.ToString(),
            LauncherPosition.ToString(),
            IgnoreAccents);
        File.WriteAllText(_path, JsonSerializer.Serialize(data));
    }

    private static SettingsData? Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path))
                : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal sealed record SettingsData(
        List<string>? DisabledSources,
        string? Hotkey,
        int? RecentsCapacity,
        bool? DebugLogging = null,
        string? Theme = null,
        string? LauncherPosition = null,
        bool? IgnoreAccents = null);
}
