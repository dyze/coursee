# coursee: roadmap

Scope of each version. The goal, principles and non-goals are in [GOAL.md](GOAL.md).

| Version | Status | Theme                                                                                |
| ------- | ------ | ------------------------------------------------------------------------------------ |
| 0.1     | Done   | Hotkey, search, recents, Desktop, Firefox and Run history sources, settings, logging |
| 0.2     | Done   | Tray icon, Start Menu source, row actions, installer                                 |
| 0.3     | Done   | Code layout, Edge source, right browser, window positions, theme, settings window    |
| 0.4     | Done   | Launcher position, list keys, Windows settings source, icons, keyboard-only Settings |
| 0.5     | Done   | Accents ignored, copy, most used first, pins, aliases                                |
| 0.6     | Done   | Coverage, hide entries, custom entries, Entries and Keys tabs, Desktop files         |
| 0.7     | Planned | About tab, update check, safety notice, folder sources                              |

## 0.1
1. **Hotkey and window.** A global hotkey toggles the compact window. Only one copy of coursee runs: starting it again shows the launcher of the running copy.
2. **Source: Desktop shortcuts.** Shortcuts (.lnk) on the Windows Desktop, resolved to their targets (files and folders). The target is what is shown and searched; Enter opens the shortcut itself, so its arguments and working folder are kept.
3. **Search** with filtering as you type.
4. **Recents.** What was opened via coursee, shown when the search box is empty.
5. **Open and hide.** Enter opens the target; Esc hides the window.
6. **Version** shown in the window.
7. **Settings view** (gear button). Section **Windows integration**: auto start on or off, when the user signs in to Windows, with no administrator rights.
8. **Source: Firefox bookmarks.** Default profile only, real bookmarks only (no folders or smart queries), opened in the default browser. Section **Sources** switches it on or off.
9. **Launcher settings.** Section **Launcher**: the global hotkey (default Ctrl+Alt+Space; it must include Ctrl, Alt or Win, since Shift alone would block normal typing) and how many recent items are kept (default 20).
10. **Fallback action**, like the Run dialog (Win+R): when the typed text matches no resource, Enter acts on the text itself. It opens a URL (`http(s)://`, `ftp://` or `www.`), opens an existing folder or file (quotes and `%VARS%` allowed), opens a network path (`\\server\share`, not checked on disk so typing never blocks), or runs the text as a command. The window always shows what Enter will do: a line under the search box ("Enter: Open folder") and a label on every row. Typed actions are added to recents.
11. **Source: Windows Run history.** The commands previously typed in Win+R, most recent first, are searchable, to ease the move away from Win+R. Switchable in **Sources**.
12. **Logging**, for debugging and feedback: start/stop, changes in each source (items added / removed at each refresh), every action the user triggers, and errors. Written to a size-limited local file and shown live in a **log window** (Settings, section **Logging**) with level and text filters, copy and clear. Info by default; a setting turns on detailed (Debug) logging. Never logged: keystrokes while searching, secrets.

## 0.2
1. **Tray icon.** Click: show the launcher. Menu: *Show coursee* (with the current hotkey), *Settings*, *Log window*, *Quit*. The temporary Ctrl+Q shortcut is removed. If no hotkey can be registered, coursee keeps running and is reached from the tray. coursee.exe and the tray use the same icon.
2. **Source: Start Menu.** Shortcuts in the user's and the common Start Menu, subfolders included, handled like the Desktop ones. Uninstall shortcuts are skipped. An item found in two sources with the same name and target is listed once. Switchable in **Sources**.
3. **Row actions** on the selected item:
   - **Ctrl+Enter**: show it in its folder (Explorer opens the containing folder with the item selected). Files, folders and network paths only.
   - **Del**, while the recent list is shown (search box empty): remove the item from recents.

   The line under the search box lists the keys available for the selected item.
4. **Run as administrator.** **Ctrl+Shift+Enter** runs the selected item elevated (Windows asks for confirmation). Commands and executable files only (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`). Cancelling the Windows prompt is not an error.
5. **Installer.** A per-user setup (no administrator rights), self-contained (no .NET runtime needed). It adds a Start Menu shortcut, optionally a desktop shortcut and auto start, and an entry in *Apps & features* to uninstall. Setup and uninstall ask to quit coursee if it is running. Uninstalling keeps the user data.

## 0.3
1. **Code layout.** `Core` and `App` files are grouped in folders, one namespace per folder (see [ARCHITECTURE.md](ARCHITECTURE.md#projects)).
2. **Source: Edge favorites.** Bookmarks of the last used Edge profile, real links only (no folders). Switchable in **Sources**.
3. **Open in the right browser.** A Firefox bookmark opens in Firefox, an Edge favorite in Edge, also from recents. If that browser is not found, the default browser is used. The row label says which one ("Open in Firefox").
4. **Window positions.** The launcher and the log window reopen where they were left, on the same monitor; the log window also keeps its size. Settings, a dialog, opens centered on the launcher, at its last size. The launcher is moved by dragging its empty area. A window that is partly off screen (monitor unplugged, resolution or scaling changed, launcher dragged past an edge) is moved back inside a visible screen, each time it is shown.
5. **Hotkey conflict.** When the hotkey cannot be registered at start, Settings opens on it and explains that another application (or Windows) uses it. There is no silent switch to the default hotkey any more. Windows does not tell which application holds a hotkey, so coursee cannot name it.
6. **Theme.** Light, dark, or follow Windows (default), in Settings > Appearance.
7. **Settings window.** Settings is its own window instead of a panel of the launcher: a dialog over the launcher (closing it goes back to the launcher), which can be moved and resized. Settings are grouped in tabs: General, Appearance, Sources, Logging. The source checkboxes are built from the list of sources.
8. **Find a setting.** A search box at the top of Settings: while it has text, only the matching settings are shown, from every tab.
9. **Start hidden.** `coursee.exe --hidden` starts in the notification area only; the auto start entry uses it. Started without it (by the user or the installer), coursee shows the launcher.

## 0.4
1. **Launcher position.** Settings > General chooses where the launcher appears each time it is shown:
   - **Centered on the mouse's monitor** (default): in the middle of the monitor under the mouse.
   - **At the mouse**: centered on the mouse pointer.
   - **Where I left it**: its last position (as in 0.3).
   It is always kept fully on screen. The launcher can still be dragged; only *Where I left it* remembers where.
2. **List keys.** Page Up / Page Down move the selection by one page of the list, Ctrl+Home / Ctrl+End to the first / last item. Home and End stay with the search box (start / end of the text).
3. **Source: Windows settings.** A fixed list of Windows settings pages and tools, opened directly: Installed apps, Network, Display, Sound, Windows Update, Services, Device Manager, Event Viewer, Advanced system settings, Environment variables, Control Panel, and more. coursee only opens them, it never changes a setting. Names are written to be found (for example "Installed apps (uninstall programs)"); search still matches name and target only. Consoles and programs (`.msc`, `.exe`) can be run as administrator. Switchable in **Sources**.
4. **Esc clears first.** Esc with text in the search box clears it (the recent list is shown again); Esc with an empty box hides the launcher.
5. **Action always visible.** In the list, a long name or target is cut with "…"; the action column on the right is always shown.
6. **Action icons.** Each row shows its action (open file, open folder, open URL, open in a browser, run command, open setting) as an icon, with the wording as tooltip. The line under the search box is unchanged.
7. **Source icons.** Each row shows a small icon of the source it comes from (typed text has its own). The **Sources** tab shows the same icon next to each source.
8. **Keyboard only.** In the launcher, Tab moves the focus to the Settings button, Enter or Space opens Settings, Tab again goes back to the search box. In Settings: a logical Tab order, Ctrl+Tab / Ctrl+Shift+Tab switch tabs (arrows too, on the tab strip), Down in the Find box goes to the first setting shown, and the focused control is always visible.

## 0.5
1. **Ignore accents.** Matching ignores accents: `evene` finds *Événements*, `parametres` finds *Paramètres*. A setting in Settings > General, on by default, switches it off. Aliases always ignore accents.
2. **Copy the target.** Ctrl+C copies the selected entry's path, URL or command, and the launcher stays open (the second line under the search box says it was copied). If text is selected in the search box, Ctrl+C copies that text instead.
3. **Priority to opened entries.** An entry opened through coursee gets a score bonus that grows with how often and how recently it was opened. The bonus is capped, so a strong name match still beats a weak match of an often opened entry. The recent list is ordered the same way (recency plus frequency).
4. **Pinning.** Ctrl+P, or the pin icon at the right of the action icon, pins or unpins the selected entry. Pinned entries that match the query are listed first. With an empty search box, pinned entries are not shown (the recent list is).
5. **Aliases.** F2, or the alias icon at the right of the action icon, sets a short name of your own for the selected entry (`ff` for Firefox), in a box on its row: Enter saves, Esc cancels. Typing the exact alias (the whole search, ignoring case and accents) lists that entry first, before the pinned ones. An alias belongs to one entry: setting it on another entry moves it. F2 with an empty alias removes it. The pin and alias icons are shown on the selected row, and on the row under the mouse; a pinned entry always shows its pin. Pins and aliases belong to the entry's target, like recents. What was typed (the fallback) cannot be pinned or given an alias; custom entries (0.6) cover that.
6. **Click the action icon.** The action icon of a row (open file, open in Edge...) does the same as Enter on that row.
7. **Two hint lines.** Under the search box, the first line says what Enter does (with Ctrl and Shift), the second the other keys that apply: Ctrl+C, Ctrl+P (Pin or Unpin), F2, Del.
8. **Log window from Settings.** Settings > Logging > *Open log window* opens the log window in front, and Settings and the launcher stay open (until 0.4 both closed). The log window stays open after Settings is closed.

## 0.6
1. **Test coverage.** Done: the build fails below 95 % line coverage of `Core` (`build.cmd`, rule in `CLAUDE.md`), and writes an HTML report.
2. **Hide an entry.** Ctrl+H, or the hide icon on the row, hides the selected entry from results and recents, to remove noise such as duplicate shortcuts or old bookmarks. A hidden entry is that exact row: its source, name and target, so one of two shortcuts to the same program can be hidden. An alias of a hidden entry finds nothing. Typed text cannot be hidden. Hidden entries are restored in the **Entries** tab.
3. **Custom entries.** Your own name and target (a URL, folder, file or command), as a new **Custom** source, switchable in **Sources**. What the target is (URL, folder...) is decided from it, like typed text.
   * While the search box has text, a **+** button at the right of it, or Ctrl+N, creates a custom entry with the typed text as its target: a box on the typed text's row asks for its name (the typed text by default), Enter saves, Esc cancels. When something matched, that row is added first.
   * Custom entries are edited (name, target) and deleted in the **Entries** tab. A new target keeps the entry's pin, alias and recent history. Deleting one leaves its pin and alias, like a removed shortcut.
4. **Entries tab.** A new Settings tab listing hidden entries (to restore), custom entries (to edit or delete), aliases (to edit or remove) and pins (to unpin).
5. **Keys tab.** A new Settings tab lists the supported keys, read only. The hotkey moves there from General, and stays changeable.
6. **Commands like Win+R.** Done (fix): a typed or Run history command starts in the user's folder, and a program whose name has spaces runs without quotes (`kill rds.cmd`): the shortest part up to a space that is a file in that folder or in PATH (with PATHEXT) is the program.
7. **Desktop files and folders.** Done: the Desktop source also lists the files and folders placed directly on the Desktop (top level, not hidden or system), not only its shortcuts. They open as they are. In Settings it is named *Desktop*, and can be switched off like the other sources (until now it was always on).
8. **Alias icon like the pin.** Done: an entry with an alias always shows its alias icon, filled, as a pinned entry shows its pin.

## 0.7 (to define)
Theme: getting ready for other users.

1. **About tab.** A new Settings tab: version, links to the GitHub repository and its issues page.
2. **Update check.** Once a day, coursee asks GitHub for the latest release. If it is newer, the Settings button shows a mark and the About tab says so, with a link to download the installer. Nothing is downloaded or installed by coursee. A setting turns the check off. 
3. **Paths.** The About tab lists coursee's files (settings, entries, logs), each with a *Show in folder* button.
4. **Safety notice.** On first start, a notice says that typed text and commands run with the user's rights and can harm the system, and that coursee comes without warranty (see `LICENSE`). Acknowledged once.
5. **Turn off typed commands.** A setting (Settings > General, and offered by the notice) stops typed text from being run as a command (`del c:\file`). Typed URLs, folders and files still open. Run history entries are not affected.
6. **Folder sources.** One source per folder: its top-level files, folders and shortcuts are listed, like the Desktop source (subfolders not included). In **Sources**, *Add folder…* adds one, ✕ removes it, its checkbox switches it off. Desktop is the default folder and can be removed like any other.
   * **Quick access.** Each folder pinned in Explorer's *Quick access* (the standard ones and the user's own pins) is a folder source too, kept in sync with Explorer: pinning `D:\code` there makes its files searchable. These have a checkbox only (no ✕); they are removed by unpinning them in Explorer. An entry found in two folder sources (Desktop added and pinned) is listed once.

## 0.8 (to define)
1. **Portable.**  Package binaries for portable usage.
2. **Game launcher sources**. Installed games only; owned but not installed games are out of scope. coursee only reads local files and the registry: no sign-in, no API key. Signing in, when a game needs it, is left to the launcher.
	- **One source per launcher**, each an `IResourceSource`, switchable in **Sources**, with its own icon. A launcher that is not installed gives no entries, without error.
	- **Tolerant reading.** These formats are not documented and may change: a file that cannot be read is logged (Warning) and skipped; the other games and sources are not affected.
	- **Icons.** The game's exe icon when it can be found, else the source icon.
3. **Donate.** Add a link to donate to the developer.
4. **Sign installer.** To make it more secure, use a certificate to sign all installers coming from the developer.
5. **Release notes.** A release notes is maintained and committed. This file is provided inside installer and its opening is proposed at end of installer.
6. **End user manual.** A html manual is shipped with installer and available thru a "?" icon from App.
7. **Display scoring.** For helping debugging, the scoring of found entries is shown in the list.