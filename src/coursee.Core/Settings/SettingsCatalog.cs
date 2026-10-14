using Coursee.Core.Sources;

namespace Coursee.Core.Settings;

/// <summary>One entry of the Settings window, as far as finding it is concerned.</summary>
/// <param name="Id">Ties the entry to its row in the window.</param>
/// <param name="Tab">The tab that shows it.</param>
/// <param name="Label">The text shown next to it.</param>
/// <param name="Keywords">Other words that find it (for example "shortcut" for the hotkey).</param>
public sealed record SettingInfo(string Id, string Tab, string Label, IReadOnlyList<string> Keywords);

/// <summary>
/// Every entry of the Settings window, grouped in tabs, and the search that finds them.
/// The window builds its rows from the same ids, so a new setting is added here first.
/// </summary>
public sealed class SettingsCatalog
{
    public const string GeneralTab = "General";
    public const string AppearanceTab = "Appearance";
    public const string SourcesTab = "Sources";
    public const string EntriesTab = "Entries";
    public const string KeysTab = "Keys";
    public const string LoggingTab = "Logging";

    public const string HotkeyId = "Hotkey";
    public const string RecentsCapacityId = "RecentsCapacity";
    public const string AutoStartId = "AutoStart";
    public const string LauncherPositionId = "LauncherPosition";
    public const string IgnoreAccentsId = "IgnoreAccents";
    public const string ThemeId = "Theme";
    public const string AddFolderId = "AddFolder";
    public const string QuickAccessId = "QuickAccess";
    public const string EntriesId = "Entries";
    public const string KeyBindingsId = "KeyBindings";
    public const string DebugLoggingId = "DebugLogging";
    public const string LogWindowId = "LogWindow";

    private const string SourceIdPrefix = "Source:";

    /// <param name="sources">
    /// All the sources; those that can be switched off get an entry in the Sources tab. The window
    /// makes a new catalog when the sources change (<see cref="SourceCatalog.Changed"/>).
    /// </param>
    /// <param name="hasQuickAccess">True when Quick access folders are listed: their group gets an entry.</param>
    public SettingsCatalog(IEnumerable<IResourceSource> sources, bool hasQuickAccess = false)
    {
        var entries = new List<SettingInfo>
        {
            new(RecentsCapacityId, GeneralTab, "Recent items to keep", ["recents", "history", "count", "number"]),
            new(AutoStartId, GeneralTab, "Start coursee when I sign in to Windows", ["auto", "startup", "boot", "login"]),
            new(LauncherPositionId, GeneralTab, "Launcher position", ["place", "where", "mouse", "center", "centre", "monitor", "screen", "window"]),
            new(IgnoreAccentsId, GeneralTab, "Ignore accents when searching", ["accent", "diacritic", "search", "match", "letters"]),
            new(ThemeId, AppearanceTab, "Theme", ["dark", "light", "mode", "color", "colour"]),
        };

        entries.AddRange(sources
            .Where(source => source.CanBeDisabled)
            .Select(source => new SettingInfo(SourceId(source.Name), SourcesTab, source.DisplayName, ["source", "on", "off", source.Name])));
        if (hasQuickAccess)
        {
            entries.Add(new(QuickAccessId, SourcesTab, "Quick access", ["pinned", "explorer", "all", "group"]));
        }

        entries.Add(new(AddFolderId, SourcesTab, "Add folder...", ["folder", "directory", "source", "add"]));

        entries.Add(new(
            EntriesId,
            EntriesTab,
            "Hidden entries, custom entries, aliases and pins",
            ["hide", "restore", "show", "custom", "own", "edit", "delete", "rename", "alias", "pin", "unpin"]));
        entries.Add(new(HotkeyId, KeysTab, "Hotkey", ["shortcut", "key", "keyboard", "show", "launcher"]));
        entries.Add(new(KeyBindingsId, KeysTab, "Key bindings", ["shortcut", "keyboard", "keys", "help"]));
        entries.Add(new(DebugLoggingId, LoggingTab, "Detailed (debug) logging", ["log", "verbose", "trace"]));
        entries.Add(new(LogWindowId, LoggingTab, "Open log window", ["log", "view", "errors"]));

        All = entries;
    }

    /// <summary>The tabs, in the order they are shown.</summary>
    public IReadOnlyList<string> Tabs { get; } = [GeneralTab, AppearanceTab, SourcesTab, EntriesTab, KeysTab, LoggingTab];

    public IReadOnlyList<SettingInfo> All { get; }

    /// <summary>The id of the checkbox that switches a source on or off.</summary>
    public static string SourceId(string sourceName) => SourceIdPrefix + sourceName;

    /// <summary>
    /// The entries matching every word of <paramref name="query"/>, in their usual order. A word
    /// matches when the label, a keyword or the tab name contains it, ignoring case. An empty
    /// query matches nothing: the window then shows its tabs instead.
    /// </summary>
    public IReadOnlyList<SettingInfo> Find(string? query)
    {
        var words = (query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            return [];
        }

        return All.Where(entry => words.All(word => Matches(entry, word))).ToList();
    }

    private static bool Matches(SettingInfo entry, string word) =>
        entry.Label.Contains(word, StringComparison.OrdinalIgnoreCase)
        || entry.Tab.Contains(word, StringComparison.OrdinalIgnoreCase)
        || entry.Keywords.Any(keyword => keyword.Contains(word, StringComparison.OrdinalIgnoreCase));
}
