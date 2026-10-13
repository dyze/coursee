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
`src/`, `tests/`, `docs/`, `build/`, `lib/`, `packages/`, `samples/`, `.config/` (dotnet tools), `artifacts/` (ignored). Do not add top-level folders without asking.

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
  Prefer diagrams and tables to long descriptions.
  Link between them rather than copying. Update them when behavior changes.
- **Don't touch** `LICENSE`.

## Design rules

* Be concise.
* Follow Clean Architecture principles 
* Avoid redundancies in explanations, try to update existing docs instead of creating new ones.
* Ask clarifying questions before making architectural changes.
* Mark in documents what is implemented and what is not.

## Coding rules

* Apply Clean Code principles. e.g. prioritize meaningful variable names rather than long comments.
* Writing Code for Today's Problems, Not Tomorrow's Fantasies.
* Remove dead code.
* Avoid long modules. Move large, monolithic classes or methods into separate `.cs` files or helper classes to keep individual files concise and maintainable.
* Avoid redundant code.
* Validate with the owner's `build.cmd` run (see Build honesty).
* Maintain Core library sub-systems code coverage target of **>=95%** (line coverage, enforced by `build.cmd`).
* Reduce build warnings.

## Git
- Commit and push only when asked.
- Create branches only when asked. Otherwise work on `main`.
- Small commits, [Conventional Commits](https://www.conventionalcommits.org/en) with an imperative subject (`feat: add Firefox bookmarks source`).

## Definition of done
Code is written, tests cover the new `Core` behavior, docs are updated, and the owner has run `build.cmd` successfully.
