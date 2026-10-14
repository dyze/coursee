using Coursee.Core.Logging;
using Coursee.Core.Settings;

namespace Coursee.Core.Sources;

/// <summary>What <see cref="SourceCatalog.AddFolder"/> did.</summary>
public enum AddFolderResult
{
    Added,
    AlreadyAdded,

    /// <summary>Network folders are never listed (see <see cref="FolderSources.IsNetwork"/>).</summary>
    NetworkFolder,
}

/// <summary>
/// The current list of sources. Most are fixed; the folder sources change while coursee runs:
/// the user adds or removes folders (<see cref="SettingsStore.Folders"/>), and pins or unpins
/// folders in Explorer's Quick access. In order:
/// <list type="number">
/// <item>the sources given as <c>first</c> (the user's custom entries),</item>
/// <item>the user's folders (the Desktop by default),</item>
/// <item>the Quick access folders not listed yet (local ones only; a pinned Desktop is the Desktop source),</item>
/// <item>the sources given as <c>last</c>.</item>
/// </list>
/// A folder is listed once: the first wins. Each folder source also lists the folder itself, under
/// the name Explorer shows when it is pinned (<see cref="FolderSources.EntryName"/>). Each is kept
/// while listed (and named the same), so its shortcuts are not resolved again. Thread-safe: Quick access is read on a background thread.
/// </summary>
public sealed class SourceCatalog
{
    public const string QuickAccessCategory = "Quick access";

    private readonly SettingsStore _settings;
    private readonly IShortcutResolver _resolver;
    private readonly IReadOnlyList<string> _desktopFolders;
    private readonly IQuickAccessReader _quickAccess;
    private readonly IReadOnlyList<IResourceSource> _first;
    private readonly IReadOnlyList<IResourceSource> _last;
    private readonly ILog _log;
    private readonly Dictionary<string, ShortcutsSource> _folderSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();
    private IReadOnlyList<PinnedFolder> _pinned = [];
    private volatile IReadOnlyList<IResourceSource> _all = [];
    private volatile IReadOnlySet<string> _quickAccessNames = new HashSet<string>();

    /// <param name="desktopFolders">The personal and common Desktop folders.</param>
    public SourceCatalog(
        SettingsStore settings,
        IShortcutResolver resolver,
        IReadOnlyList<string> desktopFolders,
        IQuickAccessReader quickAccess,
        IReadOnlyList<IResourceSource> first,
        IReadOnlyList<IResourceSource> last,
        ILog? log = null)
    {
        _settings = settings;
        _resolver = resolver;
        _desktopFolders = desktopFolders;
        _quickAccess = quickAccess;
        _first = first;
        _last = last;
        _log = log ?? NullLog.Instance;
        Rebuild();
    }

    /// <summary>
    /// The list changed (a folder added or removed, Quick access changed). Raised on the thread
    /// that changed it: the UI thread for a folder, a background one for Quick access.
    /// </summary>
    public event Action? Changed;

    /// <summary>All the sources, enabled or not. The same list until it changes.</summary>
    public IReadOnlyList<IResourceSource> All => _all;

    /// <summary>True for a folder the user added (the Desktop included): it can be removed. Quick access folders cannot.</summary>
    public bool CanRemove(string sourceName) => StoredEntryOf(sourceName) is not null;

    /// <summary>
    /// True for a source listed because it is pinned in Quick access (the Desktop too, when it
    /// is not one of the user's folders). These come together in <see cref="All"/>.
    /// </summary>
    public bool IsQuickAccess(string sourceName) => _quickAccessNames.Contains(sourceName);

    /// <summary>
    /// Adds a folder chosen by the user, at the end of their folders. One of the Desktop folders
    /// adds the Desktop. Saves the settings (may throw an I/O error).
    /// </summary>
    public AddFolderResult AddFolder(string folder)
    {
        var path = FolderSources.Normalize(folder);
        if (FolderSources.IsNetwork(path))
        {
            return AddFolderResult.NetworkFolder;
        }

        var entry = IsDesktopFolder(path) ? SettingsStore.DesktopFolder : path;

        lock (_gate)
        {
            if (!_settings.AddFolder(entry))
            {
                return AddFolderResult.AlreadyAdded;
            }
        }

        RebuildAndNotify();
        return AddFolderResult.Added;
    }

    /// <summary>
    /// Removes a folder the user added, and forgets it was switched off. False when it was not
    /// one of theirs. Saves the settings (may throw an I/O error).
    /// </summary>
    public bool RemoveFolder(string sourceName)
    {
        lock (_gate)
        {
            if (StoredEntryOf(sourceName) is not { } entry)
            {
                return false;
            }

            _settings.RemoveFolder(entry);

            _settings.SetSourceEnabled(sourceName, true);
        }

        RebuildAndNotify();
        return true;
    }

    /// <summary>
    /// Reads Quick access again and updates the list. When it cannot be read, the last folders
    /// read are kept, so a failure does not make them disappear. True when the list changed.
    /// </summary>
    public bool SyncQuickAccess()
    {
        IReadOnlyList<PinnedFolder> read;
        try
        {
            read = _quickAccess.PinnedFolders();
        }
        catch (Exception ex)
        {
            _log.Warn(QuickAccessCategory, $"Cannot read Quick access: {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        var pinned = read
            .Select(folder => folder with { Path = FolderSources.Normalize(folder.Path) })
            .Where(folder => folder.Path.Length > 0 && !FolderSources.IsNetwork(folder.Path))
            .DistinctBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lock (_gate)
        {
            // A renamed one counts too: its row is named after it.
            if (pinned.SequenceEqual(_pinned))
            {
                return false;
            }

            LogPinnedChange(_pinned, pinned);
            _pinned = pinned;
        }

        return RebuildAndNotify();
    }

    /// <summary>The entry of <see cref="SettingsStore.Folders"/> that made a source, or null (Quick access, another source).</summary>
    private string? StoredEntryOf(string sourceName)
    {
        var entry = string.Equals(sourceName, ShortcutsSource.DesktopName, StringComparison.OrdinalIgnoreCase)
            ? SettingsStore.DesktopFolder
            : FolderSources.FolderOf(sourceName);
        return entry is null ? null : _settings.Folders.FirstOrDefault(stored => FolderSources.AreSame(stored, entry));
    }

    private bool IsDesktopFolder(string folder) =>
        _desktopFolders.Any(desktop => FolderSources.AreSame(desktop, folder));

    private bool RebuildAndNotify()
    {
        bool changed;
        lock (_gate)
        {
            changed = Rebuild();
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        return changed;
    }

    /// <summary>Builds the list again; true when it differs. Called under the lock (or from the constructor).</summary>
    private bool Rebuild()
    {
        var sources = new List<IResourceSource>(_first);
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var quickAccessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The name Explorer shows, for every pinned folder: also the one of a folder the user added.
        var shownNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in _pinned)
        {
            shownNames[folder.Path] = folder.Name;
        }

        void AddDesktop()
        {
            sources.Add(DesktopSource(shownNames));
            listed.UnionWith(_desktopFolders.Select(FolderSources.Normalize));
        }

        foreach (var entry in _settings.Folders)
        {
            if (string.Equals(entry, SettingsStore.DesktopFolder, StringComparison.OrdinalIgnoreCase))
            {
                AddDesktop();
            }
            else if (listed.Add(FolderSources.Normalize(entry)))
            {
                sources.Add(FolderSource(entry, quickAccess: false, shownNames));
            }
        }

        // A pinned Desktop is the Desktop source, both Desktop folders, with its usual name.
        foreach (var folder in _pinned.Select(pinned => pinned.Path).Where(listed.Add))
        {
            if (IsDesktopFolder(folder))
            {
                AddDesktop();
            }
            else
            {
                sources.Add(FolderSource(folder, quickAccess: true, shownNames));
            }

            quickAccessNames.Add(sources[^1].Name);
        }

        sources.AddRange(_last);

        // Forget the folders no longer listed: one listed again later starts afresh.
        var kept = sources.Select(source => source.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var gone in _folderSources.Keys.Where(name => !kept.Contains(name)).ToList())
        {
            _folderSources.Remove(gone);
        }

        // The Desktop can move between the user's folders and Quick access without changing place.
        if (sources.SequenceEqual(_all) && quickAccessNames.SetEquals(_quickAccessNames))
        {
            return false;
        }

        _quickAccessNames = quickAccessNames;
        _all = sources;
        return true;
    }

    private ShortcutsSource DesktopSource(Dictionary<string, string> shownNames)
    {
        var personal = _desktopFolders.Count > 0 ? _desktopFolders[0] : ShortcutsSource.DesktopName;
        return Kept(
            ShortcutsSource.DesktopName,
            SourceIcons.Desktop,
            FolderSources.EntryName(personal, shownNames.GetValueOrDefault(FolderSources.Normalize(personal))),
            entryName => ShortcutsSource.Desktop(_desktopFolders, _resolver, _log, entryName));
    }

    private ShortcutsSource FolderSource(string folder, bool quickAccess, Dictionary<string, string> shownNames) =>
        Kept(
            FolderSources.NameOf(folder),
            quickAccess ? SourceIcons.QuickAccess : SourceIcons.Folder,
            FolderSources.EntryName(folder, shownNames.GetValueOrDefault(FolderSources.Normalize(folder))),
            entryName => ShortcutsSource.Folder(folder, quickAccess, _resolver, _log, entryName));

    /// <summary>The source already made, when it has not changed side (user or Quick access) nor name; else a new one.</summary>
    private ShortcutsSource Kept(string name, string icon, string entryName, Func<string, ShortcutsSource> make)
    {
        if (_folderSources.TryGetValue(name, out var existing) && existing.Icon == icon && existing.FolderEntryName == entryName)
        {
            return existing;
        }

        var made = make(entryName);
        _folderSources[name] = made;
        return made;
    }

    private void LogPinnedChange(IReadOnlyList<PinnedFolder> beforeFolders, IReadOnlyList<PinnedFolder> afterFolders)
    {
        var before = beforeFolders.Select(folder => folder.Path).ToList();
        var after = afterFolders.Select(folder => folder.Path).ToList();
        var added = after.Except(before, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = before.Except(after, StringComparer.OrdinalIgnoreCase).ToList();
        var parts = new List<string> { $"{after.Count} pinned folders" };
        if (added.Count > 0)
        {
            parts.Add("added: " + string.Join(", ", added));
        }

        if (removed.Count > 0)
        {
            parts.Add("removed: " + string.Join(", ", removed));
        }

        _log.Info(QuickAccessCategory, string.Join("; ", parts));
    }
}
