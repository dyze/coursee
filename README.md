# coursee

A personal Windows launcher. Press a global hotkey, type in the compact search window, press Enter to open a file, folder or bookmark.

| Doc | Content |
| --- | --- |
| [docs/GOAL.md](docs/GOAL.md) | Goal, principles, non-goals |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Scope of each version |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Projects, search ranking, adding a source, logging |

## Status

Searches your own custom entries, the Desktop and your own folders (shortcuts, files and folders), the folders pinned in Explorer's Quick access, Start Menu shortcuts, Firefox bookmarks, Edge favorites, the Windows Run history and Windows settings pages and tools. Also: recents, pins, aliases, hidden entries, row actions, tray icon, settings, installer.

## Install

```
build\installer.cmd
```

Builds `artifacts\installer\coursee-setup-<version>.exe` (needs the .NET 10 SDK and [Inno Setup](https://jrsoftware.org/isinfo.php) 6.3+). Run it to install coursee for your user only, no administrator rights: `%LOCALAPPDATA%\Programs\coursee`, with a Start Menu shortcut and, if chosen, a desktop shortcut and auto start. The app is self-contained, so the PC needs no .NET runtime.

Upgrade: run a newer setup over the old one. Uninstall: *Settings > Apps*. Your data (see below) is kept.

## Usage

| Key | Action |
| --- | --- |
| Ctrl+Alt+Space | Show / hide the launcher (configurable in Settings > Keys) |
| Type | Filter resources |
| Up / Down | Move selection |
| Page Up / Page Down | Move selection by one page |
| Ctrl+Home / Ctrl+End | Select the first / last item (Home and End move in the search box) |
| Enter | Open the selected resource, or act on the typed text if nothing matches (see below) |
| Ctrl+Enter | Show the selected file or folder in Explorer, item selected |
| Ctrl+Shift+Enter | Run the selected command, program or Windows tool as administrator (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`) |
| Ctrl+C | Copy the selected item's path, URL or command; the launcher stays open. With text selected in the search box, copies that text |
| Ctrl+P | Pin or unpin the selected item (also the pin icon on the selected row; a pinned item always shows it) |
| F2 | Set the selected item's alias, in a box on its row (also the alias icon; an item with an alias always shows it, filled): Enter saves, Esc cancels, empty removes it |
| Ctrl+H | Hide the selected item from results and recents (also the hide icon); restore it in Settings > Entries |
| Ctrl+N | Make the typed text a custom entry (also the **+** button right of the search box). A box on the typed text's row (added first when something matched) asks for its name: Enter saves, Esc cancels |
| Del | Remove the selected item from recents (search box empty) |
| Tab | Move the focus to the Settings button (Enter or Space opens Settings), and back |
| Esc | Clear the search box; when it is empty, hide the launcher |

Two lines under the search box say which keys apply to the selected item: what Enter does (with Ctrl and Shift) on the first, the other keys (Ctrl+C, Ctrl+P, F2, Ctrl+H, Ctrl+N, Del) on the second. Settings > Keys lists every key. In the list, an icon on the right of each row shows the same action (click it to do it, like Enter), and an icon on the left the source the item comes from (hover them for the wording); Settings > Sources shows the same source icons.

**Tray icon.** Click: show the launcher. Menu: Show, Settings, Log window, Quit.

**Typed text.** When nothing matches, Enter acts like Win+R: it opens a URL, folder or file, or runs the text as a command. Like Win+R, a command starts in your user folder, and its program is found there or in PATH even when its name has spaces (`kill rds.cmd`, no quotes needed). Network paths (`\\server\share`) are opened without being checked, so typing them never freezes the window. Running typed text as a command can be switched off (Settings > General): URLs, folders and files still open, and a custom entry (Ctrl+N) still runs its command. The first time the launcher shows, a safety notice explains this and offers the same switch.

## Settings

Gear button in the launcher, or tray menu > Settings. A dialog over the launcher: changes are saved at once, Esc (or closing it) goes back to the launcher. It can be moved and resized.

| Tab | Options |
| --- | --- |
| General | Number of recent items (default 20), launcher position, ignore accents when searching (default on), run typed text as a command (default on), start with Windows |
| Appearance | Theme: follow Windows (default), light or dark. The tray menu follows at the next start |
| Sources | Custom entries, Desktop, your folders, Quick access folders (grouped: one checkbox switches them all), Start Menu, Firefox bookmarks, Edge favorites, Windows Run history, Windows settings and tools (on / off). *Add folder...* adds a folder; ✕ removes one of yours (the Desktop too) |
| Entries | One list at a time, picked in a strip with counts: hidden entries (restore), custom entries (edit name and target, delete), aliases (edit, remove), pins (unpin). A filter box narrows every list (name, target, alias or source). Enter in a box saves its row |
| Keys | Hotkey (must include Ctrl, Alt or Win), and the list of keys (read only) |
| Logging | Detailed (debug) logging, open the log window |
| About | Version, links to the website (GitHub) and its issues page; coursee's files (settings, entries, recents, window positions, logs, program), each with its path and *Show in folder* |

**Find a setting**: type in the box at the top (Ctrl+F) to see only the matching settings, from every tab. Esc clears it.

**Keyboard only**: Down in the Find box goes to the first setting shown, Tab and Shift+Tab move between settings, Ctrl+Tab / Ctrl+Shift+Tab change tab (Left / Right too, on the tab strip). Space ticks a box, arrows change a list's choice.

## Search

- Matches the name and the target (path, URL, command), never file contents.
- Every word you type must match, in any order, ignoring case and accents (`parametres` finds *Paramètres*). Settings > General > *Ignore accents when searching* (on by default) can make accents count.
- Best matches first: name starts with the word, then a word inside the name, then anywhere in the name, then the target, then letters in order (`ffx` finds *Firefox*). Up to 50 results.
- Typing an item's alias exactly (the whole search, ignoring case and accents) lists that item first, even if its name does not match. An alias belongs to one item: giving it to another one moves it.
- Pinned items that match come next.
- Items you open often, and lately, move up. The boost is limited, so a much better name match still comes first.
- Empty search box: the recent list, items opened through coursee, the most used first (recent opens count more). Pinned items are not added to it.

Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#search).

## Behavior notes

- **Where the launcher appears** (Settings > General > Launcher position): in the middle of the monitor under the mouse (default), centered on the mouse, or where you left it. It can always be moved by dragging its empty area, but only *Where I left it* remembers the place.
- **Windows stay where you put them.** With *Where I left it*, the launcher reopens at its last place, on the same monitor; so does the log window, which also keeps its size. Settings opens centered on the launcher, at its last size. A window partly off screen (monitor gone, screen smaller, launcher dragged past an edge) is moved back on screen when it is shown, and the launcher also when you stop dragging it.
- **Start.** Started by you (Start Menu, desktop shortcut, end of setup), coursee shows the launcher. Started at sign-in, it stays in the notification area: the auto start entry runs `coursee.exe --hidden`. An entry written by an older version (without `--hidden`) is updated at the next start.
- **Start with Windows** is ticked only when Windows starts *this* copy. If it starts another one (say the installed copy, while you run a build), Settings shows its path; ticking the box switches auto start to this copy.
- **One copy at a time.** Starting coursee while it is already running shows the launcher of the running copy.
- **Pins and aliases** belong to an item's target (path, URL or command): two shortcuts to the same program share them, and they stay when a shortcut is renamed or a source is switched off and on. What you typed (a URL or command that matched nothing) cannot be pinned, given an alias or hidden.
- **Hidden items** are that exact row (source, name and target): hiding one of two shortcuts to the same program keeps the other. A hidden item's alias finds nothing.
- **Custom entries** are your own name for a URL, folder, file or command. What the target is, and so what Enter does, is decided like for typed text. Changing a target in Settings > Entries keeps the entry's pin, alias and recent history; deleting an entry keeps its pin and alias, like a removed shortcut.
- **Instant window.** Sources are reloaded in the background each time the launcher opens: it shows the previous list at once, and new or removed items appear a moment later.
- **Desktop and Start Menu shortcuts** are searched by their target, but opened through the shortcut itself, so their arguments and working folder are kept. Uninstall shortcuts are skipped, and a program found in both places is listed once.
- **Files and folders on the Desktop** (top level, not hidden or system ones) are listed too, by their name with its extension, and open as with a double click: `kill rds.cmd` runs.
- **Folder sources** work like the Desktop: shortcuts, files and folders at the top level, no subfolders. The folder itself is listed too, so typing its name opens it; a pinned folder also goes by the name Explorer shows (`Bureau` or `Desktop` both find the Desktop). Each folder pinned in Explorer's *Quick access* is one too, and follows Explorer: unpin it there to remove it (Settings only switches it off). A folder already listed (the Desktop, one of yours) is listed once. A pinned Desktop is the usual Desktop source: removed with ✕, it stays while pinned. Network folders are never searched: an offline one could block coursee. Quick access is read on Windows 11 only.
- **Firefox**: default profile only, bookmarks only (no folders), opened in Firefox.
- **Edge**: last used profile only, favorites only (no folders), opened in Edge.
- **Windows settings**: a fixed list of settings pages (Installed apps, Network, Display, Windows Update...), consoles (Services, Device Manager, Event Viewer...) and Control Panel items, opened directly. coursee never changes a setting. Names say what they are for, so "uninstall" finds *Installed apps*.
- **Right browser.** A bookmark opens in the browser it comes from, also from the recent list ("Open in Firefox"). If that browser cannot be found, the default browser is used. URLs you type open in the default browser.
- **Hotkey already taken?** If another application (or Windows) holds the hotkey at start, coursee keeps running without one and opens Settings on it, so you can choose another; meanwhile the tray icon opens the launcher. Windows does not tell which application holds a hotkey, so coursee cannot name it.

## Data and logs

Everything stays on your computer, in `%LOCALAPPDATA%\coursee`:

| File | Content |
| --- | --- |
| `settings.json` | Settings |
| `recents.json` | Recent list, with the last times each item was opened |
| `entries.json` | Pinned items, aliases and hidden items |
| `custom.json` | Custom entries |
| `windows.json` | Where each window was left |
| `logs\coursee.log` | Log, at most 3 files of 1 MB, oldest deleted |

coursee logs start/stop, items added / removed in each source, every action you trigger (opened resource, typed command, settings change) and errors. What you type in the search box is not logged.

**Log window**: tray menu > *Log window*, or Settings > Logging (Settings and the launcher stay open, the log window opens in front; it stays open after Settings is closed). Live view of the last 1000 entries, level and text filters, Copy, Clear (window only), Open log folder.

## Build

Requires the .NET 10 SDK.

```
build.cmd     # Windows
./build.sh    # Linux / macOS (builds and tests; the app itself only runs on Windows)
```

Both restore, build in Release and run the tests. Output goes to `artifacts/`. The version is set in `Directory.Build.props` only.

The build fails if `Core` line coverage is below **95 %**. `build.cmd` also writes an HTML coverage report to `artifacts/coverage/report/`; `build.cmd open` opens it in the browser.

## Layout

| Folder | Purpose |
| --- | --- |
| `src/` | `coursee.App` (WPF) and `coursee.Core` |
| `tests/` | Unit tests for `Core` |
| `docs/` | Documentation |
| `build/` | Installer script (`installer.cmd`, `installer/coursee.iss`), icon generator (`make_icon.py`) |
| `lib/` | Third-party binaries, if any |
| `packages/` | Local NuGet packages, if any |
| `samples/` | Samples |
| `artifacts/` | Build output (git-ignored) |
| `.config/` | Local dotnet tools (ReportGenerator) |
