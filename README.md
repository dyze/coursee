# coursee

A personal Windows launcher. Press a global hotkey, type in the compact search window, press Enter to open a file, folder or bookmark.

| Doc | Content |
| --- | --- |
| [docs/GOAL.md](docs/GOAL.md) | Goal, principles, non-goals |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Scope of each version |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Projects, search ranking, adding a source, logging |

## Status

Searches Desktop and Start Menu shortcuts, Firefox bookmarks and the Windows Run history. Also: recents, row actions, tray icon, settings, installer.

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
| Enter | Open the selected resource, or act on the typed text if nothing matches (see below) |
| Ctrl+Enter | Show the selected file or folder in Explorer, item selected |
| Ctrl+Shift+Enter | Run the selected command or program as administrator (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`) |
| Del | Remove the selected item from recents (search box empty) |
| Esc | Hide the launcher |

The line under the search box always says what Enter will do and which other keys apply.

**Tray icon.** Click: show the launcher. Menu: Show, Settings, Log window, Quit.

**Typed text.** When nothing matches, Enter acts like Win+R: it opens a URL, folder or file, or runs the text as a command. Network paths (`\\server\share`) are opened without being checked, so typing them never freezes the window.

## Settings

Gear button in the launcher, or tray menu > Settings. A window of its own: changes are saved at once, Esc closes it.

| Tab | Options |
| --- | --- |
| General | Hotkey (must include Ctrl, Alt or Win), number of recent items (default 20), start with Windows |
| Appearance | Theme: follow Windows (default), light or dark. The tray menu follows at the next start |
| Sources | Start Menu, Firefox bookmarks, Windows Run history (on / off) |
| Logging | Detailed (debug) logging, open the log window |

**Find a setting**: type in the box at the top (Ctrl+F) to see only the matching settings, from every tab. Esc clears it.

## Search

- Matches the name and the target (path, URL, command), never file contents.
- Every word you type must match, in any order, ignoring case.
- Best matches first: name starts with the word, then a word inside the name, then anywhere in the name, then the target, then letters in order (`ffx` finds *Firefox*). Up to 50 results.
- Empty search box: the recent list, items opened through coursee, most recent first.

Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#search).

## Behavior notes

- **Windows stay where you put them.** The launcher (drag its empty area to move it), Settings and the log window reopen at their last place, on the same monitor; Settings and the log window also keep their size. If that monitor is gone, or a window no longer fits, it is moved back on screen.
- **One copy at a time.** Starting coursee while it is already running shows the launcher of the running copy.
- **Instant window.** Sources are reloaded in the background each time the launcher opens: it shows the previous list at once, and new or removed items appear a moment later.
- **Desktop and Start Menu shortcuts** are searched by their target, but opened through the shortcut itself, so their arguments and working folder are kept. Uninstall shortcuts are skipped, and a program found in both places is listed once.
- **Firefox**: default profile only, bookmarks only (no folders), opened in the default browser.
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

**Log window**: tray menu > *Log window*, or Settings > Logging. Live view of the last 1000 entries, level and text filters, Copy, Clear (window only), Open log folder.

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
