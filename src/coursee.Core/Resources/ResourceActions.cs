using Coursee.Core.Platform;

namespace Coursee.Core.Resources;

/// <summary>
/// Which actions apply to a resource besides opening it, and how they are described.
/// The App performs them; the rules live here so they can be unit-tested.
/// </summary>
public static class ResourceActions
{
    /// <summary>File types Windows can run elevated ("runas" verb).</summary>
    private static readonly HashSet<string> ExecutableExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".exe", ".bat", ".cmd", ".com", ".msc" };

    /// <summary>Ctrl+Enter: files, folders and network paths can be shown in their folder.</summary>
    public static bool CanShowInFolder(Resource resource) =>
        resource.Kind is ResourceKind.File or ResourceKind.Folder or ResourceKind.NetworkPath;

    /// <summary>
    /// The arguments for explorer.exe that open the containing folder with the item selected.
    /// The real target is used, not a shortcut's .lnk.
    /// </summary>
    public static string ShowInFolderArguments(Resource resource) =>
        $"/select,\"{WithoutTrailingSlash(resource.Target)}\"";

    /// <summary>
    /// Ctrl+Shift+Enter: commands and executable files can run as administrator, and so can the
    /// Windows tools that are consoles or programs (not the <c>ms-settings:</c> pages).
    /// </summary>
    public static bool CanRunAsAdministrator(Resource resource) => resource.Kind switch
    {
        ResourceKind.Command => true,
        ResourceKind.File or ResourceKind.NetworkPath => ExecutableExtensions.Contains(Extension(resource.Target)),
        ResourceKind.Setting => ExecutableExtensions.Contains(Extension(CommandLine.Split(resource.Target).FileName)),
        _ => false,
    };

    /// <summary>
    /// Ctrl+P: entries from a source can be pinned. Not what was typed: it is no entry of its own,
    /// so it would never be found again.
    /// </summary>
    public static bool CanPin(Resource resource) => IsEntry(resource);

    /// <summary>F2: entries from a source can have an alias. Not what was typed, for the same reason.</summary>
    public static bool CanSetAlias(Resource resource) => IsEntry(resource);

    /// <summary>Ctrl+H: entries from a source can be hidden. Not what was typed: there is nothing to hide.</summary>
    public static bool CanHide(Resource resource) => IsEntry(resource);

    /// <summary>
    /// The row of the typed text, whose name box Ctrl+N or the + button opens to make it a custom
    /// entry. When something matched, the launcher adds that row first.
    /// </summary>
    public static bool CanMakeCustomEntry(Resource resource) => !IsEntry(resource);

    private static bool IsEntry(Resource resource) => resource.Source != FallbackClassifier.SourceName;

    /// <summary>
    /// Enter on a URL from a browser's bookmarks: the program of that browser, so the URL opens
    /// where it was bookmarked. Null when the resource opens with the default handler: it is not
    /// such a URL, or that browser is not installed (the default browser is then used).
    /// </summary>
    public static string? BrowserExecutable(Resource resource, IBrowserLocator locator) =>
        resource.Kind == ResourceKind.Url && Browsers.ExecutableName(resource.Browser) is { } executable
            ? locator.FindExecutable(executable)
            : null;

    /// <summary>
    /// The first line under the search box: what Enter does, with Ctrl and Shift when they apply.
    /// The other keys are on the second line (<see cref="KeysHint"/>).
    /// </summary>
    public static string Hint(Resource resource)
    {
        var typed = resource.Source == FallbackClassifier.SourceName ? " (what you typed)" : string.Empty;
        var parts = new List<string> { $"Enter: {resource.ActionLabel}{typed}" };

        if (CanShowInFolder(resource))
        {
            parts.Add("Ctrl+Enter: Show in folder");
        }

        if (CanRunAsAdministrator(resource))
        {
            parts.Add("Ctrl+Shift+Enter: Run as admin");
        }

        return string.Join(Separator, parts);
    }

    /// <summary>
    /// The second line under the search box: the other keys that apply to the selected item.
    /// <paramref name="inRecents"/> is true when the item is shown in the recent list (Del removes it),
    /// false when the search box has text (Ctrl+N makes that text a custom entry);
    /// <paramref name="isPinned"/> when it is pinned (Ctrl+P unpins it).
    /// </summary>
    public static string KeysHint(Resource resource, bool inRecents, bool isPinned)
    {
        var parts = new List<string> { "Ctrl+C: Copy" };

        if (CanPin(resource))
        {
            parts.Add(isPinned ? "Ctrl+P: Unpin" : "Ctrl+P: Pin");
        }

        if (CanSetAlias(resource))
        {
            parts.Add("F2: Alias");
        }

        if (CanHide(resource))
        {
            parts.Add("Ctrl+H: Hide");
        }

        parts.Add(inRecents ? "Del: Remove from recents" : "Ctrl+N: New custom entry");

        return string.Join(Separator, parts);
    }

    private const string Separator = "  ·  ";

    /// <summary>"C:\work\" becomes "C:\work" (Explorer selects nothing otherwise); a drive root such as "C:\" is kept.</summary>
    private static string WithoutTrailingSlash(string path)
    {
        var trimmed = path.TrimEnd('\\', '/');
        return trimmed.Length == 0 || trimmed.EndsWith(':') ? path : trimmed;
    }

    /// <summary>The extension of a path that may use either slash (paths come from Windows).</summary>
    private static string Extension(string path)
    {
        var name = path[(path.LastIndexOfAny(['\\', '/']) + 1)..];
        var dot = name.LastIndexOf('.');
        return dot < 0 ? string.Empty : name[dot..];
    }
}
