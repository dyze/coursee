using System.Text.Json;
using Coursee.Core.Resources;

namespace Coursee.Core.Recents;

/// <summary>
/// Resources opened through coursee, most recent first, persisted as JSON.
/// A missing or corrupt file simply means an empty list.
/// </summary>
public sealed class RecentStore
{
    private readonly string _path;
    private readonly List<Resource> _items;
    private int _capacity;

    public RecentStore(string path, int capacity = 20)
    {
        _path = path;
        _capacity = capacity;
        _items = Load(path, capacity);
    }

    public IReadOnlyList<Resource> Items => _items.ToArray();

    public void Add(Resource resource)
    {
        _items.RemoveAll(r => string.Equals(r.Target, resource.Target, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, resource);
        Trim();
        Save();
    }

    /// <summary>Removes the item with this target. Returns false (and saves nothing) when it is not in the list.</summary>
    public bool Remove(string target)
    {
        if (_items.RemoveAll(r => string.Equals(r.Target, target, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Changes how many items are kept; the oldest ones beyond the new limit are dropped.</summary>
    public void SetCapacity(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        Trim();
        Save();
    }

    private void Trim()
    {
        if (_items.Count > _capacity)
        {
            _items.RemoveRange(_capacity, _items.Count - _capacity);
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(_items));
    }

    private static List<Resource> Load(string path, int capacity)
    {
        try
        {
            if (!File.Exists(path))
            {
                return [];
            }

            var items = JsonSerializer.Deserialize<List<Resource>>(File.ReadAllText(path));
            return items?.Take(capacity).ToList() ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
