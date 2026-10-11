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
}
