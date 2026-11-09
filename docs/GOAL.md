# coursee: goal

## One-liner
A personal Windows launcher: press a global hotkey, a compact window appears with a search box, type to find a resource, press Enter to open it.

## Who it's for
Me first: a personal tool, optimised for my own workflow. From 0.7, it is prepared for other users (public GitHub releases).

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
- **Local first.** coursee reads local files and the registry only. Its one network access is the daily update check (GitHub releases), which can be turned off; it never downloads or installs anything itself.
- **Fast and out of the way.** Appears instantly on hotkey (sources are reloaded in the background, never while the window waits), hides on Esc or after opening.

## Versioning
- Format `MAJOR.MINOR`, starting at **0.1**, then 0.2, and so on. A release with fixes only adds a patch number: `MAJOR.MINOR.PATCH` (0.8.1). No `v` prefix, and no 1.0 until I decide.
- **Under development**, the version has a `-dev` suffix: `0.9-dev` is 0.9 being built, not released, older than `0.9` (the update check offers the release to it). Only a release is ever plain `MAJOR.MINOR[.PATCH]`:

  ```
  0.8.1 published ──► 0.9-dev … ──► 0.9 (release commit) ──► published ──► 0.10-dev …
  ```

  The release commit drops `-dev`, when I ask for a release. The next version with `-dev` is set once the release is published (tagged, its files built from the release commit, the GitHub release made), when I ask for the bump: builds of the release keep the plain version until then.
- Defined once, in `Directory.Build.props`, stamped into every produced assembly, and shown at the bottom right of the launcher window, `-dev` included. Where only numbers fit (the setup's file version), the number without `-dev` is used (`VersionNumber`).
- Each build is identified by its git commit (no build number), shown in Settings > About and the log: `0.8.1 (97bddb2)`, `-dirty` for a build with uncommitted changes.

What each version contains: [ROADMAP.md](ROADMAP.md).

## Non-goals
- Indexing or searching file contents
- Using Windows' own recent-files list
- Multi-user installs (the installer is per user)
- Managing system settings, processes or hardware (possible later, not now). Opening a Windows settings page is fine; changing a setting is not

## Stack and architecture
See [ARCHITECTURE.md](ARCHITECTURE.md).
