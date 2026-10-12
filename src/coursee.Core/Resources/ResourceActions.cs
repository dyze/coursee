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

    /// <summary>Ctrl+Shift+Enter: commands and executable files can run as administrator.</summary>
    public static bool CanRunAsAdministrator(Resource resource) => resource.Kind switch
    {
        ResourceKind.Command => true,
        ResourceKind.File or ResourceKind.NetworkPath => ExecutableExtensions.Contains(Extension(resource.Target)),
        _ => false,
    };

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
    /// The line under the search box: what Enter does, then the other keys that apply.
    /// <paramref name="inRecents"/> is true when the item is shown in the recent list (Del removes it).
    /// </summary>
    public static string Hint(Resource resource, bool inRecents)
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

        if (inRecents)
        {
            parts.Add("Del: Remove from recents");
        }

        return string.Join("  ·  ", parts);
    }

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
