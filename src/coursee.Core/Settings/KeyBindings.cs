namespace Coursee.Core.Settings;

/// <summary>A key and what it does, where it works.</summary>
/// <param name="Area">Where the key works (see the constants of <see cref="KeyBindings"/>).</param>
/// <param name="Keys">The key, as the hint lines write it (for example "Ctrl+Enter").</param>
/// <param name="Action">What it does.</param>
public sealed record KeyBinding(string Area, string Keys, string Action);

/// <summary>
/// The keys coursee supports, as listed read only in Settings > Keys. The global hotkey is not
/// listed: it is the setting above the list. Kept in step with the hint lines of the launcher
/// (<see cref="Resources.ResourceActions"/>) by the tests.
/// </summary>
public static class KeyBindings
{
    public const string Launcher = "Launcher";
    public const string RowBox = "Alias or name box (on a row of the launcher)";
    public const string Settings = "Settings";

    public static IReadOnlyList<KeyBinding> All { get; } =
    [
        new(Launcher, "Enter", "Open the selected entry, or act on the typed text when nothing matches"),
        new(Launcher, "Ctrl+Enter", "Show in folder (files and folders)"),
        new(Launcher, "Ctrl+Shift+Enter", "Run as administrator (commands, programs, consoles)"),
        new(Launcher, "Ctrl+C", "Copy the path, URL or command; with text selected in the search box, that text"),
        new(Launcher, "Ctrl+P", "Pin or unpin the selected entry"),
        new(Launcher, "F2", "Set the alias of the selected entry"),
        new(Launcher, "Ctrl+H", "Hide the selected entry from results and recents"),
        new(Launcher, "Ctrl+N", "Make the typed text a custom entry"),
        new(Launcher, "Del", "Remove the selected entry from recents (search box empty)"),
        new(Launcher, "Up / Down", "Move the selection"),
        new(Launcher, "Page Up / Page Down", "Move the selection by one page"),
        new(Launcher, "Ctrl+Home / Ctrl+End", "Select the first / last entry"),
        new(Launcher, "Tab", "Go to the Settings button (Enter or Space opens Settings), and back"),
        new(Launcher, "Esc", "Clear the search box; when it is empty, hide the launcher"),
        new(RowBox, "Enter", "Save"),
        new(RowBox, "Esc", "Cancel"),
        new(Settings, "Ctrl+F", "Find a setting"),
        new(Settings, "Down", "In the Find box: go to the first setting shown"),
        new(Settings, "Ctrl+Tab / Ctrl+Shift+Tab", "Next / previous tab"),
        new(Settings, "Esc", "Clear the Find box; when it is empty, close Settings"),
    ];
}
