namespace Coursee.Core;

/// <summary>Resolves a Windows shortcut (.lnk) to the path it points to.</summary>
public interface IShortcutResolver
{
    string? ResolveTarget(string shortcutPath);
}

/// <summary>
/// Exposes the targets of the .lnk shortcuts found in some folders: the Desktop, or the Start
/// Menu (subfolders included). The target is what is shown and searched; the .lnk itself is
/// what is opened, so the shortcut's arguments and working folder are kept. Shortcuts whose
/// target no longer exists, and uninstall shortcuts, are skipped. Resolving a shortcut is slow,
/// so each one is resolved again only when its file changes. Not thread-safe: call from one
/// thread at a time.
/// </summary>
public sealed class ShortcutsSource : IResourceSource
{
    public const string DesktopName = "Desktop";
    public const string StartMenuName = "Start Menu";

    private readonly IReadOnlyList<string> _folders;
    private readonly IShortcutResolver _resolver;
    private readonly SearchOption _searchOption;
    private readonly ILog _log;
    private readonly Dictionary<string, (DateTime Stamp, string? Target)> _resolved =
        new(StringComparer.OrdinalIgnoreCase);

    public ShortcutsSource(
        string name,
        IEnumerable<string> folders,
        IShortcutResolver resolver,
        bool includeSubfolders = false,
        ILog? log = null)
    {
        Name = name;
        _folders = folders.ToList();
        _resolver = resolver;
        _searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        _log = log ?? NullLog.Instance;
    }

    /// <summary>The user's Desktop folders (personal and common), top level only.</summary>
    public static ShortcutsSource Desktop(IEnumerable<string> folders, IShortcutResolver resolver, ILog? log = null) =>
        new(DesktopName, folders, resolver, includeSubfolders: false, log);

    /// <summary>The Start Menu folders (personal and common), subfolders included.</summary>
    public static ShortcutsSource StartMenu(IEnumerable<string> folders, IShortcutResolver resolver, ILog? log = null) =>
        new(StartMenuName, folders, resolver, includeSubfolders: true, log);

    public string Name { get; }

    public IReadOnlyList<Resource> GetResources()
    {
        var resources = new List<Resource>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
