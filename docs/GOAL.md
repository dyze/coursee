# coursee: goal

## One-liner
A personal Windows launcher: press a global hotkey, a compact window appears with a search box, type to find a resource, press Enter to open it.

## Who it's for
Just me. Personal tool, optimised for my own workflow, not for distribution.

## How it works

```
Hotkey ─► compact window
          ┌──────────────────────────┐
          │ search…                  │
          ├──────────────────────────┤
          │ recent 1                 │
          │ recent 2                 │
          │ matches from sources     │
          └──────────────────────────┘
Enter ─► open resource    Esc ─► hide
```

## Principles
- **Pluggable sources.** The app is source-agnostic. Each source supplies resources (name, target, kind). Adding a source must not touch the UI.
- **Targets only.** Search matches each resource's name and target (path, URL or command), never the contents of the folders they point to. No file-system indexing.
- **Recents come from coursee only.** "Recent" means what was opened through coursee, not Windows' own recent list.
- **Fast and out of the way.** Appears instantly on hotkey (sources are reloaded in the background, never while the window waits), hides on Esc or after opening.

## Versioning
- Format `MAJOR.MINOR`, starting at **0.1**, then 0.2, and so on. No `v` prefix, and no 1.0 until I decide.
- Defined once, in `Directory.Build.props`, stamped into every produced assembly, and shown at the bottom right of the launcher window.

What each version contains: [ROADMAP.md](ROADMAP.md).

## Non-goals
- Indexing or searching file contents
- Using Windows' own recent-files list
- Multi-user or public release (a personal, per-user installer is fine)
- Managing system settings, processes or hardware (possible later, not now). Opening a Windows settings page is fine; changing a setting is not

## Open questions
- Recent list ranking: recency only (today), or recency plus frequency

## Stack and architecture
See [ARCHITECTURE.md](ARCHITECTURE.md).
