# CLAUDE.md

Rules for working on **coursee** together. Read `docs/GOAL.md` first: it defines the goal, scope and non-goals.

## Project in one line
A personal Windows launcher: global hotkey, compact search window, opens files, folders and bookmarks.

## Architecture (keep it this way)

```
coursee.App   WPF, hotkey, window, Windows-only code
    │
coursee.Core  search, recents, IResourceSource (no Windows dependency)
    │
sources       DesktopShortcutsSource, then FirefoxBookmarksSource, ...
```

- New resource sources implement `IResourceSource`. Adding one must not change the UI.
- Search matches targets only (name, path, URL). No file-content indexing.
- Recents are what was opened through coursee, not Windows' recent list.
- Anything Windows-specific goes in `coursee.App`, behind an interface defined in `Core`.

## Stack
.NET 10, C#, WPF, xUnit. The only third-party package is `Microsoft.Data.Sqlite` (in `Core`, to read Firefox bookmarks). Shared settings live in `Directory.Build.props`; build output goes to `artifacts/`.

## Versioning
- Format `MAJOR.MINOR`, starting at **0.1**, then 0.2, and so on. No `v` prefix, and no 1.0 until the owner decides.
- One source of truth: `<Version>` in `Directory.Build.props`. Never write the version anywhere else.
- It is stamped into every assembly of every project (AssemblyVersion, FileVersion, InformationalVersion), including `coursee.exe`.
- The UI shows it at the bottom right of the launcher window, read through `AppVersion` in `Core`.
- Bump it only when the owner asks.

## Repo layout
`src/`, `tests/`, `docs/`, `build/`, `lib/`, `packages/`, `samples/`, `artifacts/` (ignored). Do not add top-level folders without asking.

## How we work
- **Concise answers.** Short explanations, diagrams over long text.
- **Do it well.** Prefer the clean, simple solution over the quick hack.
- **Ask before** adding a dependency, changing the architecture, doing a large refactor, or touching anything listed as a non-goal.
- **Tests.** Every change in `Core` comes with xUnit tests.
- **Build honesty.** Claude's workspace has no .NET SDK, so it cannot build or run tests. Never claim the code builds; ask the owner to run `build.cmd` and report errors.
- **Docs.** When behavior changes, update `README.md` and `docs/`.
- **Don't touch** `LICENSE`.

## Git
- Commit and push only when asked.
- Create branches only when asked. Otherwise work on `main`.
- Small commits, imperative subject line ("Add Firefox bookmarks source").

## Definition of done
Code is written, tests cover the new `Core` behavior, docs are updated, and the owner has run `build.cmd` successfully.
