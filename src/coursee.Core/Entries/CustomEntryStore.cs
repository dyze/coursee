using System.Text.Json;

namespace Coursee.Core.Entries;

/// <summary>
/// The custom entries, persisted as JSON, in the order they were made. Each target is used once
/// (ignoring case): an entry is known by its target. Read by the Custom source on a background
/// thread while the UI may change the list, hence the lock. A missing or corrupt file means none.
/// </summary>
public sealed class CustomEntryStore
{
    private readonly string _path;
    private readonly object _lock = new();
    private readonly List<CustomEntry> _entries;

    public CustomEntryStore(string path)
    {
        _path = path;
        _entries = [];
        foreach (var entry in Load(path) ?? [])
        {
            // A file edited by hand may hold an incomplete entry or repeat a target: skipped.
            if (entry is { Name: not null, Target: not null }
                && entry.Target.Trim().Length > 0
                && IndexOf(entry.Target.Trim()) < 0)
            {
                _entries.Add(Cleaned(entry.Name, entry.Target));
            }
        }
    }

    /// <summary>A copy of the entries, safe to read on any thread.</summary>
    public IReadOnlyList<CustomEntry> All
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    /// <summary>
    /// Adds an entry, or renames the one with this target. Spaces around the name and target are
    /// removed; an empty name becomes the target.
    /// </summary>
    /// <exception cref="ArgumentException">The target is empty.</exception>
    public CustomEntry Add(string name, string target)
    {
        var entry = Cleaned(name, target);
        lock (_lock)
        {
            var index = IndexOf(entry.Target);
            if (index < 0)
            {
                _entries.Add(entry);
            }
            else
            {
                _entries[index] = entry;
            }

            Save();
        }

        return entry;
    }

    /// <summary>
    /// Replaces <paramref name="entry"/> with a new name and target, at its place in the list.
    /// Returns the new entry, or null (and changes nothing) when the entry is gone or another
    /// entry has the new target.
    /// </summary>
    /// <exception cref="ArgumentException">The target is empty.</exception>
    public CustomEntry? Update(CustomEntry entry, string name, string target)
    {
        var updated = Cleaned(name, target);
        lock (_lock)
        {
            var index = IndexOf(entry.Target);
            var holder = IndexOf(updated.Target);
            if (index < 0 || (holder >= 0 && holder != index))
            {
                return null;
            }

            _entries[index] = updated;
            Save();
        }

        return updated;
    }

    /// <summary>Removes the entry with this one's target. Returns false (and saves nothing) when there is none.</summary>
    public bool Delete(CustomEntry entry)
    {
        lock (_lock)
        {
            var index = IndexOf(entry.Target);
            if (index < 0)
            {
                return false;
            }

            _entries.RemoveAt(index);
            Save();
        }

        return true;
    }

    private int IndexOf(string target) =>
        _entries.FindIndex(entry => string.Equals(entry.Target, target, StringComparison.OrdinalIgnoreCase));

    private static CustomEntry Cleaned(string name, string target)
    {
        var cleanedTarget = target.Trim();
        if (cleanedTarget.Length == 0)
        {
            throw new ArgumentException("A custom entry needs a target.", nameof(target));
        }

        var cleanedName = name.Trim();
        return new CustomEntry(cleanedName.Length == 0 ? cleanedTarget : cleanedName, cleanedTarget);
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(_entries));
    }

    private static List<CustomEntry>? Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<List<CustomEntry>>(File.ReadAllText(path))
                : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
