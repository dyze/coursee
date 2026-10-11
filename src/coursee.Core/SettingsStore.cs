using System.Text.Json;

namespace Coursee.Core;

/// <summary>
/// User settings persisted as JSON: which sources are disabled (every source is on by default),
/// the global hotkey and how many recent items to keep. A missing, corrupt or partial file
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
    }

    /// <summary>The values offered in Settings for the number of recent items to keep.</summary>
    public static IReadOnlyList<int> RecentsCapacityChoices { get; } = [5, 10, 20, 30, 50];

    public HotkeyGesture Hotkey { get; private set; }

    public int RecentsCapacity { get; private set; }

    /// <summary>When on, the log also records detailed (debug) entries. Off by default.</summary>
    public bool DebugLogging { get; private set; }

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
            DebugLogging);
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

    internal sealed record SettingsData(List<string>? DisabledSources, string? Hotkey, int? RecentsCapacity, bool? DebugLogging = null);
}
