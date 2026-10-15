using Coursee.Core.Sources;

namespace Coursee.UI.Platform;

/// <summary>
/// Reads Explorer's Quick access through the Shell (see <see cref="ShellFolder"/>). Quick access
/// also shows frequent folders: only those with the <c>System.Home.IsPinned</c> property
/// (Windows 11) are pinned ones.
/// </summary>
public sealed class ShellQuickAccessReader : IQuickAccessReader
{
    private const string QuickAccessFolder = "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public IReadOnlyList<PinnedFolder> PinnedFolders() =>
        ShellFolder.Items(QuickAccessFolder, "Quick access", Timeout, AsPinnedFolder);

    /// <summary>A pinned file system folder, with the name Explorer shows; else null. Virtual ones (This PC, Gallery) have no full path.</summary>
    private static PinnedFolder? AsPinnedFolder(object item)
    {
        dynamic shellItem = item;
        bool isFolder = shellItem.IsFolder;
        string? path = shellItem.Path;
        object? pinned = shellItem.ExtendedProperty("System.Home.IsPinned");
        string? name = shellItem.Name;
        return isFolder && pinned is true && path is not null && Path.IsPathFullyQualified(path)
            ? new PinnedFolder(path, string.IsNullOrWhiteSpace(name) ? Path.GetFileName(path) : name)
            : null;
    }
}
