using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// The icon of each source (the name of one of <see cref="Icons.IconLibrary"/>'s) and the icon of
/// a row, found from the name of the source its resource comes from. Recents keep that name, so a
/// recent item shows the icon of its source too. Not thread-safe: used on the UI thread.
/// </summary>
public sealed class SourceIcons
{
    public const string Desktop = "device";
    public const string StartMenu = "view-cards";
    public const string Firefox = "firefox";
    public const string Edge = "edge";
    public const string RunHistory = "history";
    public const string WindowsSettings = "settings";
    public const string Custom = "add";              // Made with the + button.
    public const string Folder = "folder";           // Added in Settings > Sources.
    public const string QuickAccess = "home";        // Pinned in Explorer's Quick access.
    public const string PackagedApps = "shop";
    public const string Steam = "steam";
    public const string Epic = "epic";
    public const string Gog = "gog";

    /// <summary>Text typed in the search box (Enter acts on it).</summary>
    public const string Typed = "keyboard";

    private readonly Func<IReadOnlyList<IResourceSource>> _sources;
    private IReadOnlyList<IResourceSource>? _indexed;
    private Dictionary<string, (string Icon, string Label)> _bySource = [];

    /// <param name="sources">All the sources, enabled or not: a recent item may come from one that is off.</param>
    public SourceIcons(IEnumerable<IResourceSource> sources)
        : this(Fixed(sources))
    {
    }

    /// <param name="sources">
    /// The current sources (see <see cref="SourceCatalog.All"/>), asked at each lookup: the icons
    /// are indexed again when it returns another list.
    /// </param>
    public SourceIcons(Func<IReadOnlyList<IResourceSource>> sources)
    {
        _sources = sources;
    }

    /// <summary>
    /// The icon of a source. A folder source that is gone (removed, or unpinned in Explorer) has
    /// the folder icon; another unknown source (such as one removed from coursee) has none.
    /// </summary>
    public string IconOf(string sourceName) =>
        Index().TryGetValue(sourceName, out var entry) ? entry.Icon
        : FolderSources.IsFolderName(sourceName) ? Folder
        : string.Empty;

    /// <summary>
    /// The name of a source as shown to the user (the tooltip of its icon): the folder of a folder
    /// source that is gone, the name itself for another unknown one.
    /// </summary>
    public string LabelOf(string sourceName) =>
        Index().TryGetValue(sourceName, out var entry) ? entry.Label
        : FolderSources.FolderOf(sourceName) ?? sourceName;

    private static Func<IReadOnlyList<IResourceSource>> Fixed(IEnumerable<IResourceSource> sources)
    {
        var list = sources.ToList();
        return () => list;
    }

    private Dictionary<string, (string Icon, string Label)> Index()
    {
        var sources = _sources();
        if (ReferenceEquals(sources, _indexed))
        {
            return _bySource;
        }

        var bySource = new Dictionary<string, (string Icon, string Label)>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            bySource.TryAdd(source.Name, (source.Icon, source.DisplayName));
        }

        bySource.TryAdd(FallbackClassifier.SourceName, (Typed, "What you typed"));
        _bySource = bySource;
        _indexed = sources;
        return bySource;
    }
}
