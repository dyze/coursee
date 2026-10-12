# coursee: roadmap

Scope of each version. The goal, principles and non-goals are in [GOAL.md](GOAL.md).

| Version | Status | Theme |
| --- | --- | --- |
| 0.1 | Done | Hotkey, search, recents, Desktop, Firefox and Run history sources, settings, logging |
| 0.2 | Done | Tray icon, Start Menu source, row actions, installer |
| 0.3 | Not defined | |

## 0.1
1. **Hotkey and window.** A global hotkey toggles the compact window. Only one copy of coursee runs: starting it again shows the launcher of the running copy.
2. **Source: Desktop shortcuts.** Shortcuts (.lnk) on the Windows Desktop, resolved to their targets (files and folders). The target is what is shown and searched; Enter opens the shortcut itself, so its arguments and working folder are kept.
3. **Search** with filtering as you type.
4. **Recents.** What was opened via coursee, shown when the search box is empty.
5. **Open and hide.** Enter opens the target; Esc hides the window.
6. **Version** shown in the window.
7. **Settings view** (gear button). Section **Windows integration**: auto start on or off, when the user signs in to Windows, with no administrator rights.
8. **Source: Firefox bookmarks.** Default profile only, real bookmarks only (no folders or smart queries), opened in the default browser. Section **Sources** switches it on or off.
9. **Launcher settings.** Section **Launcher**: the global hotkey (default Ctrl+Alt+Space; it must include Ctrl, Alt or Win, since Shift alone would block normal typing) and how many recent items are kept (default 20).
10. **Fallback action**, like the Run dialog (Win+R): when the typed text matches no resource, Enter acts on the text itself. It opens a URL (`http(s)://`, `ftp://` or `www.`), opens an existing folder or file (quotes and `%VARS%` allowed), opens a network path (`\\server\share`, not checked on disk so typing never blocks), or runs the text as a command. The window always shows what Enter will do: a line under the search box ("Enter: Open folder") and a label on every row. Typed actions are added to recents.
11. **Source: Windows Run history.** The commands previously typed in Win+R, most recent first, are searchable, to ease the move away from Win+R. Switchable in **Sources**.
12. **Logging**, for debugging and feedback: start/stop, changes in each source (items added / removed at each refresh), every action the user triggers, and errors. Written to a size-limited local file and shown live in a **log window** (Settings, section **Logging**) with level and text filters, copy and clear. Info by default; a setting turns on detailed (Debug) logging. Never logged: keystrokes while searching, secrets.

## 0.2
1. **Tray icon.** Click: show the launcher. Menu: *Show coursee* (with the current hotkey), *Settings*, *Log window*, *Quit*. The temporary Ctrl+Q shortcut is removed. If no hotkey can be registered, coursee keeps running and is reached from the tray. coursee.exe and the tray use the same icon.
2. **Source: Start Menu.** Shortcuts in the user's and the common Start Menu, subfolders included, handled like the Desktop ones. Uninstall shortcuts are skipped. An item found in two sources with the same name and target is listed once. Switchable in **Sources**.
3. **Row actions** on the selected item:
   - **Ctrl+Enter**: show it in its folder (Explorer opens the containing folder with the item selected). Files, folders and network paths only.
   - **Del**, while the recent list is shown (search box empty): remove the item from recents.

   The line under the search box lists the keys available for the selected item.
4. **Run as administrator.** **Ctrl+Shift+Enter** runs the selected item elevated (Windows asks for confirmation). Commands and executable files only (`.exe`, `.bat`, `.cmd`, `.com`, `.msc`). Cancelling the Windows prompt is not an error.
5. **Installer.** A per-user setup (no administrator rights), self-contained (no .NET runtime needed). It adds a Start Menu shortcut, optionally a desktop shortcut and auto start, and an entry in *Apps & features* to uninstall. Setup and uninstall ask to quit coursee if it is running. Uninstalling keeps the user data.

## 0.3
1. **File reorganisation.** In App & Core, all files are in root of project. Define a suitable structure and move files. Adapt namespaces.
2. **Add Edge browser source.** Retrieve links from Edge browser.
3. **Use right web browser.** If a link comes from a specific browser database, when opening url the source browser is used.
4. **Save & restore Windows positions.** Save position of Main, Log windows... including Monitor index. And restore them when windows are shown again. Ensure positions are valid. 
5. **Hot key conflict.** If hot key is not available at registration, show settings window directly to let user change it. Can we say who is taking the lead of the hot key? if yes, show info to user.
6. **Dark mode.** Allow user to choose between light and dark mode. By default, sync with system setting, but user can override.
7. **Group settings.** App will have more and more settings. Add tabs to group them.
8. **Search for setting.** An edit box can be used to locate a setting quicker.
9. In settings window, Main window fields must not be shown.

## 0.4
Not defined yet.
