using System.Diagnostics;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>What one source returned during a refresh, or why it failed, and how long it took.</summary>
public sealed record SourceResult(string SourceName, IReadOnlyList<Resource> Resources, Exception? Error = null, TimeSpan Duration = default);

/// <summary>
/// Reads every source once. Meant to run on a background thread, so the launcher window
/// opens instantly while slow sources (network shortcuts, a large Firefox database) load.
/// The sources are read in parallel, so a refresh takes as long as the slowest one, not their sum.
/// A failing source never affects the others. Logging and change tracking are left to the
/// caller, which does them on the UI thread.
/// </summary>
public static class SourceLoader
{
    /// <summary>The results, in the order of <paramref name="sources"/>.</summary>
    public static IReadOnlyList<SourceResult> Load(IEnumerable<IResourceSource> sources)
    {
        var list = sources.ToList();
        var results = new SourceResult[list.Count];
        Parallel.For(0, list.Count, index => results[index] = LoadOne(list[index]));
        return results;
    }

    private static SourceResult LoadOne(IResourceSource source)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return new SourceResult(source.Name, source.GetResources(), Duration: watch.Elapsed);
        }
        catch (Exception ex)
        {
            return new SourceResult(source.Name, [], ex, watch.Elapsed);
        }
    }

    /// <summary>
    /// Joins the resources of several sources, in order. An item with the same name and target
    /// as an earlier one (for example a program on both the Desktop and the Start Menu) is
    /// listed once: the first source wins. Items with the same target but different names stay,
    /// since they are usually different shortcuts (two browser profiles, say).
    /// An app or a game is also left out when an earlier item has its name: its target (an app ID,
    /// a launcher link) never equals the one of the shortcut that the Start Menu has for it.
    /// A game takes the place of that shortcut, though, so that it is listed as a game.
    /// </summary>
    public static IReadOnlyList<Resource> Merge(IEnumerable<IReadOnlyList<Resource>> lists)
    {
        var seen = new HashSet<(string, string)>(NameTargetComparer.Instance);
        var firstByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<Resource>();
        foreach (var resource in lists.SelectMany(list => list))
        {
            if (!seen.Add((resource.Name, resource.Target)))
            {
                continue;
            }

            if (resource.Kind is ResourceKind.App or ResourceKind.Game && firstByName.TryGetValue(resource.Name, out var earlier))
            {
                if (resource.Kind == ResourceKind.Game && IsShortcut(merged[earlier]))
                {
                    merged[earlier] = resource;
                }

                continue;
            }

            firstByName.TryAdd(resource.Name, merged.Count);
            merged.Add(resource);
        }

        return merged;
    }

    /// <summary>A .lnk file of the Desktop, the Start Menu or a folder: opened through its file.</summary>
    private static bool IsShortcut(Resource resource) =>
        resource.Kind == ResourceKind.File && !string.IsNullOrWhiteSpace(resource.LaunchPath);

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
