using System.Windows;
using Forms = System.Windows.Forms;

namespace Coursee.UI;

/// <summary>
/// The icon in the Windows notification area. A click shows the launcher; the menu offers
/// Show, Settings, Log window and Quit. Uses WinForms' NotifyIcon, since WPF has none.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _showItem;
    private readonly Func<string> _hotkeyText;

    /// <param name="version">Shown in the tooltip.</param>
    /// <param name="hotkeyText">The current hotkey, read each time the menu opens (it can change in Settings).</param>
    public TrayIcon(
        string version,
        Func<string> hotkeyText,
        Action show,
        Action showSettings,
        Action showLog,
        Action quit)
    {
        _hotkeyText = hotkeyText;

        _showItem = new Forms.ToolStripMenuItem("Show coursee", null, (_, _) => show());
        _showItem.Font = new System.Drawing.Font(_showItem.Font, System.Drawing.FontStyle.Bold);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_showItem);
        menu.Items.Add(new Forms.ToolStripMenuItem("Settings", null, (_, _) => showSettings()));
        menu.Items.Add(new Forms.ToolStripMenuItem("Log window", null, (_, _) => showLog()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Quit", null, (_, _) => quit()));
        menu.Opening += (_, _) => _showItem.ShortcutKeyDisplayString = _hotkeyText();

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = $"coursee {version}",
            ContextMenuStrip = menu,
            Visible = true,
        };

        // A left click shows the launcher; the right click opens the menu by itself.
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                show();
            }
        };
    }

    public void Dispose()
    {
        // Hide first, or the icon lingers in the notification area until the mouse passes over it.
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Icon?.Dispose();
        _icon.Dispose();
    }

    /// <summary>The application icon embedded in the assembly (see coursee.App.csproj), at the tray size.</summary>
    private static System.Drawing.Icon LoadIcon()
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/coursee.ico"))
            ?? throw new InvalidOperationException("coursee.ico is missing from the assembly resources.");
        using var stream = resource.Stream;
        return new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
    }
}
