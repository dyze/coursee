namespace Coursee.Core;

/// <summary>The wording shown to the user for what Enter will do with a resource.</summary>
public static class ActionLabels
{
    public static string For(ResourceKind kind) => kind switch
    {
        ResourceKind.File => "Open file",
        ResourceKind.Folder => "Open folder",
        ResourceKind.Url => "Open URL",
        ResourceKind.Command => "Run command",
        _ => "Open",
    };
}
