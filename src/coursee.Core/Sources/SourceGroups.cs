using Coursee.Core.Games;

namespace Coursee.Core.Sources;

/// <summary>A group of sources in Settings > Sources: one checkbox switches them all.</summary>
/// <param name="Name">Shown on the group's checkbox.</param>
/// <param name="Icon">A Segoe Fluent Icons glyph, like a source's.</param>
/// <param name="Keywords">More words that find the group in Settings.</param>
public sealed record SourceGroup(string Name, string Icon, string ToolTip, IReadOnlyList<string> Keywords);

/// <summary>Which group a source belongs to, if any: by its name, as one of the user's folders, or as a Quick access folder.</summary>
public static class SourceGroups
{
    public static SourceGroup Folders { get; } = new(
        "Folders", "\uE838", "The folders you added (the Desktop by default): ✕ removes one", ["folder", "directory", "desktop"]);

    public static SourceGroup QuickAccess { get; } = new(
        "Quick access", SourceIcons.QuickAccess, "The folders pinned in Explorer's Quick access: unpin one there to remove it", ["pinned", "explorer"]);

    public static SourceGroup Windows { get; } = new(
        "Windows", "\uE7F8", "What Windows lists: Start Menu, packaged apps, Run history, settings", ["system", "apps", "start"]);

    public static SourceGroup Browsers { get; } = new(
        "Browsers", "\uE774", "Bookmarks and favorites", ["bookmarks", "favorites", "web"]);

    public static SourceGroup Games { get; } = new(
        "Games", "\uE768", "The installed games of each launcher", ["launcher", "play"]);

    /// <summary>In the order the groups' checkboxes come when their sources are mixed.</summary>
    public static IReadOnlyList<SourceGroup> All { get; } = [Folders, QuickAccess, Windows, Browsers, Games];

    private static readonly Dictionary<string, SourceGroup> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        [ShortcutsSource.StartMenuName] = Windows,
        [PackagedAppsSource.SourceName] = Windows,
        [RunHistorySource.SourceName] = Windows,
        [WindowsSettingsSource.SourceName] = Windows,
        [FirefoxBookmarksSource.SourceName] = Browsers,
        [EdgeFavoritesSource.SourceName] = Browsers,
        [SteamSource.SourceName] = Games,
        [EpicSource.SourceName] = Games,
        [GogSource.SourceName] = Games,
    };

    /// <param name="isQuickAccess">True for a folder pinned in Quick access (see <see cref="SourceCatalog.IsQuickAccess"/>).</param>
    /// <param name="isUserFolder">True for a folder the user added, or the Desktop, unless pinned (see <see cref="SourceCatalog.CanRemove"/>).</param>
    public static SourceGroup? Of(string sourceName, bool isQuickAccess, bool isUserFolder = false) =>
        isQuickAccess ? QuickAccess
        : isUserFolder ? Folders
        : ByName.GetValueOrDefault(sourceName);
}
