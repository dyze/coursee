# Changelog

All notable changes to coursee are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow coursee's own `MAJOR.MINOR` scheme ([docs/GOAL.md](docs/GOAL.md#versioning)), not Semantic Versioning. Planned versions: [docs/ROADMAP.md](docs/ROADMAP.md).

## [Unreleased]

### Fixed
- **Recents follow the sources.** The recent list leaves out the items of a source switched off in Settings > Sources (until now, they were still listed). Their history is kept: they come back when the source is switched on. The recent list is updated as soon as a source is switched, like search results.

## [0.8] - 2026-10-15

### Added
- **Portable zip.** Each release also has `coursee-portable-<version>.zip`: the self-contained coursee.exe and a `portable.txt` marker. While the marker is next to the exe, coursee keeps its data (settings, entries, logs) in a `data` folder beside it instead of `%LOCALAPPDATA%\coursee`, so it runs from a USB stick. The update check links to the release page, which has both the installer and the zip.
- **Changelog.** This file replaces the released versions' sections of the roadmap. The installer and the zip include it; the last page of setup offers to open it, and *Show changelog* in Settings > About opens it in a window of its own.
- **Source: Packaged apps.** Store / MSIX apps (1Password, Windows Terminal, Calculator, Xbox / Game Pass games...) have no `.lnk` in the Start Menu folders, so the Start Menu source missed them. This source lists the installed packaged apps, as Windows Start does, and opens them by their app ID (`shell:AppsFolder\<AppUserModelID>`). Run as administrator and *Show in folder* do not apply. Switchable in **Sources**, with its own icon.
- **Game launcher sources.** Installed games of Steam, Epic Games and GOG; owned but not installed games are out of scope. coursee only reads local files and the registry: no sign-in, no API key. Signing in, when a game needs it, is left to the launcher.
  - **One source per launcher**, switchable in **Sources** (on by default), with its own icon. A launcher that is not installed gives no entries, without error.
  - **Started by the launcher.** A game opens through its launcher's link (`steam://rungameid/<id>`, `com.epicgames.launcher://apps/<id>?action=launch`...), so the launcher still handles updates and checks. GOG Galaxy has no such link: a GOG game starts with the command of GOG Galaxy's own shortcuts (`GalaxyClient.exe /command=runGame /gameId=<id> /path=<folder>`). Without GOG Galaxy (offline installers), it starts from its own program, as GOG's registry gives it (GOG games have no DRM). The row says *Launch game*; Ctrl+C copies the link.
  - **Games only.** Tools Steam lists as apps (Proton, Steamworks Common Redistributables...) are skipped, and so are Epic's add-ons and engines.
  - **Tolerant reading.** These formats are not documented and may change: a file that cannot be read is logged (Warning) and skipped; the other games and sources are not affected.

- **Source groups.** In Settings > Sources, the sources come in groups, each with a checkbox that switches them all, like Quick access: **Folders** (yours, with *Add folder...* on the group's row), **Windows** (Start Menu, packaged apps, Run history, settings), **Browsers** (Firefox, Edge) and **Games** (Steam, Epic Games, GOG).
- **Load times.** With detailed (Debug) logging, each refresh logs how long every source took to read, Quick access, and the whole refresh.

### Changed
- **Compact log window.** One short line per entry.
- **About buttons.** *Website (GitHub)*, *Report an issue* and *Show changelog* are buttons, side by side.
- **Up to date in green.** "You have the latest version" shows in green, as a newer release shows in yellow.
- **Log window in the tray menu** is ticked while the window is open; a click shows or closes it.
- **Listed once by name.** A packaged app or a game also found by an earlier source, such as the Start Menu (same name, ignoring case), is listed once, as that entry. A game replaces a shortcut, though, so a GOG game shows as a game rather than as its Start Menu or Desktop shortcut.
- **8 files instead of 264.** `coursee.exe` now holds the app and the .NET runtime, next to six native DLLs (WPF, SQLite) and the changelog. Still self-contained: no .NET runtime to install. No `.pdb` files or translated framework messages.

### Fixed
- **Upgrades leave no old files.** The setup removes what an older version shipped before copying the new files (until now, files a version no longer needed stayed).

## [0.7] - 2026-10-14

Getting ready for other users.

### Added
- **About tab.** A new Settings tab: version, links to the GitHub repository and its issues page.
- **Update check.** Once a day, coursee asks GitHub for the latest release. If it is newer, the Settings button shows a mark and the About tab says so, with a link to download the installer. Nothing is downloaded or installed by coursee. A setting turns the check off.
- **Paths.** The About tab lists coursee's files (settings, entries, logs), each with a *Show in folder* button.
- **Safety notice.** The first time the launcher shows, a notice says that typed text and commands run with the user's rights and can harm the system, and that coursee comes without warranty. Acknowledged once (`SafetyNoticeSeen`).
- **Turn off typed commands.** A setting (Settings > General, and offered by the notice) stops typed text from being run as a command (`del c:\file`). Typed URLs, folders and files still open. Run history entries are not affected.
- **Folder sources.** One source per folder: its top-level files, folders and shortcuts are listed, like the Desktop source (subfolders not included). In **Sources**, *Add folder…* adds one, ✕ removes it, its checkbox switches it off. Desktop is the default folder and can be removed like any other. Network folders are never listed; Quick access needs Windows 11. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#folder-sources).
  - **Quick access.** Each folder pinned in Explorer's *Quick access* (the standard ones and the user's own pins) is a folder source too, kept in sync with Explorer: pinning `D:\code` there makes its files searchable. These have a checkbox only (no ✕); they are removed by unpinning them in Explorer. An entry found in two folder sources (Desktop added and pinned) is listed once.

## [0.6] - 2026-10-14

### Added
- **Hide an entry.** Ctrl+H, or the hide icon on the row, hides the selected entry from results and recents, to remove noise such as duplicate shortcuts or old bookmarks. A hidden entry is that exact row: its source, name and target, so one of two shortcuts to the same program can be hidden. An alias of a hidden entry finds nothing. Typed text cannot be hidden. Hidden entries are restored in the **Entries** tab.
- **Custom entries.** Your own name and target (a URL, folder, file or command), as a new **Custom** source, switchable in **Sources**. What the target is (URL, folder...) is decided from it, like typed text.
  - While the search box has text, a **+** button at the right of it, or Ctrl+N, creates a custom entry with the typed text as its target: a box on the typed text's row asks for its name (the typed text by default), Enter saves, Esc cancels. When something matched, that row is added first.
  - Custom entries are edited (name, target) and deleted in the **Entries** tab. A new target keeps the entry's pin, alias and recent history. Deleting one leaves its pin and alias, like a removed shortcut.
- **Entries tab.** A new Settings tab listing hidden entries (to restore), custom entries (to edit or delete), aliases (to edit or remove) and pins (to unpin).
- **Keys tab.** A new Settings tab lists the supported keys, read only. The hotkey moves there from General, and stays changeable.
- **Desktop files and folders.** The Desktop source also lists the files and folders placed directly on the Desktop (top level, not hidden or system), not only its shortcuts. They open as they are.

### Changed
- **Test coverage.** The build fails below 95 % line coverage of `Core` (`build.cmd`, rule in `CLAUDE.md`), and writes an HTML report.
- **Commands like Win+R.** A typed or Run history command starts in the user's folder, and a program whose name has spaces runs without quotes (`kill rds.cmd`): the shortest part up to a space that is a file in that folder or in PATH (with PATHEXT) is the program.
- **Desktop source.** In Settings it is named *Desktop*, and can be switched off like the other sources (until now it was always on).
- **Alias icon like the pin.** An entry with an alias always shows its alias icon, filled, as a pinned entry shows its pin.

## [0.5] - 2026-10-13

### Added
- **Ignore accents.** Matching ignores accents: `evene` finds *Événements*, `parametres` finds *Paramètres*. A setting in Settings > General, on by default, switches it off. Aliases always ignore accents.
- **Copy the target.** Ctrl+C copies the selected entry's path, URL or command, and the launcher stays open (the second line under the search box says it was copied). If text is selected in the search box, Ctrl+C copies that text instead.
- **Priority to opened entries.** An entry opened through coursee gets a score bonus that grows with how often and how recently it was opened. The bonus is capped, so a strong name match still beats a weak match of an often opened entry. The recent list is ordered the same way (recency plus frequency).
- **Pinning.** Ctrl+P, or the pin icon at the right of the action icon, pins or unpins the selected entry. Pinned entries that match the query are listed first. With an empty search box, pinned entries are not shown (the recent list is).
- **Aliases.** F2, or the alias icon at the right of the action icon, sets a short name of your own for the selected entry (`ff` for Firefox), in a box on its row: Enter saves, Esc cancels. Typing the exact alias (the whole search, ignoring case and accents) lists that entry first, before the pinned ones. An alias belongs to one entry: setting it on another entry moves it. F2 with an empty alias removes it. The pin and alias icons are shown on the selected row, and on the row under the mouse; a pinned entry always shows its pin. Pins and aliases belong to the entry's target, like recents. What was typed (the fallback) cannot be pinned or given an alias; custom entries (0.6) cover that.
- **Click the action icon.** The action icon of a row (open file, open in Edge...) does the same as Enter on that row.

### Changed
- **Two hint lines.** Under the search box, the first line says what Enter does (with Ctrl and Shift), the second the other keys that apply: Ctrl+C, Ctrl+P (Pin or Unpin), F2, Del.
- **Log window from Settings.** Settings > Logging > *Open log window* opens the log window in front, and Settings and the launcher stay open (until 0.4 both closed). The log window stays open after Settings is closed.

## 0.4 - 2026-10-13

### Added
- **Launcher position.** Settings > General chooses where the launcher appears each time it is shown:
  - **Centered on the mouse's monitor** (default): in the middle of the monitor under the mouse.
  - **At the mouse**: centered on the mouse pointer.
  - **Where I left it**: its last position (as in 0.3).

  It is always kept fully on screen. The launcher can still be dragged; only *Where I left it* remembers where.
- **List keys.** Page Up / Page Down move the selection by one page of the list, Ctrl+Home / Ctrl+End to the first / last item. Home and End stay with the search box (start / end of the text).
- **Source: Windows settings.** A fixed list of Windows settings pages and tools, opened directly: Installed apps, Network, Display, Sound, Windows Update, Services, Device Manager, Event Viewer, Advanced system settings, Environment variables, Control Panel, and more. coursee only opens them, it never changes a setting. Names are written to be found (for example "Installed apps (uninstall programs)"); search still matches name and target only. Consoles and programs (`.msc`, `.exe`) can be run as administrator. Switchable in **Sources**.
- **Action icons.** Each row shows its action (open file, open folder, open URL, open in a browser, run command, open setting) as an icon, with the wording as tooltip. The line under the search box is unchanged.
- **Source icons.** Each row shows a small icon of the source it comes from (typed text has its own). The **Sources** tab shows the same icon next to each source.
- **Keyboard only.** In the launcher, Tab moves the focus to the Settings button, Enter or Space opens Settings, Tab again goes back to the search box. In Settings: a logical Tab order, Ctrl+Tab / Ctrl+Shift+Tab switch tabs (arrows too, on the tab strip), Down in the Find box goes to the first setting shown, and the focused control is always visible.

### Changed
- **Esc clears first.** Esc with text in the search box clears it (the recent list is shown again); Esc with an empty box hides the launcher.
- **Action always visible.** In the list, a long name or target is cut with "…"; the action column on the right is always shown.

## 0.3 - 2026-10-12

### Added
- **Source: Edge favorites.** Bookmarks of the last used Edge profile, real links only (no folders). Switchable in **Sources**.
- **Open in the right browser.** A Firefox bookmark opens in Firefox, an Edge favorite in Edge, also from recents. If that browser is not found, the default browser is used. The row label says which one ("Open in Firefox").
- **Window positions.** The launcher and the log window reopen where they were left, on the same monitor; the log window also keeps its size. Settings, a dialog, opens centered on the launcher, at its last size. The launcher is moved by dragging its empty area. A window that is partly off screen (monitor unplugged, resolution or scaling changed, launcher dragged past an edge) is moved back inside a visible screen, each time it is shown.
- **Theme.** Light, dark, or follow Windows (default), in Settings > Appearance.
- **Find a setting.** A search box at the top of Settings: while it has text, only the matching settings are shown, from every tab.
- **Start hidden.** `coursee.exe --hidden` starts in the notification area only; the auto start entry uses it. Started without it (by the user or the installer), coursee shows the launcher.

### Changed
- **Code layout.** `Core` and `App` files are grouped in folders, one namespace per folder (see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#projects)).
- **Hotkey conflict.** When the hotkey cannot be registered at start, Settings opens on it and explains that another application (or Windows) uses it. There is no silent switch to the default hotkey any more. Windows does not tell which application holds a hotkey, so coursee cannot name it.
- **Settings window.** Settings is its own window instead of a panel of the launcher: a dialog over the launcher (closing it goes back to the launcher), which can be moved and resized. Settings are grouped in tabs: General, Appearance, Sources, Logging. The source checkboxes are built from the list of sources.

## 0.2 - 2026-10-12

### Added
- **Tray icon.** Click: show the launcher. Menu: *Show coursee* (with the current hotkey), *Settings*, *Log window*, *Quit*. If no hotkey can be registered, coursee keeps running and is reached from the tray. coursee.exe and the tray use the same icon.
- **Source: Start Menu.** Shortcuts in the user's and the common Start Menu, subfolders included, handled like the Desktop ones. Uninstall shortcuts are skipped. An item found in two sources with the same name and target is listed once. Switchable in **Sources**.
- **Row actions** on the selected item:
  - **Ctrl+Enter**: show it in its folder (Explorer opens the containing folder with the item selected). Files, folders and network paths only.
  - **Del**, while the recent list is shown (search box empty): remove the item from recents.

  The line under the search box lists the keys available for the selected item.
- **Run as administrator.** **Ctrl+Shift+Enter** runs the selected item elevated (Windows asks for confirmation). Commands and executable files only (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`). Cancelling the Windows prompt is not an error.
- **Installer.** A per-user setup (no administrator rights), self-contained (no .NET runtime needed). It adds a Start Menu shortcut, optionally a desktop shortcut and auto start, and an entry in *Apps & features* to uninstall. Setup and uninstall ask to quit coursee if it is running. Uninstalling keeps the user data.

### Removed
- The temporary Ctrl+Q shortcut (Quit is in the tray menu).

## 0.1 - 2026-10-11

### Added
- **Hotkey and window.** A global hotkey toggles the compact window. Only one copy of coursee runs: starting it again shows the launcher of the running copy.
- **Source: Desktop shortcuts.** Shortcuts (.lnk) on the Windows Desktop, resolved to their targets (files and folders). The target is what is shown and searched; Enter opens the shortcut itself, so its arguments and working folder are kept.
- **Search** with filtering as you type.
- **Recents.** What was opened via coursee, shown when the search box is empty.
- **Open and hide.** Enter opens the target; Esc hides the window.
- **Version** shown in the window.
- **Settings view** (gear button). Section **Windows integration**: auto start on or off, when the user signs in to Windows, with no administrator rights.
- **Source: Firefox bookmarks.** Default profile only, real bookmarks only (no folders or smart queries), opened in the default browser. Section **Sources** switches it on or off.
- **Launcher settings.** Section **Launcher**: the global hotkey (default Ctrl+Alt+Space; it must include Ctrl, Alt or Win, since Shift alone would block normal typing) and how many recent items are kept (default 20).
- **Fallback action**, like the Run dialog (Win+R): when the typed text matches no resource, Enter acts on the text itself. It opens a URL (`http(s)://`, `ftp://` or `www.`), opens an existing folder or file (quotes and `%VARS%` allowed), opens a network path (`\\server\share`, not checked on disk so typing never blocks), or runs the text as a command. The window always shows what Enter will do: a line under the search box ("Enter: Open folder") and a label on every row. Typed actions are added to recents.
- **Source: Windows Run history.** The commands previously typed in Win+R, most recent first, are searchable, to ease the move away from Win+R. Switchable in **Sources**.
- **Logging**, for debugging and feedback: start/stop, changes in each source (items added / removed at each refresh), every action the user triggers, and errors. Written to a size-limited local file and shown live in a **log window** (Settings, section **Logging**) with level and text filters, copy and clear. Info by default; a setting turns on detailed (Debug) logging. Never logged: keystrokes while searching, secrets.

[Unreleased]: https://github.com/dyze/coursee/compare/0.8...HEAD
[0.8]: https://github.com/dyze/coursee/compare/0.7...0.8
[0.7]: https://github.com/dyze/coursee/compare/0.6...0.7
[0.6]: https://github.com/dyze/coursee/compare/0.5...0.6
[0.5]: https://github.com/dyze/coursee/releases/tag/0.5
