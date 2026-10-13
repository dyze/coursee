using System.Text.Json;

namespace Coursee.Core.Entries;

/// <summary>
/// What the user set on entries, persisted as JSON: which ones are pinned. An entry is known by
/// its target, like in the recent list, so it keeps its pin when a shortcut is renamed, and gets
/// it back when its source is switched on again. A missing or corrupt file means nothing is set.
/// </summary>
public sealed class EntryStore
{
    private readonly string _path;
    private readonly HashSet<string> _pinned;

    public EntryStore(string path)
    {
        _path = path;

        var data = Load(path);
        _pinned = new HashSet<string>(data?.Pinned ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public bool IsPinned(string target) => _pinned.Contains(target);

    /// <summary>Pins the entry with this target, or unpins it. Returns true when it is now pinned.</summary>
    public bool TogglePin(string target)
    {
        if (!_pinned.Remove(target))
        {
            _pinned.Add(target);
        }

        Save();
        return IsPinned(target);
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new EntryData(_pinned.Order(StringComparer.OrdinalIgnoreCase).ToList());
        File.WriteAllText(_path, JsonSerializer.Serialize(data));
    }

    private static EntryData? Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<EntryData>(File.ReadAllText(path))
                : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal sealed record EntryData(List<string>? Pinned);
}
