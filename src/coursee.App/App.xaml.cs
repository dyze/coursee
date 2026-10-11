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

        IResourceSource[] sources = [new DesktopShortcutsSource(desktops, new WshShortcutResolver())];

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "coursee");
        var recents = new RecentStore(Path.Combine(dataDirectory, "recents.json"));

        var executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "coursee.exe");
        var window = new MainWindow(sources, recents, new RegistryAutoStart(executablePath));

        try
        {
            _hotkey = new HotkeyService(window.Toggle);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "coursee");
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();
        base.OnExit(e);
    }
}
