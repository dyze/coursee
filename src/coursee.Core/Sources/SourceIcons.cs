using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// The icon of each source (a Segoe Fluent Icons glyph) and the icon of a row, found from the
/// name of the source its resource comes from. Recents keep that name, so a recent item shows
/// the icon of its source too.
/// </summary>
public sealed class SourceIcons
{
    public const string Desktop = "";        // TVMonitor
    public const string StartMenu = "";      // Tiles
    public const string Firefox = "";        // FavoriteList
    public const string Edge = "";           // FavoriteStar
    public const string RunHistory = "";     // History
    public const string WindowsSettings = ""; // System

    /// <summary>Text typed in the search box (Enter acts on it).</summary>
    public const string Typed = "";          // KeyboardClassic

    private readonly Dictionary<string, (string Icon, string Label)> _bySource = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="sources">All the sources, enabled or not: a recent item may come from one that is off.</param>
    public SourceIcons(IEnumerable<IResourceSource> sources)
    {
        foreach (var source in sources)
        {
            _bySource.TryAdd(source.Name, (source.Icon, source.DisplayName));
        }

        _bySource.TryAdd(FallbackClassifier.SourceName, (Typed, "What you typed"));
    }

    /// <summary>The icon of a source; empty for an unknown one (such as a source that was removed).</summary>
    public string IconOf(string sourceName) =>
        _bySource.TryGetValue(sourceName, out var entry) ? entry.Icon : string.Empty;

    /// <summary>The name of a source as shown to the user (the tooltip of its icon); the name itself for an unknown one.</summary>
    public string LabelOf(string sourceName) =>
        _bySource.TryGetValue(sourceName, out var entry) ? entry.Label : sourceName;
}
