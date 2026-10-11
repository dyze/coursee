# coursee

A personal Windows launcher. Press a global hotkey, type in the compact search window, press Enter to open a file, folder or bookmark.

See [docs/GOAL.md](docs/GOAL.md) for the goal, scope and non-goals.

## Status

Working: sources (Desktop and Start Menu shortcuts, Firefox bookmarks, Windows Run history), search, recents, row actions (show in folder, run as administrator), hotkey, tray icon, and a settings view.

## Usage

| Key | Action |
| --- | --- |
| Ctrl+Alt+Space (default, configurable in Settings; needs Ctrl, Alt or Win) | Show / hide the launcher |
| Type | Filter resources |
| Up / Down | Move selection |
| Enter | Open the selected resource. If nothing matches, act on the typed text like Win+R: open a URL, folder or file, or run a command. Network paths (`\\server\share`) are opened without being checked, so typing them never freezes the window. A line under the search box says which, and lists the other keys that apply |
| Ctrl+Enter | Show the selected file or folder in Explorer (containing folder, item selected) |
| Ctrl+Shift+Enter | Run the selected command or program as administrator (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`) |
| Del | On the recent list (search box empty): remove the selected item from recents |
| Gear button | Open / close settings (Windows integration: start with Windows; Sources: Start Menu, Firefox bookmarks, Windows Run history; Launcher: hotkey and number of recent items) |
| Log window | Tray menu > Log window, or Settings > Logging > Open log window |
| Esc | Close settings, or hide the launcher |
| Tray icon | Click: show the launcher. Menu: Show, Settings, Log window, Quit |

## Behavior notes

- **One copy at a time.** Starting coursee while it is already running shows the launcher of the running copy.
- **Instant window.** Sources are reloaded in the background each time the launcher opens: it shows the previous list at once, and new or removed items appear a moment later.
- **Desktop and Start Menu shortcuts** are searched by their target, but opened through the shortcut itself, so their arguments and working folder are kept. Uninstall shortcuts are skipped, and a program found in both places is listed once.
- **No hotkey available?** coursee keeps running: use the tray icon and choose another hotkey in Settings.

## Logging

coursee logs its activity to help debugging: start/stop, changes in each source (items added / removed), every action you trigger (opened resource, typed command, settings change) and errors.

- **Log window**: tray menu > *Log window*, or Settings > Logging > *Open log window*. Live view of the last 1000 entries, level filter, text filter, Copy, Clear (window only), Open log folder.
- **Files**: `%LOCALAPPDATA%\coursee\logs\coursee.log`. Size is limited: at most 3 files of 1 MB (about 3 MB), the oldest are deleted.
- **Level**: Info by default. Settings > Logging > *Detailed (debug) logging* adds more detail.
- **Privacy**: logs stay on your computer. What you type in the search box is not logged, only the action you trigger.

## Versioning

Versions are `MAJOR.MINOR`, starting at 0.1; the current version is the one in `Directory.Build.props` (scope of each version: [docs/GOAL.md](docs/GOAL.md)). It is set only there, and is stamped into all produced assemblies (including `coursee.exe`). The launcher window shows it at the bottom right.

## Architecture

```
coursee.App (WPF: hotkey, window, tray icon, shortcut resolver, log file + log window)
      │
coursee.Core (search, recents, IResourceSource)
      │
sources: ShortcutsSource (Desktop, Start Menu) · FirefoxBookmarksSource · RunHistorySource
```

`Core` has no Windows dependency, so it is fully unit-tested on any OS. Windows-specific parts (hotkey, tray icon, `.lnk` resolution, registry) live in `coursee.App`.

## Layout

| Folder | Purpose |
| --- | --- |
| `src/` | Application code |
| `tests/` | Unit tests |
| `docs/` | Documentation |
| `build/` | Build scripts and helpers (`installer.cmd` and `installer/coursee.iss` for the installer, `make_icon.py` for the app icon) |
| `lib/` | Third-party binaries, if any |
| `packages/` | Local NuGet packages, if any |
| `samples/` | Samples |
| `artifacts/` | Build output (git-ignored) |

## Install

```
build\installer.cmd
```

Builds `artifacts\installer\coursee-setup-<version>.exe` (needs the .NET 10 SDK and [Inno Setup](https://jrsoftware.org/isinfo.php) 6.3+). Run it to install coursee for your user only, no administrator rights: `%LOCALAPPDATA%\Programs\coursee`, with a Start Menu shortcut and, if chosen, a desktop shortcut and auto start. The app is self-contained, so the PC needs no .NET runtime.

Upgrade: run a newer setup over the old one. Uninstall: *Settings > Apps*. Your settings, recents and logs (`%LOCALAPPDATA%\coursee`) are kept.

## Build

Requires the .NET 10 SDK.

```
build.cmd     # Windows
./build.sh    # Linux / macOS (builds and tests; the app itself only runs on Windows)
```
