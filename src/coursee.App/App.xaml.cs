using System.Windows;
using Coursee.Core;
using Coursee.Core.Logging;
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
    private TrayIcon? _tray;

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
        _logHub = new LogHub(Path.Combine(dataDirectory, "logs"), settings.DebugLogging);
        var log = _logHub.Filter;
        log.Info("App", $"coursee {AppVersion.Of(typeof(App).Assembly)} started"
            + (settings.DebugLogging ? " (debug logging on)" : string.Empty));

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

        IResourceSource[] sources =
        [
            // Order matters: an item on both the Desktop and the Start Menu is listed once, as the Desktop one.
            ShortcutsSource.Desktop(desktops, shortcutResolver, log),
            ShortcutsSource.StartMenu(startMenus, shortcutResolver, log),
            new FirefoxBookmarksSource(firefoxProfile, log),
            new RunHistorySource(new RegistryRunHistoryReader(), log),
        ];

        var recents = new RecentStore(Path.Combine(dataDirectory, "recents.json"), settings.RecentsCapacity);

        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "coursee.exe");

        _hotkey = new HotkeyService(() => _window?.Toggle());
        _window = new MainWindow(sources, recents, settings, new RegistryAutoStart(executablePath), _hotkey, _logHub);

        // Load the sources now, in the background, so the first hotkey press shows a full list.
        _window.RefreshResources();

        var window = _window;
        _tray = new TrayIcon(
            AppVersion.Of(typeof(App).Assembly),
            () => settings.Hotkey.ToString(),
            show: () => window.ShowFromOutside(),
            showSettings: () => window.ShowSettingsFromOutside(),
            showLog: () => window.OpenLogWindow(),
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
            if (settings.Hotkey != HotkeyGesture.Default && _hotkey.TryRegister(HotkeyGesture.Default))
            {
                log.Warn("Hotkey", $"{settings.Hotkey} is not available, using {HotkeyGesture.Default}");
                MessageBox.Show(
                    $"The hotkey {settings.Hotkey} is not available. Using {HotkeyGesture.Default} instead.",
                    "coursee");
                settings.SetHotkey(HotkeyGesture.Default);
            }
            else
            {
                // The tray icon still gives access to the launcher and to Settings.
                log.Error("Hotkey", $"Could not register {settings.Hotkey} nor {HotkeyGesture.Default}; no hotkey");
                MessageBox.Show(
                    $"Could not register the hotkey {settings.Hotkey} (already in use?).\n\n"
                    + "Use the coursee icon in the notification area, and choose another hotkey in Settings.",
                    "coursee");
            }
        }
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
