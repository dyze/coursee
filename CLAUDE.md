# CLAUDE.md

Rules for working on **coursee** together. Read first:
- `docs/GOAL.md`: goal, principles, non-goals
- `docs/ROADMAP.md`: scope of each version
- `docs/ARCHITECTURE.md`: projects, stack, search, adding a source, logging

## Project in one line
A personal Windows launcher: global hotkey, compact search window, opens files, folders and bookmarks.

## Architecture rules (keep it this way)
- `coursee.App` (WPF) holds the UI and all Windows-specific code. `coursee.Core` has no Windows dependency.
- Windows access goes behind an interface defined in `Core` and implemented in `App`.
- New resource sources implement `IResourceSource`, in `Core`. Adding one must not change the UI.
- Search matches targets only (name, path, URL). No file-content indexing.
- Recents are what was opened through coursee, not Windows' recent list.

## Dependencies
Only those listed in `docs/ARCHITECTURE.md#stack`. Ask before adding one.

## Logging rules
- `Core` writes through `ILog`; only `App` knows Serilog. Source classes take an optional `ILog`.
- Every user-triggered action at **Info**, source changes via `SourceMonitor`, failures at Warning/Error, details at **Debug**.
- Never log keystrokes typed in the search box, or secrets.
- Logs stay local: 3 files x 1 MB, plus 1000 entries in memory.

## Versioning rules
- Format and policy: `docs/GOAL.md#versioning`.
- `<Version>` in `Directory.Build.props` is the only place the version is written: not in code, docs or scripts.
- Bump it only when the owner asks.

## Repo layout
`src/`, `tests/`, `docs/`, `build/`, `lib/`, `packages/`, `samples/`, `artifacts/` (ignored). Do not add top-level folders without asking.

## How we work
- **Concise answers.** Short explanations, diagrams over long text.
- **Do it well.** Prefer the clean, simple solution over the quick hack.
- **Ask before** adding a dependency, changing the architecture, doing a large refactor, or touching anything listed as a non-goal.
- **Tests.** Every change in `Core` comes with xUnit tests.
- **Build honesty.** Claude's workspace has no .NET SDK, so it cannot build or run tests. Never claim the code builds; ask the owner to run `build.cmd` and report errors.
- **Docs.** One home per fact:
  - user-facing behavior → `README.md`
  - version scope → `docs/ROADMAP.md`
  - design and internals → `docs/ARCHITECTURE.md`
  - goal and principles → `docs/GOAL.md`

  Link between them rather than copying. Update them when behavior changes.
- **Don't touch** `LICENSE`.

## Git
- Commit and push only when asked.
- Create branches only when asked. Otherwise work on `main`.
- Small commits, imperative subject line ("Add Firefox bookmarks source").

## Definition of done
Code is written, tests cover the new `Core` behavior, docs are updated, and the owner has run `build.cmd` successfully.
