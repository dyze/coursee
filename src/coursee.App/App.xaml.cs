using System.Windows;
using Coursee.Core;
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
    private SettingsWindow? _settingsWindow;
    private LogWindow? _logWindow;
    private TrayIcon? _tray;

    // Set at startup; a closed window cannot be shown again, so each opening creates a new one.
    private Func<SettingsWindow>? _createSettingsWindow;
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
            // Order matters: an item on both the Desktop and the Start Menu is listed once, as the Desktop one.
            ShortcutsSource.Desktop(desktops, shortcutResolver, log),
            ShortcutsSource.StartMenu(startMenus, shortcutResolver, log),
            new FirefoxBookmarksSource(firefoxProfile, log),
            new EdgeFavoritesSource(edgeProfile, log),
            new RunHistorySource(new RegistryRunHistoryReader(), log),
        ];

        var recents = new RecentStore(Path.Combine(dataDirectory, "recents.json"), settings.RecentsCapacity);

        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "coursee.exe");

        var autoStart = new RegistryAutoStart(executablePath);
        var placer = new WindowPlacer(new PlacementStore(Path.Combine(dataDirectory, "windows.json")), log);
        var logHub = _logHub;
        _hotkey = new HotkeyService(() => _window?.Toggle());
        var hotkey = _hotkey;
        _window = new MainWindow(sources, recents, settings, log, new RegistryBrowserLocator(), openSettings: () => OpenSettings());
        var window = _window;
        placer.Attach(window, "Launcher", keepSize: false);

        _createSettingsWindow = () =>
        {
            var created = new SettingsWindow(
                sources,
                settings,
                recents,
                autoStart,
                hotkey,
                logHub,
                onSourceToggled: window.OnSourceToggled,
                openLog: OpenLogWindow);
            placer.Attach(created, "Settings", keepSize: true);
            return created;
        };
        _createLogWindow = () =>
        {
            var created = new LogWindow(logHub.Ring, logHub.Directory);
            placer.Attach(created, "Log", keepSize: true);
            return created;
        };

        // Load the sources now, in the background, so the first hotkey press shows a full list.
        _window.RefreshResources();

        _tray = new TrayIcon(
            AppVersion.Of(typeof(App).Assembly),
            () => hotkey.Current?.ToString() ?? "no hotkey",
            show: () => window.ShowFromOutside(),
            showSettings: () => OpenSettings(),
            showLog: OpenLogWindow,
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
            OpenSettings(SettingsCatalog.GeneralTab);
            _settingsWindow?.ShowHotkeyProblem();
        }
    }

    /// <summary>Opens the Settings window, or brings it to the front, on the given tab if any.</summary>
    private void OpenSettings(string? tab = null)
    {
        if (_settingsWindow is null && _createSettingsWindow is not null)
        {
            _settingsWindow = _createSettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        if (_settingsWindow is null)
        {
            return;
        }

        BringToFront(_settingsWindow);
        if (tab is not null)
        {
            _settingsWindow.ShowTab(tab);
        }
    }

    /// <summary>Opens the log window, or brings it to the front when it is already open.</summary>
    private void OpenLogWindow()
    {
        if (_logWindow is null && _createLogWindow is not null)
        {
            _logWindow = _createLogWindow();
            _logWindow.Closed += (_, _) => _logWindow = null;
        }

        if (_logWindow is not null)
        {
            BringToFront(_logWindow);
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
