# coursee: architecture

## Projects

```
coursee.App   (WPF, Windows only)
│  launcher, settings window, log window, tray icon
│  adapters: WshShortcutResolver, RegistryRunHistoryReader, RegistryAutoStart,
│            RegistryBrowserLocator, HotkeyService, SerilogFileLog, SingleInstance,
│            NativeWindows + WindowPlacer (window positions), ShellQuickAccessReader
│
│  implements ▼  IShortcutResolver · IRunHistoryReader · IQuickAccessReader · IAutoStart · IHotkeyRegistrar · IBrowserLocator · ILog
│
coursee.Core  (no Windows dependency, fully unit-tested on any OS)
   SearchEngine · RecentStore · EntryStore · CustomEntryStore · CustomEntryEditor · SettingsStore · SettingsCatalog · KeyBindings
   SourceCatalog · SourceLoader · SourceMonitor · FallbackClassifier
   ResourceActions · PlacementRules · PlacementStore
   sources (IResourceSource): CustomSource · ShortcutsSource (Desktop, folders, Start Menu) · FirefoxBookmarksSource · EdgeFavoritesSource · RunHistorySource
                              · WindowsSettingsSource (fixed list, ResourceKind.Setting)
```

- The sources live in `Core`. When one needs Windows (resolving `.lnk` files, reading the registry), it takes an interface defined in `Core`, and `App` supplies the implementation.
- `App.xaml.cs` wires everything together at startup.
- Tests: `tests/coursee.Core.Tests` (xUnit), `Core` only.

### Folders
One namespace per folder (`Coursee.Core.Sources`, `Coursee.UI.Views`, ...). The tests use the same folders as `Core`, and import every `Core` namespace once in their `.csproj`.

| `coursee.Core` | Content |
| --- | --- |
| *(root)* | `AppVersion` |
| `Resources/` | `Resource` and what can be done with it: `ResourceActions`, `ActionLabels` (the wording, and the `ResourceAction` behind the row icon), `FallbackClassifier`, `CommandLine` (program and arguments), `ProgramFinder` (the program's file, in the user's folder or PATH, so `kill rds.cmd` needs no quotes), `Browsers` |
| `Search/` | `SearchEngine`, `SearchBoosts` (what raises a result above its match), `TextFolding` (accents removed before matching), `ListNavigation` (moving the selection in the result list) |
| `Recents/` | `RecentStore`, `Frecency` (recency plus frequency) |
| `Entries/` | `EntryStore`: what the user set on entries (pins, aliases as `EntryAlias`, hidden entries as `HiddenEntry`); custom entries: `CustomEntry`, `CustomEntryStore`, `CustomEntryEditor`; `EntryFilter` (the filter of Settings > Entries) |
| `Settings/` | `SettingsStore`, `SettingsCatalog` (the entries of the Settings window, their tabs, and the search that finds them), `KeyBindings` (the keys listed in Settings > Keys) |
| `Sources/` | `IResourceSource`, `SourceCatalog` (the current list of sources, see [Folder sources](#folder-sources)), `FolderSources` (their naming rules), `IQuickAccessReader`, `SourceLoader`, `SourceMonitor`, `SourceIcons` (the icon of each source, found by name for a row), and each source with the interface it needs from `App` (`CustomSource` reads `CustomEntryStore`); `BookmarkUrls` and `ChromiumBookmarks` (Edge's file format, shared by Chrome) for the browser sources |
| `Platform/` | The other interfaces `App` implements (`IAutoStart`, `IHotkeyRegistrar`, `IBrowserLocator`) and their rules (`AutoStartCommand`, `HotkeyGesture`) |
| `Logging/` | `ILog`, `LogEntry`, `RingLog`, `CompositeLog`, `FilteredLog` |
| `Placement/` | Window positions: `WindowPlacement`, `PlacementRules`, `PlacementStore` (see [Window positions](#window-positions)) |

| `coursee.App` | Content |
| --- | --- |
| *(root)* | `App.xaml`: startup and wiring |
| `Views/` | Windows: `MainWindow` (the launcher; its entry actions in `MainWindow.Entries.cs`), `SettingsWindow`, `LogWindow`; `ResultRow` (a row of the launcher: the resource, its pin and alias, and what its box is open for: `RowEdit`); `EntriesPanel` and `EntryRows` (Settings > Entries); `MessageDialog` (messages and yes / no questions in the app's theme, instead of the Windows `MessageBox`); `AppTheme`; `ActionGlyphConverter` (the Segoe Fluent glyph of each `ResourceAction`), `SourceIconConverter` (the icon of a row's source) |
| `Tray/` | `TrayIcon` |
| `Platform/` | Implementations of the `Core` interfaces, `SingleInstance`, and window placement (`NativeWindows`, `WindowPlacer`) |
| `Logging/` | `LogHub`, `SerilogFileLog` |
| `Assets/` | `coursee.ico` |

## Stack
.NET 10, C#, WPF with its built-in Fluent theme for light and dark (`Application.ThemeMode`, `AppTheme`). WinForms only for the tray icon (`NotifyIcon`). xUnit for tests.

| Package | Project | Use |
| --- | --- | --- |
| `Microsoft.Data.Sqlite` | Core | Read Firefox bookmarks (`places.sqlite`) |
| `Serilog`, `Serilog.Sinks.File` | App | Log file |
| `coverlet.msbuild` | Tests | Coverage of `Core`, build fails below 95 % lines |
| `dotnet-reportgenerator-globaltool` | Local tool (`.config/dotnet-tools.json`) | HTML coverage report (`build.cmd`) |

Shared build settings and the version are in `Directory.Build.props`; output goes to `artifacts/`. Builds are deterministic and `DeterministicSourcePaths` replaces the local repo path with `/_/` in DLLs, PDBs and their Source Link data, so no local path (or user name) ships. The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) (`build/installer.cmd`, `build/installer/coursee.iss`), a tool installed on the PC, not a package.

## Data flow

```
hotkey ─► window shows the cached list at once
             │
             └─► background: SourceCatalog.SyncQuickAccess (pinned folders may have changed)
                 then SourceLoader.Load(enabled sources)
                   ├─ a failing source is logged and skipped, the others still load
                   ├─ SourceMonitor logs items added / removed per source
                   └─ SourceLoader.Merge: same name + target listed once (first source wins)
typing ─► EntryStore.Visible (hidden entries left out) ─► SearchEngine.Search(resources, query)
          (empty query: the RecentStore list, hidden entries left out too)
Enter  ─► shell opens Resource.OpenTarget, RecentStore records it
           (a bookmark with a Browser: that browser's .exe, found by IBrowserLocator; else the shell)
```

## Search
`SearchEngine` matches the name and the target only. Every word of the query must match. Each word scores, best first:

| Match | Score |
| --- | --- |
| Name starts with the word | 100 |
| Name contains the word at a word start | 80 |
| Name contains the word | 60 |
| Target (path, URL, command) contains the word | 30 |
| Word's letters appear in order in the name (`ffx` → *Firefox*) | 10 |

An entry opened through coursee then gains a **usage bonus**, added once to its total (not per word): see [Recents](#recents). Then, before the 50 are cut (see [Entries](#entries)):
1. A query that is exactly an entry's **alias** (whole query, trimmed, ignoring case and accents) lists that entry first, every item with its target, whether the name matches or not.
2. **Pinned** entries that match come next, ordered by their own scores.
3. Then all the others.

Results are sorted by total score, then by name, and capped at 50. Matching ignores case, and accents: `TextFolding.Fold` removes them from the query, names and targets before comparing (`Événements` → `Evenements`; letters such as `ß` or `œ` are kept). The `IgnoreAccents` setting (on by default) is passed to `SearchEngine.Search`; off, the texts are compared as they are, still ignoring case. Aliases always ignore accents.

## Recents
- `RecentStore` (`recents.json`) keeps the entries opened through coursee, by target, each with its last 10 open times (`Frecency.MaxOpens`). When full, the least recently opened entry goes first. Del on the recent list removes an entry with its history. The time comes from an injected `TimeProvider`, so tests move the clock.
- `Frecency` gives each open points by its age: 10 within a day, 5 within a week, 2 within a month, 1 after.
  - The **recent list** is ordered by the total, then by the last open.
  - The **search bonus** is the total capped at 25 (`Frecency.MaxBonus`). The cap keeps the match levels apart where they matter: a name start (100) beats letters in order plus the bonus (35), and a name match (60) beats a target match plus the bonus (55). A used entry can pass one level up, for example a word start (80 + 25) over a name start (100).
  - Only entries still in the list have a bonus (20 by default, Settings > General).
- `SearchEngine` gets the bonus through `SearchBoosts`, so it stays a pure function of its inputs.
- Up to 0.4 the file was a plain list of resources; it is read as each one opened once, in its order, and saved in the new format at the next open.

## Entries
- `EntryStore` (`entries.json`) keeps what the user set on entries: the pinned targets, and the aliases (stored as alias → target, as the user reads them).
- An alias belongs to one entry and an entry has one alias: `SetAlias` replaces the entry's old alias, and moves an alias another entry had (it returns that entry, for the log). An empty alias removes it. Aliases are compared with `TextFolding.AreEqual` (case and accents ignored). It is kept out of the sources, so adding a source still does not touch it, and out of `settings.json`: it is about entries, not how coursee behaves.
- An entry is known by its **target**, case-insensitive, as in recents. Two items with the same target (one program, two shortcut names) share their pin. A pin outlives a renamed shortcut and a source switched off.
- **Hidden entries** (Ctrl+H) are known by source, name and target (`HiddenEntry`, case-insensitive), not by target only: one of two shortcuts to the same program can be hidden. `EntryStore.Visible` leaves them out of the resources and of the recent list before searching, so a hidden entry's alias finds nothing.
- Typed text (the fallback) cannot be pinned, given an alias or hidden (`ResourceActions.CanPin`, `CanSetAlias`, `CanHide`): it is no entry, and would never be listed. It can become a custom entry (`CanMakeCustomEntry`).
- The launcher shows a `ResultRow` per item. The pin and alias icons are on the selected row and the row under the mouse (a click selects that row first), and the pin of a pinned entry, and the alias icon of an entry with an alias, are always shown, filled (the pin with the font's PinnedFill glyph; the tag with a `Path` of its shape, since the font has no filled tag); hidden (not collapsed) where they do not apply, so the action icons stay aligned. The action icon, on every row, is a button too: a click selects its row and opens it, like Enter. None of these buttons takes the focus, so it stays in the search box.
- F2 (alias) or Ctrl+N (name of a new custom entry) opens a text box in the selected row (`ResultRow.Edit`). While it is open, the launcher's keys are off (Enter saves, Esc cancels, everything else is typed in the box), a background refresh does not rebuild the list under it, and losing the focus cancels.
- Settings > Entries (`EntriesPanel`) lists what `EntryStore` and `CustomEntryStore` hold, to undo or change it: one list at a time, narrowed by `EntryFilter` (every word, ignoring case and accents). It fills the window's height with its own scroll, so `SettingsWindow` shows it in `EntriesHost` instead of the shared scrolled sections; in the Find results, the Entries row is a button that opens the tab. The launcher reloads its list and its sources when Settings closes.

### Custom entries
- `CustomEntryStore` (`custom.json`) keeps the user's entries (name, target), in the order they were made, one per target (case-insensitive). `CustomSource`, first in the `sources` array (on a duplicate, the user's entry wins), lists them; it reads the store on the background thread, so the store locks its list.
- What an entry is (URL, folder, file, network path, command) is not stored: `CustomEntry.ToResource` decides it at each load with `FallbackClassifier`, as for typed text. The stored target is the one that is opened (`www.x` is stored as `https://www.x`), so pins and aliases, keyed by target, match.
- `CustomEntryEditor.Create` makes one from the typed text (the + button, Ctrl+N, whenever the search box has text; the launcher adds the typed text's row first when something matched). `Edit` changes name and target, and moves what was set on the entry: pin, alias and hidden state (`EntryStore.Move`), recent history (`RecentStore.Move`, merged with an item that already had the new target). Deleting an entry leaves its pin and alias, like a shortcut removed from the Desktop.

## Folder sources
The list of sources changes while coursee runs, so `SourceCatalog` owns it; the launcher, Settings and `SourceIcons` read `SourceCatalog.All`.

```
settings.json "Folders" ─┐
  ["Desktop", "D:\code"] ├─► SourceCatalog ─► All ─► launcher refresh, Sources tab, SourceIcons
IQuickAccessReader ──────┘      │
  (ShellQuickAccessReader)      └─► Changed ─► Sources tab rebuilt
first (Custom) · last (Start Menu, Firefox, Edge, Run, Windows settings): fixed, from App.xaml.cs
```

| Topic | Rule |
| --- | --- |
| Order | first · the user's folders (`Folders`, the Desktop by default) · Quick access folders · last. A folder is listed once, the first wins: the Desktop covers both Desktop folders. A pinned Desktop is the Desktop source (its name, icon and both folders), shown with its path and no remove button like the other Quick access folders |
| A folder source | `ShortcutsSource.Folder`: top level only, shortcuts, files and folders, like the Desktop. It also lists the folder itself (the Desktop: the personal one), named as Explorer shows it when pinned: `FolderSources.EntryName` keeps both names when they differ (`Bureau (Desktop)`), so either finds it. Kept while listed, so shortcuts are not resolved again |
| Name | `Folder:<path>` (`FolderSources`), the same for a user and a Quick access folder: recents, hidden entries and the on / off state follow a folder from one to the other. The Desktop keeps `Desktop`, written `"Desktop"` in `Folders` |
| Add, remove | Settings > Sources. Quick access folders are indented under a *Quick access* checkbox that switches them all (`SourceCatalog.IsQuickAccess`, `SettingsStore.SetSourcesEnabled`; mixed when some are off). Choosing a Desktop folder adds the Desktop. Removing forgets the folder's off state; its pins and aliases stay. Quick access folders have no remove button: unpin them in Explorer |
| Network | Never listed (`FolderSources.IsNetwork`: `\\server\share`, mapped drives): an offline one blocks for seconds. Refused on add, skipped from Quick access |
| Quick access | Read at each launcher refresh, on a background thread. `ShellQuickAccessReader` runs the Shell COM object on its own STA thread (5 s timeout) and keeps folders whose `System.Home.IsPinned` is true, with their shown name (`PinnedFolder`) (Windows 11; frequent folders are left out). A failed read keeps the last folders read, and is logged (Warning) |
| Icons | Folder `\uE8B7`, Quick access `\uE80F`. A recent item from a folder that is gone gets the folder icon |
| Logs | Quick access changes at Info (category `Quick access`); a source that is off or gone is forgotten by `SourceMonitor.Keep`, and logged as loaded when it comes back |

## Adding a source
1. In `Core/Sources`, implement `IResourceSource`: a stable `Name` (it is also the key stored in `settings.json`), a `DisplayName` for Settings, `CanBeDisabled` (true gives it a checkbox in **Sources**), an `Icon` (a Segoe Fluent Icons glyph, kept in `SourceIcons` with the others so each stays distinct; shown on its rows and next to its checkbox), and `GetResources()` returning `Resource` records. Take an optional `ILog`.
2. If it needs Windows, define an interface in `Core` (like `IRunHistoryReader`) and implement it in `App`.
3. Register it in `App.xaml.cs`, in the `first` or `last` list of the `SourceCatalog` (before or after the folder sources). Order matters for duplicates: the first source wins.
4. Add xUnit tests in `tests/coursee.Core.Tests`, with a fake for any interface from step 2.
5. Update `README.md` and [ROADMAP.md](ROADMAP.md).

`GetResources()` runs on a background thread, at each launcher opening. It must not block for long, and may throw: `SourceLoader` catches the error, the window logs it and keeps the other sources.

## Window positions
- Saved in `windows.json` by window name (`Launcher`, `Settings`, `Log`) when a window is hidden or closed, restored when it is first shown. Kept out of `settings.json`: it is state, not a setting.
- Stored in screen pixels (WPF units change with each monitor's scaling), as the monitor's device name (`\\.\DISPLAY2`, steadier than a monitor number) and the position relative to its work area.
- `PlacementRules.Fit` (in `Core`, tested) makes a saved position visible again: back on its monitor, or on the primary one if it is gone, then shrunk to the work area and moved inside it.
- `PlacementRules.KeepVisible` does the same for a window on screen. `WindowPlacer.KeepOnScreen` applies it each time a window is shown, after a scaling change (`DpiChanged`), and after the launcher is dragged. It checks the visible frame (without the invisible resize borders), so a window snapped to an edge is left alone.
- `App` reads the monitors and window rectangles through Win32 (`NativeWindows`); WinForms' `Screen` is not used, so WinForms stays limited to the tray.
- A maximized window keeps its normal size, to be restored to it.
- What is restored depends on the window (`RestoreMode`): the launcher its position (its size is fixed), the log window its position and size, Settings its size only: as a dialog, it opens centered on the launcher (`PlacementRules.CenterOn`).
- The launcher's position also depends on the `LauncherPosition` setting. With `Centered` (default) or `AtMouse`, `WindowPlacer.PlaceLauncher` places it just before each show, from the mouse pointer (`NativeWindows.CursorPosition`): `PlacementRules.CenterOnMonitorAt` or `CenterOnPoint`, both on the monitor under the pointer (`MonitorAt`; the nearest one when the pointer is on a taskbar) and fully inside its work area. With `LastPosition`, the saved position is restored as above. Its position is saved on hide in every mode, so switching to `LastPosition` starts from the last place.

## Logging
- `Core` writes through `ILog` (`log.Info(category, message)`); only `App` knows Serilog.
- Category: the area (`App`, `Action`, `Hotkey`, a source name, ...).
- **Info**: every action the user triggers, start/stop, source changes (via `SourceMonitor`). **Warning/Error**: failures. **Debug**: details.
- Never logged: keystrokes in the search box, secrets.
- Local only: 3 files x 1 MB, plus the last 1000 entries in memory for the log window.

## Versioning
`<Version>` in `Directory.Build.props` is the only place the version is written. It is stamped into every assembly (AssemblyVersion, FileVersion, InformationalVersion), read back through `AppVersion` in `Core`, and passed to the installer by `build/installer.cmd`. Rules: [GOAL.md](GOAL.md#versioning).
