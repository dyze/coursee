# coursee

A personal Windows launcher. Press a global hotkey, type in the compact search window, press Enter to open a file, folder or bookmark.

See [docs/GOAL.md](docs/GOAL.md) for the goal, scope and non-goals.

## Status

Skeleton. Working so far: the Desktop shortcuts source (targets only), search, recents, hotkey, the compact window and a settings view (auto start with Windows). Firefox bookmarks (default profile, can be switched off in Settings) are included; a tray icon comes next.

## Usage

| Key | Action |
| --- | --- |
| Ctrl+Alt+Space (default, configurable in Settings) | Show / hide the launcher |
| Type | Filter resources |
| Up / Down | Move selection |
| Enter | Open the selected resource. If nothing matches, act on the typed text like Win+R: open a URL, folder or file, or run a command. A line under the search box says which |
| Gear button | Open / close settings (Windows integration: start with Windows; Sources: Firefox bookmarks; Launcher: hotkey and number of recent items) |
| Esc | Close settings, or hide the launcher |
| Ctrl+Q | Quit (temporary, until the tray icon exists) |

## Versioning

Versions are `MAJOR.MINOR`: the first is **0.1**, the next **0.2**. The number is set once, in `Directory.Build.props`, and is stamped into all produced assemblies (including `coursee.exe`). The launcher window shows it at the bottom right.

## Architecture

```
coursee.App (WPF: hotkey, window, shortcut resolver)
      │
coursee.Core (search, recents, IResourceSource)
      │
sources: DesktopShortcutsSource · FirefoxBookmarksSource
```

`Core` has no Windows dependency, so it is fully unit-tested on any OS. Windows-specific parts (hotkey, `.lnk` resolution) live in `coursee.App`.

## Layout

| Folder | Purpose |
| --- | --- |
| `src/` | Application code |
| `tests/` | Unit tests |
| `docs/` | Documentation |
| `build/` | Build scripts and helpers |
| `lib/` | Third-party binaries, if any |
| `packages/` | Local NuGet packages, if any |
| `samples/` | Samples |
| `artifacts/` | Build output (git-ignored) |

## Build

Requires the .NET 10 SDK.

```
build.cmd     # Windows
./build.sh    # Linux / macOS (builds and tests; the app itself only runs on Windows)
```
