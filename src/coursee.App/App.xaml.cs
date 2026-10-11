using System.Windows;
using Coursee.Core;

namespace Coursee.UI;

public partial class App : Application
{
    private HotkeyService? _hotkey;
    private LogHub? _logHub;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
            new DesktopShortcutsSource(desktops, new WshShortcutResolver()),
            new FirefoxBookmarksSource(firefoxProfile, log),
            new RunHistorySource(new RegistryRunHistoryReader(), log),
        ];

        var recents = new RecentStore(Path.Combine(dataDirectory, "recents.json"), settings.RecentsCapacity);

        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "coursee.exe");

        MainWindow? window = null;
        _hotkey = new HotkeyService(() => window?.Toggle());
        window = new MainWindow(sources, recents, settings, new RegistryAutoStart(executablePath), _hotkey, _logHub);

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
                log.Error("Hotkey", $"Could not register {HotkeyGesture.Default}; exiting");
                MessageBox.Show(
                    $"Could not register the hotkey {HotkeyGesture.Default} (already in use?).",
                    "coursee");
                Shutdown(1);
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();
        _logHub?.Filter.Info("App", "coursee stopped");
        _logHub?.Dispose();
        base.OnExit(e);
    }
}
