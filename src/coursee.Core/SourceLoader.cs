namespace Coursee.Core;

/// <summary>What one source returned during a refresh, or why it failed.</summary>
public sealed record SourceResult(string SourceName, IReadOnlyList<Resource> Resources, Exception? Error = null);

/// <summary>
/// Reads every source once. Meant to run on a background thread, so the launcher window
/// opens instantly while slow sources (network shortcuts, a large Firefox database) load.
/// A failing source never affects the others. Logging and change tracking are left to the
/// caller, which does them on the UI thread.
/// </summary>
public static class SourceLoader
{
    public static IReadOnlyList<SourceResult> Load(IEnumerable<IResourceSource> sources)
    {
        var results = new List<SourceResult>();
        foreach (var source in sources)
        {
            try
            {
                results.Add(new SourceResult(source.Name, source.GetResources()));
            }
            catch (Exception ex)
            {
                results.Add(new SourceResult(source.Name, [], ex));
            }
        }

        return results;
    }

    /// <summary>
    /// Joins the resources of several sources, in order. An item with the same name and target
    /// as an earlier one (for example a program on both the Desktop and the Start Menu) is
    /// listed once: the first source wins. Items with the same target but different names stay,
    /// since they are usually different shortcuts (two browser profiles, say).
    /// </summary>
    public static IReadOnlyList<Resource> Merge(IEnumerable<IReadOnlyList<Resource>> lists)
    {
        var seen = new HashSet<(string, string)>(NameTargetComparer.Instance);
        var merged = new List<Resource>();
        foreach (var resource in lists.SelectMany(list => list))
        {
            if (seen.Add((resource.Name, resource.Target)))
            {
                merged.Add(resource);
            }
        }

        return merged;
    }

    private sealed class NameTargetComparer : IEqualityComparer<(string Name, string Target)>
    {
        public static NameTargetComparer Instance { get; } = new();

        public bool Equals((string Name, string Target) x, (string Name, string Target) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name)
            && StringComparer.OrdinalIgnoreCase.Equals(x.Target, y.Target);

        public int GetHashCode((string Name, string Target) value) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Name),
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Target));
    }
}
