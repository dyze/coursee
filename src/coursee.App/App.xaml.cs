using System.Windows;
using Coursee.Core;

namespace Coursee.UI;

public partial class App : Application
{
    private HotkeyService? _hotkey;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        IResourceSource[] sources =
        [
            new DesktopShortcutsSource(desktops, new WshShortcutResolver()),
            new FirefoxBookmarksSource(firefoxProfile),
            new RunHistorySource(new RegistryRunHistoryReader()),
        ];

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "coursee");
        var settings = new SettingsStore(Path.Combine(dataDirectory, "settings.json"));
        var recents = new RecentStore(Path.Combine(dataDirectory, "recents.json"), settings.RecentsCapacity);

        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "coursee.exe");

        MainWindow? window = null;
        _hotkey = new HotkeyService(() => window?.Toggle());
        window = new MainWindow(sources, recents, settings, new RegistryAutoStart(executablePath), _hotkey);

        if (!_hotkey.TryRegister(settings.Hotkey))
        {
            if (settings.Hotkey != HotkeyGesture.Default && _hotkey.TryRegister(HotkeyGesture.Default))
            {
                MessageBox.Show(
                    $"The hotkey {settings.Hotkey} is not available. Using {HotkeyGesture.Default} instead.",
                    "coursee");
                settings.SetHotkey(HotkeyGesture.Default);
            }
            else
            {
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
        base.OnExit(e);
    }
}
