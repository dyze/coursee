# coursee

A personal Windows launcher. Press a global hotkey, type in the compact search window, press Enter to open a file, folder or bookmark.

See [docs/GOAL.md](docs/GOAL.md) for the goal, scope and non-goals.

## Status

Skeleton. Working so far: the Desktop shortcuts source (targets only), search, recents, hotkey and the compact window. Firefox bookmarks and a tray icon come next.

## Usage

| Key | Action |
| --- | --- |
| Ctrl+Alt+Space | Show / hide the launcher |
| Type | Filter resources |
| Up / Down | Move selection |
| Enter | Open the selected resource |
| Esc | Hide |
| Ctrl+Q | Quit (temporary, until the tray icon exists) |

## Architecture

```
coursee.App (WPF: hotkey, window, shortcut resolver)
      │
coursee.Core (search, recents, IResourceSource)
      │
sources: DesktopShortcutsSource · (next) FirefoxBookmarksSource
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
