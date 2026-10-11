using System.Text.Json;

namespace Coursee.Core;

/// <summary>
/// Resources opened through coursee, most recent first, persisted as JSON.
/// A missing or corrupt file simply means an empty list.
/// </summary>
public sealed class RecentStore
{
    private readonly string _path;
    private readonly int _capacity;
    private readonly List<Resource> _items;

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

        if (_items.Count > _capacity)
        {
            _items.RemoveRange(_capacity, _items.Count - _capacity);
        }

        Save();
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
