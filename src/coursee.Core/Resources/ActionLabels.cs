namespace Coursee.Core.Resources;

/// <summary>What Enter does with a resource, as shown by the icon of its row.</summary>
public enum ResourceAction
{
    OpenFile,
    OpenFolder,
    OpenUrl,

    /// <summary>A bookmark opened in the browser it comes from, not the default one.</summary>
    OpenInBrowser,
    RunCommand,

    /// <summary>A network path: the shell opens it, file or folder.</summary>
    Open,
}

/// <summary>The wording and the action shown to the user for what Enter will do with a resource.</summary>
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

    /// <param name="browser">For a URL, the browser it opens in, when it is not the default one.</param>
    public static ResourceAction ActionFor(ResourceKind kind, string? browser = null) => kind switch
    {
        ResourceKind.File => ResourceAction.OpenFile,
        ResourceKind.Folder => ResourceAction.OpenFolder,
        ResourceKind.Url when !string.IsNullOrWhiteSpace(browser) => ResourceAction.OpenInBrowser,
        ResourceKind.Url => ResourceAction.OpenUrl,
        ResourceKind.Command => ResourceAction.RunCommand,
        _ => ResourceAction.Open,
    };
}
