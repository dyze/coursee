using System.Diagnostics;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>What one source returned during a refresh, or why it failed, and how long it took.</summary>
public sealed record SourceResult(string SourceName, IReadOnlyList<Resource> Resources, Exception? Error = null, TimeSpan Duration = default);

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
            var watch = Stopwatch.StartNew();
            try
            {
                var resources = source.GetResources();
                results.Add(new SourceResult(source.Name, resources, Duration: watch.Elapsed));
            }
            catch (Exception ex)
            {
                results.Add(new SourceResult(source.Name, [], ex, watch.Elapsed));
            }
        }

        return results;
    }

    /// <summary>
    /// Joins the resources of several sources, in order. An item with the same name and target
    /// as an earlier one (for example a program on both the Desktop and the Start Menu) is
    /// listed once: the first source wins. Items with the same target but different names stay,
    /// since they are usually different shortcuts (two browser profiles, say).
    /// An app or a game is also left out when an earlier item has its name: its target (an app ID,
    /// a launcher link) never equals the one of the shortcut that the Start Menu has for it.
    /// </summary>
    public static IReadOnlyList<Resource> Merge(IEnumerable<IReadOnlyList<Resource>> lists)
    {
        var seen = new HashSet<(string, string)>(NameTargetComparer.Instance);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<Resource>();
        foreach (var resource in lists.SelectMany(list => list))
        {
            var listedByName = resource.Kind is ResourceKind.App or ResourceKind.Game && names.Contains(resource.Name);
            if (seen.Add((resource.Name, resource.Target)) && !listedByName)
            {
                names.Add(resource.Name);
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
