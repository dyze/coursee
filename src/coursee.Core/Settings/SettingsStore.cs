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
/// the folders searched (see <see cref="Folders"/>), the global hotkey, how many recent items to keep, the theme, where the launcher appears and whether
/// searching ignores accents. A missing, corrupt or partial file
/// means defaults for whatever cannot be read.
/// </summary>
public sealed class SettingsStore
{
    /// <summary>The Desktop in <see cref="Folders"/>: both Desktop folders (personal and common).</summary>
    public const string DesktopFolder = "Desktop";

    public const int DefaultRecentsCapacity = 20;
    private const int MinRecentsCapacity = 1;
    private const int MaxRecentsCapacity = 100;

    private readonly string _path;
    private readonly HashSet<string> _disabledSources;
    private readonly List<string> _folders;

    public SettingsStore(string path)
    {
        _path = path;

        var data = Load(path);
        _disabledSources = new HashSet<string>(data?.DisabledSources ?? [], StringComparer.OrdinalIgnoreCase);
        _folders = (data?.Folders ?? [DesktopFolder])
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    /// <summary>
    /// The folders the user added as sources, in the order added: <see cref="DesktopFolder"/> or a
    /// path. Only the Desktop by default; it can be removed like the others.
    /// </summary>
    public IReadOnlyList<string> Folders => _folders.ToList();

    /// <summary>Adds a folder at the end. False when it is already there (ignoring case).</summary>
    public bool AddFolder(string folder)
    {
        if (_folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        _folders.Add(folder);
        Save();
        return true;
    }

    /// <summary>False when the folder was not there.</summary>
    public bool RemoveFolder(string folder)
    {
        if (_folders.RemoveAll(existing => string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            return false;
        }

        Save();
        return true;
    }

    public bool IsSourceEnabled(string sourceName) => !_disabledSources.Contains(sourceName);

    /// <summary>The state of a group of sources: true when all are on, false when none is, null when some are.</summary>
    public bool? AreSourcesEnabled(IEnumerable<string> sourceNames)
    {
        var states = sourceNames.Select(IsSourceEnabled).Distinct().ToList();
        return states.Count switch
        {
            0 => true,
            1 => states[0],
            _ => null,
        };
    }

    public void SetSourceEnabled(string sourceName, bool enabled) => SetSourcesEnabled([sourceName], enabled);

    /// <summary>Switches several sources at once (a group in Settings), saving once.</summary>
    public void SetSourcesEnabled(IEnumerable<string> sourceNames, bool enabled)
    {
        var changed = false;
        foreach (var name in sourceNames)
        {
            changed |= enabled ? _disabledSources.Remove(name) : _disabledSources.Add(name);
        }

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
            IgnoreAccents,
            _folders);
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
        bool? IgnoreAccents = null,
        List<string>? Folders = null);
}
