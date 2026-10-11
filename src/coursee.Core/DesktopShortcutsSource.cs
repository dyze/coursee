namespace Coursee.Core;

/// <summary>Resolves a Windows shortcut (.lnk) to the path it points to.</summary>
public interface IShortcutResolver
{
    string? ResolveTarget(string shortcutPath);
}

/// <summary>
/// Exposes the targets of the .lnk shortcuts found in the given folders (the Desktop).
/// Shortcuts whose target no longer exists are skipped.
/// </summary>
public sealed class DesktopShortcutsSource : IResourceSource
{
    private readonly IReadOnlyList<string> _folders;
    private readonly IShortcutResolver _resolver;

    public DesktopShortcutsSource(IEnumerable<string> folders, IShortcutResolver resolver)
    {
        _folders = folders.ToList();
        _resolver = resolver;
    }

    public string Name => "Desktop";

    public IReadOnlyList<Resource> GetResources()
    {
        var resources = new List<Resource>();

        foreach (var folder in _folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var shortcut in Directory.EnumerateFiles(folder, "*.lnk"))
            {
                var target = _resolver.ResolveTarget(shortcut);
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

                resources.Add(new Resource(Path.GetFileNameWithoutExtension(shortcut), target, kind, Name));
            }
        }

        return resources;
    }
}
