using Coursee.Core.Logging;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// Reads the raw values of the Windows Run dialog history (the per-user <c>RunMRU</c> registry key):
/// an <c>MRUList</c> value giving the order, and one value per letter holding a command.
/// </summary>
public interface IRunHistoryReader
{
    IReadOnlyDictionary<string, string> Read();
}

/// <summary>
/// The commands previously typed in the Windows Run dialog (Win+R), most recent first,
/// so they can be found from coursee while migrating away from Win+R.
/// </summary>
public sealed class RunHistorySource : IResourceSource
{
    public const string SourceName = "Windows Run";

    private const string OrderValue = "MRUList";
    private const string EntrySuffix = "\\1";

    private readonly IRunHistoryReader _reader;
    private readonly ILog _log;

    public RunHistorySource(IRunHistoryReader reader, ILog? log = null)
    {
        _reader = reader;
        _log = log ?? NullLog.Instance;
    }

    public string Name => SourceName;

    public string DisplayName => "Windows Run history (Win+R)";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.RunHistory;

    public IReadOnlyList<Resource> GetResources()
    {
        IReadOnlyDictionary<string, string> values;
        try
        {
            values = _reader.Read();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            _log.Warn(Name, $"Cannot read the Run history: {ex.GetType().Name}: {ex.Message}");
            return [];
        }

        if (!values.TryGetValue(OrderValue, out var order))
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>();

        foreach (var letter in order)
        {
            if (!values.TryGetValue(letter.ToString(), out var raw))
            {
                continue;
            }

            var command = Clean(raw);
            if (command.Length == 0 || !seen.Add(command))
            {
                continue;
            }

            // No file system checks here: this runs every time the window opens, and a
            // disconnected network path could block it. Anything that is not a URL is a command.
            var classified = FallbackClassifier.Classify(command, _ => false, _ => false)!;
            resources.Add(classified with { Source = Name });
        }

        return resources;
    }

    /// <summary>Run stores each command followed by the two characters "\1".</summary>
    private static string Clean(string raw)
    {
        var text = raw.Trim();
        if (text.EndsWith(EntrySuffix, StringComparison.Ordinal))
        {
            text = text[..^EntrySuffix.Length];
        }

        return text.Trim();
    }
}
