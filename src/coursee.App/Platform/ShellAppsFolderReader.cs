using Coursee.Core.Sources;

namespace Coursee.UI.Platform;

/// <summary>
/// Reads the Apps folder (<c>shell:AppsFolder</c>), the list Windows Start shows, through the
/// Shell (see <see cref="ShellFolder"/>). An item's path there is its app ID.
/// </summary>
public sealed class ShellAppsFolderReader : IAppsFolderReader
{
    private const string AppsFolder = "shell:AppsFolder";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public IReadOnlyList<AppsFolderItem> Items() =>
        ShellFolder.Items(AppsFolder, "Apps folder", Timeout, AsApp);

    private static AppsFolderItem? AsApp(object item)
    {
        dynamic shellItem = item;
        string? name = shellItem.Name;
        string? appId = shellItem.Path;
        object? family = shellItem.ExtendedProperty("System.AppUserModel.PackageFamilyName");
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(appId)
            ? null
            : new AppsFolderItem(name, appId, family as string);
    }
}
