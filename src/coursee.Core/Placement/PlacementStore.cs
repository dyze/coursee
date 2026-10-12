using System.Text.Json;

namespace Coursee.Core.Placement;

/// <summary>
/// Where each window was left, by window name, persisted as JSON. Kept apart from the
/// settings: it is state, not a choice of the user. A missing or corrupt file means no
/// saved position (the windows open centered).
/// </summary>
public sealed class PlacementStore
{
    private readonly string _path;
    private readonly Dictionary<string, WindowPlacement> _placements;

    public PlacementStore(string path)
    {
        _path = path;
        _placements = Load(path);
    }

    public WindowPlacement? Get(string windowName) => _placements.GetValueOrDefault(windowName);

    /// <summary>Remembers where a window is, and saves the file when it changed.</summary>
    public void Set(string windowName, WindowPlacement placement)
    {
        if (_placements.TryGetValue(windowName, out var previous) && previous == placement)
        {
            return;
        }

        _placements[windowName] = placement;

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(_placements));
    }

    private static Dictionary<string, WindowPlacement> Load(string path)
    {
        try
        {
            var loaded = File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, WindowPlacement>>(File.ReadAllText(path))
                : null;

            // A hand-edited entry may lack a part; such an entry is dropped.
            return new Dictionary<string, WindowPlacement>(
                (loaded ?? []).Where(pair => pair.Value?.Monitor is not null && pair.Value.Bounds is not null),
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new Dictionary<string, WindowPlacement>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
