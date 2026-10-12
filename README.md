# coursee

A personal Windows launcher. Press a global hotkey, type in the compact search window, press Enter to open a file, folder or bookmark.

| Doc | Content |
| --- | --- |
| [docs/GOAL.md](docs/GOAL.md) | Goal, principles, non-goals |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Scope of each version |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Projects, search ranking, adding a source, logging |

## Status

Searches Desktop and Start Menu shortcuts, Firefox bookmarks, Edge favorites and the Windows Run history. Also: recents, row actions, tray icon, settings, installer.

## Install

```
build\installer.cmd
```

Builds `artifacts\installer\coursee-setup-<version>.exe` (needs the .NET 10 SDK and [Inno Setup](https://jrsoftware.org/isinfo.php) 6.3+). Run it to install coursee for your user only, no administrator rights: `%LOCALAPPDATA%\Programs\coursee`, with a Start Menu shortcut and, if chosen, a desktop shortcut and auto start. The app is self-contained, so the PC needs no .NET runtime.

Upgrade: run a newer setup over the old one. Uninstall: *Settings > Apps*. Your data (see below) is kept.

## Usage

| Key | Action |
| --- | --- |
| Ctrl+Alt+Space | Show / hide the launcher (configurable in Settings) |
| Type | Filter resources |
| Up / Down | Move selection |
| Page Up / Page Down | Move selection by one page |
| Ctrl+Home / Ctrl+End | Select the first / last item (Home and End move in the search box) |
| Enter | Open the selected resource, or act on the typed text if nothing matches (see below) |
| Ctrl+Enter | Show the selected file or folder in Explorer, item selected |
| Ctrl+Shift+Enter | Run the selected command or program as administrator (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`) |
| Del | Remove the selected item from recents (search box empty) |
| Esc | Clear the search box; when it is empty, hide the launcher |

The line under the search box always says what Enter will do and which other keys apply. In the list, an icon on the right of each row shows the same action, and an icon on the left the source the item comes from (hover them for the wording); Settings > Sources shows the same source icons.

**Tray icon.** Click: show the launcher. Menu: Show, Settings, Log window, Quit.

**Typed text.** When nothing matches, Enter acts like Win+R: it opens a URL, folder or file, or runs the text as a command. Network paths (`\\server\share`) are opened without being checked, so typing them never freezes the window.

## Settings

Gear button in the launcher, or tray menu > Settings. A dialog over the launcher: changes are saved at once, Esc (or closing it) goes back to the launcher. It can be moved and resized.

| Tab | Options |
| --- | --- |
| General | Hotkey (must include Ctrl, Alt or Win), number of recent items (default 20), start with Windows |
| Appearance | Theme: follow Windows (default), light or dark. The tray menu follows at the next start |
| Sources | Start Menu, Firefox bookmarks, Edge favorites, Windows Run history (on / off) |
| Logging | Detailed (debug) logging, open the log window |

**Find a setting**: type in the box at the top (Ctrl+F) to see only the matching settings, from every tab. Esc clears it.

## Search

- Matches the name and the target (path, URL, command), never file contents.
- Every word you type must match, in any order, ignoring case.
- Best matches first: name starts with the word, then a word inside the name, then anywhere in the name, then the target, then letters in order (`ffx` finds *Firefox*). Up to 50 results.
- Empty search box: the recent list, items opened through coursee, most recent first.

Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#search).

## Behavior notes

- **Windows stay where you put them.** The launcher (drag its empty area to move it) and the log window reopen at their last place, on the same monitor; the log window also keeps its size. Settings opens centered on the launcher, at its last size. A window partly off screen (monitor gone, screen smaller, launcher dragged past an edge) is moved back on screen when it is shown, and the launcher also when you stop dragging it.
- **Start.** Started by you (Start Menu, desktop shortcut, end of setup), coursee shows the launcher. Started at sign-in, it stays in the notification area: the auto start entry runs `coursee.exe --hidden`. An entry written by an older version (without `--hidden`) is updated at the next start.
- **Start with Windows** is ticked only when Windows starts *this* copy. If it starts another one (say the installed copy, while you run a build), Settings shows its path; ticking the box switches auto start to this copy.
- **One copy at a time.** Starting coursee while it is already running shows the launcher of the running copy.
- **Instant window.** Sources are reloaded in the background each time the launcher opens: it shows the previous list at once, and new or removed items appear a moment later.
- **Desktop and Start Menu shortcuts** are searched by their target, but opened through the shortcut itself, so their arguments and working folder are kept. Uninstall shortcuts are skipped, and a program found in both places is listed once.
- **Firefox**: default profile only, bookmarks only (no folders), opened in Firefox.
- **Edge**: last used profile only, favorites only (no folders), opened in Edge.
- **Right browser.** A bookmark opens in the browser it comes from, also from the recent list ("Open in Firefox"). If that browser cannot be found, the default browser is used. URLs you type open in the default browser.
- **Hotkey already taken?** If another application (or Windows) holds the hotkey at start, coursee keeps running without one and opens Settings on it, so you can choose another; meanwhile the tray icon opens the launcher. Windows does not tell which application holds a hotkey, so coursee cannot name it.

## Data and logs

Everything stays on your computer, in `%LOCALAPPDATA%\coursee`:

| File | Content |
| --- | --- |
| `settings.json` | Settings |
| `recents.json` | Recent list |
| `windows.json` | Where each window was left |
| `logs\coursee.log` | Log, at most 3 files of 1 MB, oldest deleted |

coursee logs start/stop, items added / removed in each source, every action you trigger (opened resource, typed command, settings change) and errors. What you type in the search box is not logged.

**Log window**: tray menu > *Log window*, or Settings > Logging (this closes Settings and the launcher, so the log window stays open on its own). Live view of the last 1000 entries, level and text filters, Copy, Clear (window only), Open log folder.

## Build

Requires the .NET 10 SDK.

```
build.cmd     # Windows
./build.sh    # Linux / macOS (builds and tests; the app itself only runs on Windows)
```

Both restore, build in Release and run the tests. Output goes to `artifacts/`. The version is set in `Directory.Build.props` only.

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
