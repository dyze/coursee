namespace Coursee.Core;

/// <summary>Resolves a Windows shortcut (.lnk) to the path it points to.</summary>
public interface IShortcutResolver
{
    string? ResolveTarget(string shortcutPath);
}

/// <summary>
/// Exposes the targets of the .lnk shortcuts found in the given folders (the Desktop).
/// The target is what is shown and searched; the .lnk itself is what is opened, so the
/// shortcut's arguments and working folder are kept. Shortcuts whose target no longer
/// exists are skipped. Resolving a shortcut is slow, so each one is resolved again only
/// when its file changes. Not thread-safe: call from one thread at a time.
/// </summary>
public sealed class DesktopShortcutsSource : IResourceSource
{
    private readonly IReadOnlyList<string> _folders;
    private readonly IShortcutResolver _resolver;
    private readonly Dictionary<string, (DateTime Stamp, string? Target)> _resolved =
        new(StringComparer.OrdinalIgnoreCase);

    public DesktopShortcutsSource(IEnumerable<string> folders, IShortcutResolver resolver)
    {
        _folders = folders.ToList();
        _resolver = resolver;
    }

    public string Name => "Desktop";

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

            foreach (var shortcut in Directory.EnumerateFiles(folder, "*.lnk"))
            {
                seen.Add(shortcut);

                var target = Resolve(shortcut);
                if (string.IsNullOrWhiteSpace(target))
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

                resources.Add(new Resource(Path.GetFileNameWithoutExtension(shortcut), target, kind, Name, shortcut));
            }
        }

        // Forget shortcuts that were deleted.
        foreach (var gone in _resolved.Keys.Where(path => !seen.Contains(path)).ToList())
        {
            _resolved.Remove(gone);
        }

        return resources;
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
