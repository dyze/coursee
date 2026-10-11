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
- The version is defined once (`Directory.Build.props`) and stamped into every produced assembly.
- The version is shown in the UI, at the bottom right of the launcher window.

## Version 0.1 scope
1. Global hotkey toggles the compact window. Only one copy of coursee runs: starting it again shows the launcher of the running copy.
2. Source #1: shortcuts (.lnk) on the Windows Desktop, resolved to their targets (files and folders). The target is what is shown and searched; Enter opens the shortcut itself, so its arguments and working folder are kept.
3. Search box with filtering as you type.
4. Recent list (opened via coursee), shown when the search box is empty.
5. Enter opens the target; Esc hides the window.
6. The version number is shown in the window.
7. A settings button (gear) in the window opens a settings view. Its first section, **Windows integration**, lets the user turn auto start on or off: when on, coursee starts when the user signs in to Windows. Auto start uses the per-user Run registry key (no administrator rights needed).
8. Source #2: Firefox bookmarks. Default profile only, real bookmarks only (no folders or smart queries), opened in the default browser. A second settings section, **Sources**, lets the user switch the Firefox source on or off.
9. A third settings section, **Launcher**, lets the user change the global hotkey (default Ctrl+Alt+Space; it must include Ctrl, Alt or Win, since Shift alone would block normal typing) and how many recent items are kept (default 20).
10. Fallback action, like the Windows Run dialog (Win+R): when the typed text matches no resource, Enter acts on the text itself. It opens a URL (`http(s)://`, `ftp://` or `www.`), opens an existing folder or file (quotes and `%VARS%` allowed), opens a network path (`\\server\share`, not checked on disk so typing never blocks), or runs the text as a command. The window always shows which kind of action Enter will do: a line under the search box ("Enter: Open folder") and a label on every row. Typed actions are added to recents.
11. Source #3: Windows Run history. The commands previously typed in the Win+R dialog (read from the per-user `RunMRU` registry key, most recent first) are searchable in coursee, to ease the migration away from Win+R. They can be switched off in Settings, section **Sources**.
12. Logging, for debugging and user feedback. coursee logs its activity: application start/stop, **changes in each source** (items added / removed at each refresh), **every action the user triggers** (resource opened, typed command, settings changed) and errors. Entries go to a rolling text file (`%LOCALAPPDATA%\coursee\logs`, at most 3 files of 1 MB) and to a **log window** opened from Settings (section **Logging**) or the tray menu, which shows the last 1000 entries live with a level filter, a text filter, copy and clear. Level is Info by default; a Settings checkbox turns on detailed (Debug) logging. Never logged: keystrokes while searching, secrets. Library: Serilog (file sink) in the App.

## Version 0.2 scope
1. **Tray icon.** coursee shows an icon in the notification area. Click: show the launcher. Menu: *Show coursee* (with the current hotkey), *Settings*, *Log window*, *Quit*. The temporary Ctrl+Q shortcut is removed. If no hotkey can be registered, coursee keeps running and is reached from the tray. coursee.exe and the tray use the same application icon.
2. **Source #4: Start Menu.** Shortcuts (.lnk) in the user's and the common Start Menu, subfolders included, handled like the Desktop ones (target shown and searched, the shortcut itself opened). Uninstall shortcuts are skipped. An item found in two sources with the same name and target is listed once. It can be switched off in Settings, section **Sources**.
3. **Row actions.** On the selected item:
   - **Ctrl+Enter**: show it in its folder (Explorer opens the containing folder with the item selected). Files, folders and network paths only.
   - **Del**, while the recent list is shown (search box empty): remove the item from recents.

   The line under the search box lists the keys available for the selected item.
4. **Run as administrator.** **Ctrl+Shift+Enter** runs the selected item elevated (Windows asks for confirmation). Commands and executable files only (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`). Cancelling the Windows prompt is not an error.
5. **Installer.** `build\installer.cmd` builds `coursee-setup-<version>.exe` with Inno Setup. Per-user (no administrator rights), installed in `%LOCALAPPDATA%\Programs\coursee`, self-contained (no .NET runtime needed on the PC). It adds a Start Menu shortcut, optionally a desktop shortcut and auto start, and an entry in *Apps & features* to uninstall. Setup and uninstall ask to quit coursee if it is running. Uninstalling keeps the user data (`%LOCALAPPDATA%\coursee`).

## Planned sources
- Other sources later, using the same plug-in contract

## Non-goals
- Indexing or searching file contents
- Using Windows' own recent-files list
- Multi-user or public release (a personal, per-user installer is fine: see 0.2 scope)
- Managing system settings, processes or hardware (possible later, not now)

## Open questions
- Recent list ranking: recency only (today), or recency plus frequency

## Stack (decided)
.NET 10, C#, WPF for the UI (WinForms only for the tray icon), xUnit for tests, Microsoft.Data.Sqlite for Firefox bookmarks, Serilog (file sink) for the log file, Inno Setup for the installer. See `CLAUDE.md` for the architecture rules.
