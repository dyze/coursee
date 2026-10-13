using System.Windows;
using System.Windows.Interop;
using Coursee.Core;
using Coursee.Core.Entries;
using Coursee.Core.Logging;
using Coursee.Core.Placement;
using Coursee.Core.Platform;
using Coursee.Core.Recents;
using Coursee.Core.Settings;
using Coursee.Core.Sources;
using Coursee.UI.Logging;
using Coursee.UI.Platform;
using Coursee.UI.Tray;
using Coursee.UI.Views;

namespace Coursee.UI;

public partial class App : Application
{
    private HotkeyService? _hotkey;
    private LogHub? _logHub;
    private SingleInstance? _singleInstance;
    private MainWindow? _window;
    private LogWindow? _logWindow;
    private TrayIcon? _tray;

    // Set at startup; a closed window cannot be shown again, so each opening creates a new one.
    private Func<LogWindow>? _createLogWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Another copy is running: it has been asked to show its launcher, so this one leaves.
        _singleInstance = SingleInstance.TryClaim(() => Dispatcher.BeginInvoke(() => _window?.ShowFromOutside()));
        if (_singleInstance is null)
        {
            Shutdown(0);
            return;
        }

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "coursee");
        var settings = new SettingsStore(Path.Combine(dataDirectory, "settings.json"));

        // Before any window or the tray menu is created.
        AppTheme.ApplyToTrayMenu(settings.Theme);
        AppTheme.Apply(settings.Theme);
        _logHub = new LogHub(Path.Combine(dataDirectory, "logs"), settings.DebugLogging);
        var log = _logHub.Filter;
        log.Info("App", $"coursee {AppVersion.Of(typeof(App).Assembly)} started"
            + (settings.DebugLogging ? " (debug logging on)" : string.Empty));
        log.Debug("App", $"Theme: {settings.Theme}");

        DispatcherUnhandledException += (_, args) =>
            log.Error("App", "Unhandled error", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            log.Error("App", "Fatal error: " + args.ExceptionObject);

        var desktops = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
        };
        var startMenus = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
        };
        var shortcutResolver = new WshShortcutResolver();
        var customEntries = new CustomEntryStore(Path.Combine(dataDirectory, "custom.json"));

        var firefoxRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mozilla",
            "Firefox");
        var firefoxProfile = FirefoxProfileLocator.FindDefaultProfile(firefoxRoot);
        if (firefoxProfile is null)
        {
            log.Info("Firefox", "No Firefox profile found");
        }
        else
        {
            log.Debug("Firefox", $"Profile: {firefoxProfile}");
        }

        var edgeUserData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Edge",
            "User Data");
        var edgeProfile = ChromiumBookmarks.FindProfile(edgeUserData);
        if (edgeProfile is null)
        {
            log.Info("Edge", "No Edge profile found");
        }
        else
        {
            log.Debug("Edge", $"Profile: {edgeProfile}");
        }

        IResourceSource[] sources =
        [
            // The user's own entries first: one with the same name and target as a shortcut is listed as theirs.
            new CustomSource(customEntries),
            // Order matters: an item on both the Desktop and the Start Menu is listed once, as the Desktop one.
            ShortcutsSource.Desktop(desktops, shortcutResolver, log),
            ShortcutsSource.StartMenu(startMenus, shortcutResolver, log),
            new FirefoxBookmarksSource(firefoxProfile, log),
            new EdgeFavoritesSource(edgeProfile, log),
            new RunHistorySource(new RegistryRunHistoryReader(), log),
            new WindowsSettingsSource(),
        ];

        var recents = new RecentStore(Path.Combine(dataDirectory, "recents.json"), settings.RecentsCapacity);

        var entries = new EntryStore(Path.Combine(dataDirectory, "entries.json"));
        var customEntryEditor = new CustomEntryEditor(customEntries, entries, recents);

        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "coursee.exe");

        var autoStart = new RegistryAutoStart(executablePath);
        UpgradeAutoStart(autoStart, log);
        var placer = new WindowPlacer(new PlacementStore(Path.Combine(dataDirectory, "windows.json")), log);
        var logHub = _logHub;
        _hotkey = new HotkeyService(() => _window?.Toggle());
        var hotkey = _hotkey;
        _window = new MainWindow(
            sources,
            recents,
            entries,
            customEntryEditor,
            settings,
            log,
            new RegistryBrowserLocator(),
            createSettings: () =>
            {
                var created = new SettingsWindow(
                    sources,
                    settings,
                    recents,
                    entries,
                    customEntries,
                    customEntryEditor,
                    autoStart,
                    hotkey,
                    logHub,
                    onSourceToggled: OnSourceToggled,
                    openLog: OpenLogWindow);
                placer.Attach(created, "Settings", RestoreMode.SizeOverOwner);
                return created;
            });
        var window = _window;
        placer.Attach(window, "Launcher", RestoreMode.Position);

        _createLogWindow = () =>
        {
            var created = new LogWindow(logHub.Ring, logHub.Directory);
            placer.Attach(created, "Log", RestoreMode.PositionAndSize);
            return created;
        };

        // Load the sources now, in the background, so the first hotkey press shows a full list.
        _window.RefreshResources();

        _tray = new TrayIcon(
            AppVersion.Of(typeof(App).Assembly),
            () => hotkey.Current?.ToString() ?? "no hotkey",
            show: () => window.ShowFromOutside(),
            // After the menu has closed: Settings is a dialog, it waits until closed.
            showSettings: () => Dispatcher.BeginInvoke(() => window.ShowSettings()),
            showLog: () => OpenLogWindow(),
            quit: () =>
            {
                log.Info("Action", "Quit from the tray");
                Shutdown();
            });

        if (_hotkey.TryRegister(settings.Hotkey))
        {
            log.Info("Hotkey", $"Registered {settings.Hotkey}");
        }
        else
        {
            // Windows does not tell which application holds a hotkey. The tray icon still gives
            // access to the launcher; Settings opens so that another hotkey can be chosen.
            log.Warn("Hotkey", $"Could not register {settings.Hotkey} (used by another application or by Windows); no hotkey");
            Dispatcher.BeginInvoke(() => window.ShowSettings(SettingsCatalog.GeneralTab, hotkeyProblem: true));
            return;
        }

        // Started by the user or by the installer: show the launcher. At sign-in, stay in the tray.
        if (AutoStartCommand.StartsHidden(e.Args))
        {
            log.Debug("App", "Started hidden");
        }
        else
        {
            Dispatcher.BeginInvoke(() => window.ShowFromOutside());
        }
    }

    private void OnSourceToggled(string sourceName) => _window?.OnSourceToggled(sourceName);

    /// <summary>
    /// An auto start entry written by an older version starts this copy without <see cref="AutoStartCommand.HiddenArgument"/>,
    /// so the launcher would pop up at each sign-in: rewrite it.
    /// </summary>
    private static void UpgradeAutoStart(IAutoStart autoStart, ILog log)
    {
        try
        {
            if (autoStart.Status == AutoStartStatus.ThisCopyOutdated)
            {
                autoStart.SetEnabled(true);
                log.Info("App", "Auto start entry updated to start hidden");
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            log.Warn("App", $"Cannot update the auto start entry: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>Opens the log window, or brings it to the front when it is already open.</summary>
    /// <param name="over">
    /// A window that stays open, on top, and modal (Settings over the launcher): the log window is
    /// shown in front of it and usable until it closes, then it stays open on its own.
    /// </param>
    private void OpenLogWindow(Window? over = null)
    {
        if (_logWindow is null && _createLogWindow is not null)
        {
            _logWindow = _createLogWindow();
            _logWindow.Closed += (_, _) => _logWindow = null;
        }

        if (_logWindow is not { } logWindow)
        {
            return;
        }

        if (over is not null)
        {
            // Settings is on top (its owner, the launcher, is), so the log window must be too while it is open.
            logWindow.Topmost = true;
            over.Closed += (_, _) => logWindow.Topmost = false;
        }

        BringToFront(logWindow);

        // A log window opened before Settings was disabled by it, as a modal dialog does with the others.
        if (over is not null)
        {
            NativeWindows.Enable(new WindowInteropHelper(logWindow).Handle);
        }
    }

    private static void BringToFront(Window window)
    {
        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _hotkey?.Dispose();
        _logHub?.Filter.Info("App", "coursee stopped");
        _logHub?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
