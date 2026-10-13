using System.Text.Json;
using Coursee.Core.Search;

namespace Coursee.Core.Entries;

/// <summary>
/// What the user set on entries, persisted as JSON: which ones are pinned, and their aliases.
/// An entry is known by its target, like in the recent list, so it keeps its pin and alias when
/// a shortcut is renamed, and gets them back when its source is switched on again. A missing or
/// corrupt file means nothing is set.
/// </summary>
public sealed class EntryStore
{
    private readonly string _path;
    private readonly HashSet<string> _pinned;

    /// <summary>Alias by target: an entry has at most one alias, and an alias belongs to one entry.</summary>
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    public EntryStore(string path)
    {
        _path = path;

        var data = Load(path);
        _pinned = new HashSet<string>(data?.Pinned ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var (alias, target) in data?.Aliases ?? new Dictionary<string, string>())
        {
            // A file edited by hand may repeat an alias (ignoring case or accents) or a target: the first one wins.
            var cleaned = alias.Trim();
            if (cleaned.Length > 0 && TargetOfAlias(cleaned) is null && !_aliases.ContainsKey(target))
            {
                _aliases[target] = cleaned;
            }
        }
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

    /// <summary>The alias of the entry with this target, or null.</summary>
    public string? AliasOf(string target) => _aliases.GetValueOrDefault(target);

    /// <summary>The target of the entry whose alias is this text (ignoring case, accents and surrounding spaces), or null.</summary>
    public string? TargetOfAlias(string text)
    {
        var alias = text.Trim();
        return alias.Length == 0
            ? null
            : _aliases.FirstOrDefault(pair => TextFolding.AreEqual(pair.Value, alias)).Key;
    }

    /// <summary>
    /// Gives the entry with this target its alias, replacing the one it had. An alias already used
    /// by another entry moves to this one. An empty alias removes the entry's alias.
    /// </summary>
    /// <returns>The target of the entry the alias was taken from, or null.</returns>
    public string? SetAlias(string target, string alias)
    {
        var cleaned = alias.Trim();
        string? movedFrom = null;
        if (cleaned.Length == 0)
        {
            _aliases.Remove(target);
        }
        else
        {
            var holder = TargetOfAlias(cleaned);
            if (holder is not null && !string.Equals(holder, target, StringComparison.OrdinalIgnoreCase))
            {
                _aliases.Remove(holder);
                movedFrom = holder;
            }

            _aliases[target] = cleaned;
        }

        Save();
        return movedFrom;
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new EntryData(
            _pinned.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            _aliases
                .OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(pair => pair.Value, pair => pair.Key));
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

    /// <param name="Aliases">Target by alias, as the user reads it.</param>
    internal sealed record EntryData(List<string>? Pinned, Dictionary<string, string>? Aliases = null);
}
