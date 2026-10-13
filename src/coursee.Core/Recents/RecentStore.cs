using System.Text.Json;
using Coursee.Core.Resources;

namespace Coursee.Core.Recents;

/// <summary>
/// Resources opened through coursee, with the last times each was opened, persisted as JSON.
/// The list keeps the most recently opened ones (the oldest go first when it is full) and is
/// shown ordered by <see cref="Frecency"/>: recency plus frequency. An entry is known by its
/// target. A missing or corrupt file simply means an empty list.
/// </summary>
public sealed class RecentStore
{
    private readonly string _path;
    private readonly TimeProvider _clock;

    /// <summary>Most recently opened first.</summary>
    private readonly List<RecentItem> _items;
    private int _capacity;

    public RecentStore(string path, int capacity = 20, TimeProvider? clock = null)
    {
        _path = path;
        _capacity = capacity;
        _clock = clock ?? TimeProvider.System;
        _items = Load(path, capacity, _clock.GetUtcNow());
    }

    /// <summary>The recent list: the most used first (recent opens count more), then the last opened.</summary>
    public IReadOnlyList<Resource> Items
    {
        get
        {
            var now = _clock.GetUtcNow();
            return _items
                .Select((item, index) => (item.Resource, Score: Frecency.Score(item.Opens, now), Index: index))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Index)
                .Select(x => x.Resource)
                .ToList();
        }
    }

    /// <summary>The search bonus of the entry with this target, 0 when it is not in the list.</summary>
    public int UsageBonus(string target) =>
        Find(target) is { } item ? Frecency.Bonus(item.Opens, _clock.GetUtcNow()) : 0;

    /// <summary>Records that the resource was opened now. An entry with the same target keeps its earlier opens.</summary>
    public void Add(Resource resource)
    {
        var opens = Find(resource.Target)?.Opens ?? [];
        opens = [.. opens.TakeLast(Frecency.MaxOpens - 1), _clock.GetUtcNow()];

        _items.RemoveAll(r => SameTarget(r.Resource.Target, resource.Target));
        _items.Insert(0, new RecentItem(resource, opens));
        Trim();
        Save();
    }

    /// <summary>
    /// Removes the item with this target, with its open history. Returns false (and saves nothing)
    /// when it is not in the list.
    /// </summary>
    public bool Remove(string target)
    {
        if (_items.RemoveAll(r => SameTarget(r.Resource.Target, target)) == 0)
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Changes how many items are kept; the least recently opened ones beyond the new limit are dropped.</summary>
    public void SetCapacity(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        Trim();
        Save();
    }

    private RecentItem? Find(string target) => _items.Find(r => SameTarget(r.Resource.Target, target));

    private static bool SameTarget(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

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

        File.WriteAllText(_path, JsonSerializer.Serialize(new RecentData(_items)));
    }

    private static List<RecentItem> Load(string path, int capacity, DateTimeOffset now)
    {
        try
        {
            if (!File.Exists(path))
            {
                return [];
            }

            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            var items = document.RootElement.ValueKind == JsonValueKind.Array
                ? FromVersion04(JsonSerializer.Deserialize<List<Resource>>(json), now)
                : JsonSerializer.Deserialize<RecentData>(json)?.Items;

            return items?
                .Where(item => item.Resource is not null)
                .Select(item => item with { Opens = item.Opens ?? [] })
                .Take(capacity)
                .ToList() ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// Up to 0.4 the file was the plain list of resources, most recent first, without open times:
    /// each one counts as opened once, a second apart, so the order is kept.
    /// </summary>
    private static List<RecentItem>? FromVersion04(List<Resource>? resources, DateTimeOffset now) =>
        resources?
            .Select((resource, index) => new RecentItem(resource, [now.AddSeconds(-index)]))
            .ToList();

    /// <param name="Opens">When it was opened, oldest first, at most <see cref="Frecency.MaxOpens"/>.</param>
    internal sealed record RecentItem(Resource Resource, List<DateTimeOffset> Opens);

    internal sealed record RecentData(List<RecentItem>? Items);
}
