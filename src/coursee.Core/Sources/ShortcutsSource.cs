using Coursee.Core.Logging;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>Resolves a Windows shortcut (.lnk) to the path it points to.</summary>
public interface IShortcutResolver
{
    string? ResolveTarget(string shortcutPath);
}

/// <summary>
/// Exposes the targets of the .lnk shortcuts found in some folders: the Desktop, a folder source,
/// or the Start Menu (subfolders included). The target is what is shown and searched; the .lnk itself is
/// what is opened, so the shortcut's arguments and working folder are kept. Shortcuts whose
/// target no longer exists, and uninstall shortcuts, are skipped. Resolving a shortcut is slow,
/// so each one is resolved again only when its file changes. The Desktop and folder sources also
/// list the files and folders placed in them directly (not hidden or system ones). Not thread-safe: call from
/// one thread at a time.
/// </summary>
public sealed class ShortcutsSource : IResourceSource
{
    public const string DesktopName = "Desktop";
    public const string StartMenuName = "Start Menu";

    private readonly IReadOnlyList<string> _folders;
    private readonly IShortcutResolver _resolver;
    private readonly SearchOption _searchOption;
    private readonly bool _includeFilesAndFolders;
    private readonly ILog _log;
    private readonly Dictionary<string, (DateTime Stamp, string? Target)> _resolved =
        new(StringComparer.OrdinalIgnoreCase);

    private ShortcutsSource(
        string name,
        string displayName,
        string icon,
        IEnumerable<string> folders,
        IShortcutResolver resolver,
        bool includeSubfolders,
        bool includeFilesAndFolders,
        string? folderEntryName,
        ILog? log)
    {
        Name = name;
        DisplayName = displayName;
        Icon = icon;
        _folders = folders.ToList();
        _resolver = resolver;
        _searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        _includeFilesAndFolders = includeFilesAndFolders;
        FolderEntryName = folderEntryName;
        _log = log ?? NullLog.Instance;
    }

    /// <summary>
    /// The user's Desktop folders (personal first, then common), top level only: shortcuts, files
    /// and folders. Shown as the personal folder's path, like a folder source.
    /// With <paramref name="folderEntryName"/>, the personal folder itself is listed too, under that name.
    /// </summary>
    public static ShortcutsSource Desktop(IEnumerable<string> folders, IShortcutResolver resolver, ILog? log = null, string? folderEntryName = null)
    {
        var list = folders.ToList();
        var path = list.Count > 0 ? FolderSources.Normalize(list[0]) : DesktopName;
        return new(DesktopName, path, SourceIcons.Desktop, list, resolver, includeSubfolders: false, includeFilesAndFolders: true, folderEntryName, log);
    }

    /// <summary>
    /// A folder added by the user, or pinned in Explorer's Quick access: top level only, like the
    /// Desktop. Named after its path (see <see cref="FolderSources"/>).
    /// With <paramref name="folderEntryName"/>, the folder itself is listed too, under that name.
    /// </summary>
    public static ShortcutsSource Folder(string folder, bool quickAccess, IShortcutResolver resolver, ILog? log = null, string? folderEntryName = null)
    {
        var path = FolderSources.Normalize(folder);
        return new(
            FolderSources.NameOf(path),
            path,
            quickAccess ? SourceIcons.QuickAccess : SourceIcons.Folder,
            [path],
            resolver,
            includeSubfolders: false,
            includeFilesAndFolders: true,
            folderEntryName,
            log);
    }

    /// <summary>The Start Menu folders (personal and common), subfolders included.</summary>
    public static ShortcutsSource StartMenu(IEnumerable<string> folders, IShortcutResolver resolver, ILog? log = null) =>
        new(StartMenuName, "Start Menu shortcuts", SourceIcons.StartMenu, folders, resolver, includeSubfolders: true, includeFilesAndFolders: false, folderEntryName: null, log);

    public string Name { get; }

    public string DisplayName { get; }

    /// <summary>The name of the row of the (first) folder itself, or null when the folder is not listed.</summary>
    public string? FolderEntryName { get; }

    public bool CanBeDisabled => true;

    public string Icon { get; }

    public IReadOnlyList<Resource> GetResources()
    {
        var resources = new List<Resource>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FolderEntryName is not null && _folders.Count > 0 && Directory.Exists(_folders[0]))
        {
            resources.Add(new Resource(FolderEntryName, _folders[0], ResourceKind.Folder, Name));
        }

        foreach (var folder in _folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var shortcut in Shortcuts(folder))
            {
                seen.Add(shortcut);

                var name = Path.GetFileNameWithoutExtension(shortcut);
                var target = Resolve(shortcut);
                if (string.IsNullOrWhiteSpace(target) || IsUninstaller(name, target))
                {
                    continue;
                }

                ResourceKind kind;
                if (Directory.Exists(target))
                {
                    kind = ResourceKind.Folder;
                }
                else if (File.Exists(target))
                {
                    kind = ResourceKind.File;
                }
                else
                {
                    continue;
                }

                resources.Add(new Resource(name, target, kind, Name, shortcut));
            }

            if (_includeFilesAndFolders)
            {
                resources.AddRange(FilesAndFolders(folder));
            }
        }

        // Forget shortcuts that were deleted.
        foreach (var gone in _resolved.Keys.Where(path => !seen.Contains(path)).ToList())
        {
            _resolved.Remove(gone);
        }

        return resources;
    }

    /// <summary>
    /// An uninstall shortcut (its name mentions "uninstall", or it points at an uninstaller such
    /// as uninstall.exe or Inno Setup's unins000.exe): never what one wants to launch by
    /// accident from a search.
    /// </summary>
    public static bool IsUninstaller(string name, string target) =>
        name.Contains("uninstall", StringComparison.OrdinalIgnoreCase)
        || Path.GetFileName(target).StartsWith("unins", StringComparison.OrdinalIgnoreCase);

    private IEnumerable<string> Shortcuts(string folder)
    {
        // A protected subfolder must not hide the rest of the Start Menu.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = _searchOption == SearchOption.AllDirectories,
            IgnoreInaccessible = true,
        };

        try
        {
            return Directory.EnumerateFiles(folder, "*.lnk", options).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Warn(Name, $"Cannot list {folder}: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    /// <summary>What is on the folder besides shortcuts, opened as it is (a script runs, a folder opens).</summary>
    private IEnumerable<Resource> FilesAndFolders(string folder)
    {
        // The default options skip hidden and system entries, such as desktop.ini.
        var options = new EnumerationOptions { IgnoreInaccessible = true };
        try
        {
            return Directory.EnumerateFileSystemEntries(folder, "*", options)
                .Where(path => !path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                .Select(path => new Resource(
                    Path.GetFileName(path),
                    path,
                    Directory.Exists(path) ? ResourceKind.Folder : ResourceKind.File,
                    Name))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Warn(Name, $"Cannot list {folder}: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    private string? Resolve(string shortcut)
    {
        var stamp = File.GetLastWriteTimeUtc(shortcut);
        if (_resolved.TryGetValue(shortcut, out var cached) && cached.Stamp == stamp)
        {
            return cached.Target;
        }

        var target = _resolver.ResolveTarget(shortcut);
        _resolved[shortcut] = (stamp, target);
        return target;
    }
}
