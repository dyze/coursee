using Coursee.Core.Logging;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// Logs what changed in each resource source between two refreshes
/// (items added or removed), so the log shows how the sources evolve.
/// </summary>
public sealed class SourceMonitor
{
    private const int MaxListed = 5;

    private readonly ILog _log;
    private readonly Dictionary<string, Dictionary<string, string>> _previous =
        new(StringComparer.OrdinalIgnoreCase);

    public SourceMonitor(ILog log)
    {
        _log = log;
    }

    /// <summary>Forgets what was seen for a source (for example when it was switched off).</summary>
    public void Reset(string sourceName) => _previous.Remove(sourceName);

    /// <summary>Compares the source's current resources with the previous refresh and logs the difference.</summary>
    public void Observe(string sourceName, IReadOnlyList<Resource> resources)
    {
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in resources)
        {
            current.TryAdd(resource.Target, resource.Name);
        }

        if (!_previous.TryGetValue(sourceName, out var previous))
        {
            _log.Info(sourceName, $"{current.Count} items loaded");
        }
        else
        {
            var added = current.Where(pair => !previous.ContainsKey(pair.Key)).Select(pair => pair.Value).ToList();
            var removed = previous.Where(pair => !current.ContainsKey(pair.Key)).Select(pair => pair.Value).ToList();

            if (added.Count == 0 && removed.Count == 0)
            {
                _log.Debug(sourceName, $"No change ({current.Count} items)");
            }
            else
            {
                var parts = new List<string> { $"{current.Count} items (+{added.Count} -{removed.Count})" };
                if (added.Count > 0)
                {
                    parts.Add("added: " + Describe(added));
                }

                if (removed.Count > 0)
                {
                    parts.Add("removed: " + Describe(removed));
                }

                _log.Info(sourceName, string.Join("; ", parts));
            }
        }

        _previous[sourceName] = current;
    }

    private static string Describe(List<string> names)
    {
        var shown = string.Join(", ", names.Take(MaxListed).Select(name => $"'{name}'"));
        return names.Count > MaxListed ? $"{shown} and {names.Count - MaxListed} more" : shown;
    }
}
