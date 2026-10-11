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
- **Targets only.** Search matches the shortcut targets themselves (files and folders), never the contents of the folders they point to. No file-system indexing.
- **Recents come from coursee only.** "Recent" means what was opened through coursee, not Windows' own recent list.
- **Fast and out of the way.** Appears instantly on hotkey, hides on Esc or after opening.

## Versioning
- Format `MAJOR.MINOR`, starting at **0.1**, then 0.2, and so on. No `v` prefix, and no 1.0 until I decide.
- The version is defined once (`Directory.Build.props`) and stamped into every produced assembly.
- The version is shown in the UI, at the bottom right of the launcher window.

## Version 0.1 scope
1. Global hotkey toggles the compact window.
2. Source #1: shortcuts (.lnk) on the Windows Desktop, resolved to their targets (files and folders).
3. Search box with filtering as you type.
4. Recent list (opened via coursee), shown when the search box is empty.
5. Enter opens the target; Esc hides the window.
6. The version number is shown in the window.
7. A settings button (gear) in the window opens a settings view. Its first section, **Windows integration**, lets the user turn auto start on or off: when on, coursee starts when the user signs in to Windows. Auto start uses the per-user Run registry key (no administrator rights needed).
8. Source #2: Firefox bookmarks. Default profile only, real bookmarks only (no folders or smart queries), opened in the default browser. A second settings section, **Sources**, lets the user switch the Firefox source on or off.
9. A third settings section, **Launcher**, lets the user change the global hotkey (default Ctrl+Alt+Space) and how many recent items are kept (default 20).
10. Fallback action, like the Windows Run dialog (Win+R): when the typed text matches no resource, Enter acts on the text itself. It opens a URL (`http(s)://`, `ftp://` or `www.`), opens an existing folder or file (quotes and `%VARS%` allowed), or runs the text as a command. The window always shows which kind of action Enter will do: a line under the search box ("Enter: Open folder") and a label on every row. Typed actions are added to recents.
11. Source #3: Windows Run history. The commands previously typed in the Win+R dialog (read from the per-user `RunMRU` registry key, most recent first) are searchable in coursee, to ease the migration away from Win+R. They can be switched off in Settings, section **Sources**.
12. Logging, for debugging and user feedback. coursee logs its activity: application start/stop, **changes in each source** (items added / removed at each refresh), **every action the user triggers** (resource opened, typed command, settings changed) and errors. Entries go to a rolling text file (`%LOCALAPPDATA%\coursee\logs`, at most 3 files of 1 MB) and to a **log window** opened from Settings (section **Logging**), which shows the last 1000 entries live with a level filter, a text filter, copy and clear. Level is Info by default; a Settings checkbox turns on detailed (Debug) logging. Never logged: keystrokes while searching, secrets. Library: Serilog (file sink) in the App.

## Planned sources (after 0.1)
- Other sources later, using the same plug-in contract

## Non-goals
- Indexing or searching file contents
- Using Windows' own recent-files list
- Multi-user, installer, or public release
- Managing system settings, processes or hardware (possible later, not now)

## Open questions
- Hotkey choice and whether it should be configurable
- Recent list: size limit, and ranking by recency only or recency plus frequency

## Stack (decided)
.NET 10, C#, WPF for the UI, xUnit for tests, Serilog (file sink) for the log file. See `CLAUDE.md` for the architecture rules.
