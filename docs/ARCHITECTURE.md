# coursee: architecture

## Projects

```
coursee.App   (WPF, Windows only)
│  window, tray icon, settings view, log window
│  adapters: WshShortcutResolver, RegistryRunHistoryReader, RegistryAutoStart,
│            HotkeyService, SerilogFileLog, SingleInstance
│
│  implements ▼  IShortcutResolver · IRunHistoryReader · IAutoStart · IHotkeyRegistrar · ILog
│
coursee.Core  (no Windows dependency, fully unit-tested on any OS)
   SearchEngine · RecentStore · SettingsStore · SourceLoader · SourceMonitor · FallbackClassifier
   sources (IResourceSource): ShortcutsSource (Desktop, Start Menu) · FirefoxBookmarksSource · RunHistorySource
```

- The sources live in `Core`. When one needs Windows (resolving `.lnk` files, reading the registry), it takes an interface defined in `Core`, and `App` supplies the implementation.
- `App.xaml.cs` wires everything together at startup.
- Tests: `tests/coursee.Core.Tests` (xUnit), `Core` only.

## Stack
.NET 10, C#, WPF. WinForms only for the tray icon (`NotifyIcon`). xUnit for tests.

| Package | Project | Use |
| --- | --- | --- |
| `Microsoft.Data.Sqlite` | Core | Read Firefox bookmarks (`places.sqlite`) |
| `Serilog`, `Serilog.Sinks.File` | App | Log file |

Shared build settings and the version are in `Directory.Build.props`; output goes to `artifacts/`. The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) (`build/installer.cmd`, `build/installer/coursee.iss`), a tool installed on the PC, not a package.

## Data flow

```
hotkey ─► window shows the cached list at once
             │
             └─► background: SourceLoader.Load(enabled sources)
                   ├─ a failing source is logged and skipped, the others still load
                   ├─ SourceMonitor logs items added / removed per source
                   └─ SourceLoader.Merge: same name + target listed once (first source wins)
typing ─► SearchEngine.Search(resources, query)   (empty query: RecentStore list)
Enter  ─► shell opens Resource.OpenTarget, RecentStore records it
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

Results are sorted by total score, then by name, and capped at 50. Matching ignores case.

## Adding a source
1. In `Core`, implement `IResourceSource`: a stable `Name` (it is also the key stored in `settings.json`), and `GetResources()` returning `Resource` records. Take an optional `ILog`.
2. If it needs Windows, define an interface in `Core` (like `IRunHistoryReader`) and implement it in `App`.
3. Register it in the `sources` array in `App.xaml.cs`. Order matters for duplicates: the first source wins.
4. Add xUnit tests in `tests/coursee.Core.Tests`, with a fake for any interface from step 2.
5. To make it switchable: today this needs a checkbox in the **Sources** section of `MainWindow.xaml` and its line in `MainWindow.xaml.cs` (an open question in [GOAL.md](GOAL.md#open-questions)).
6. Update `README.md` and [ROADMAP.md](ROADMAP.md).

`GetResources()` runs on a background thread, at each launcher opening. It must not block for long, and may throw: `SourceLoader` catches the error, the window logs it and keeps the other sources.

## Logging
- `Core` writes through `ILog` (`log.Info(category, message)`); only `App` knows Serilog.
- Category: the area (`App`, `Action`, `Hotkey`, a source name, ...).
- **Info**: every action the user triggers, start/stop, source changes (via `SourceMonitor`). **Warning/Error**: failures. **Debug**: details.
- Never logged: keystrokes in the search box, secrets.
- Local only: 3 files x 1 MB, plus the last 1000 entries in memory for the log window.

## Versioning
`<Version>` in `Directory.Build.props` is the only place the version is written. It is stamped into every assembly (AssemblyVersion, FileVersion, InformationalVersion), read back through `AppVersion` in `Core`, and passed to the installer by `build/installer.cmd`. Rules: [GOAL.md](GOAL.md#versioning).
