using Coursee.Core.Resources;

namespace Coursee.Core.Recents;

/// <summary>
/// The recent list as the sources list its items now. <see cref="RecentStore"/> keeps a copy of
/// each item as it was opened: a shortcut renamed since, or an item saved by an older version
/// (a game without its icon path), shows as it is now.
/// </summary>
public static class CurrentRecents
{
    /// <summary>
    /// Each of <paramref name="recents"/>, in its order, replaced by the item of <paramref name="loaded"/>
    /// with its source and target (ignoring case); kept as saved when the sources no longer list it.
    /// </summary>
    public static IReadOnlyList<Resource> Of(IReadOnlyList<Resource> recents, IReadOnlyList<Resource> loaded)
    {
        var current = new Dictionary<(string Source, string Target), Resource>(KeyComparer.Instance);
        foreach (var resource in loaded)
        {
            current.TryAdd((resource.Source, resource.Target), resource);
        }

        return recents.Select(recent => current.GetValueOrDefault((recent.Source, recent.Target), recent)).ToList();
    }

    private sealed class KeyComparer : IEqualityComparer<(string Source, string Target)>
    {
        public static readonly KeyComparer Instance = new();

        public bool Equals((string Source, string Target) x, (string Source, string Target) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Source, y.Source) && StringComparer.OrdinalIgnoreCase.Equals(x.Target, y.Target);

        public int GetHashCode((string Source, string Target) key) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(key.Source), StringComparer.OrdinalIgnoreCase.GetHashCode(key.Target));
    }
}
