namespace Coursee.Core.Resources;

/// <summary>The wording shown to the user for what Enter will do with a resource.</summary>
public static class ActionLabels
{
    /// <param name="browser">For a URL, the browser it opens in, when it is not the default one.</param>
    public static string For(ResourceKind kind, string? browser = null) => kind switch
    {
        ResourceKind.File => "Open file",
        ResourceKind.Folder => "Open folder",
        ResourceKind.Url when !string.IsNullOrWhiteSpace(browser) => $"Open in {browser}",
        ResourceKind.Url => "Open URL",
        ResourceKind.Command => "Run command",
        ResourceKind.NetworkPath => "Open",
        _ => "Open",
    };
}
