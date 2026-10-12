# coursee: roadmap

Scope of each version. The goal, principles and non-goals are in [GOAL.md](GOAL.md).

| Version | Status | Theme |
| --- | --- | --- |
| 0.1 | Done | Hotkey, search, recents, Desktop, Firefox and Run history sources, settings, logging |
| 0.2 | Done | Tray icon, Start Menu source, row actions, installer |
| 0.3 | Done | Code layout, Edge source, right browser, window positions, theme, settings window |
| 0.4 | In progress | Launcher position, list keys, Windows settings source, icons, keyboard-only Settings |

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
1. **Code layout.** `Core` and `App` files are grouped in folders, one namespace per folder (see [ARCHITECTURE.md](ARCHITECTURE.md#projects)).
2. **Source: Edge favorites.** Bookmarks of the last used Edge profile, real links only (no folders). Switchable in **Sources**.
3. **Open in the right browser.** A Firefox bookmark opens in Firefox, an Edge favorite in Edge, also from recents. If that browser is not found, the default browser is used. The row label says which one ("Open in Firefox").
4. **Window positions.** The launcher and the log window reopen where they were left, on the same monitor; the log window also keeps its size. Settings, a dialog, opens centered on the launcher, at its last size. The launcher is moved by dragging its empty area. A window that is partly off screen (monitor unplugged, resolution or scaling changed, launcher dragged past an edge) is moved back inside a visible screen, each time it is shown.
5. **Hotkey conflict.** When the hotkey cannot be registered at start, Settings opens on it and explains that another application (or Windows) uses it. There is no silent switch to the default hotkey any more. Windows does not tell which application holds a hotkey, so coursee cannot name it.
6. **Theme.** Light, dark, or follow Windows (default), in Settings > Appearance.
7. **Settings window.** Settings is its own window instead of a panel of the launcher: a dialog over the launcher (closing it goes back to the launcher), which can be moved and resized. Settings are grouped in tabs: General, Appearance, Sources, Logging. The source checkboxes are built from the list of sources.
8. **Find a setting.** A search box at the top of Settings: while it has text, only the matching settings are shown, from every tab.
9. **Start hidden.** `coursee.exe --hidden` starts in the notification area only; the auto start entry uses it. Started without it (by the user or the installer), coursee shows the launcher.

## 0.4
1. **Launcher position.** Settings > General chooses where the launcher appears each time it is shown:
   - **Centered on the mouse's monitor** (default): in the middle of the monitor under the mouse.
   - **At the mouse**: centered on the mouse pointer.
   - **Where I left it**: its last position (as in 0.3).

   It is always kept fully on screen. The launcher can still be dragged; only *Where I left it* remembers where.
2. **List keys.** Page Up / Page Down move the selection by one page of the list, Ctrl+Home / Ctrl+End to the first / last item. Home and End stay with the search box (start / end of the text).
3. **Source: Windows settings.** A fixed list of Windows settings pages and tools, opened directly: Installed apps, Network, Display, Sound, Windows Update, Services, Device Manager, Event Viewer, Advanced system settings, Environment variables, Control Panel, and more. coursee only opens them, it never changes a setting. Names are written to be found (for example "Installed apps (uninstall programs)"); search still matches name and target only. Consoles and programs (`.msc`, `.exe`) can be run as administrator. Switchable in **Sources**.
4. **Esc clears first.** Esc with text in the search box clears it (the recent list is shown again); Esc with an empty box hides the launcher.
5. **Action always visible.** In the list, a long name or target is cut with "…"; the action column on the right is always shown.
6. **Action icons.** Each row shows its action (open file, open folder, open URL, open in a browser, run command, open setting) as an icon, with the wording as tooltip. The line under the search box is unchanged.
7. **Source icons.** Each row shows a small icon of the source it comes from (typed text has its own). The **Sources** tab shows the same icon next to each source.
8. **Keyboard only.** In the launcher, Tab moves the focus to the Settings button, Enter or Space opens Settings, Tab again goes back to the search box. In Settings: a logical Tab order, Ctrl+Tab / Ctrl+Shift+Tab switch tabs (arrows too, on the tab strip), Down in the Find box goes to the first setting shown, and the focused control is always visible.
