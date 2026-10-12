using System.Windows;
using Coursee.Core.Settings;
using Forms = System.Windows.Forms;

namespace Coursee.UI.Views;

/// <summary>
/// Applies the light or dark look with WPF's built-in Fluent theme (<see cref="Application.ThemeMode"/>),
/// which also follows the Windows setting when asked. The API is marked experimental (WPF0001).
/// </summary>
public static class AppTheme
{
#pragma warning disable WPF0001 // ThemeMode is experimental; it is the built-in way to get a dark WPF theme.
    /// <summary>Changes the look of every window, at once.</summary>
    public static void Apply(ThemeChoice choice)
    {
        Application.Current.ThemeMode = choice switch
        {
            ThemeChoice.Light => ThemeMode.Light,
            ThemeChoice.Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
    }
#pragma warning restore WPF0001

#pragma warning disable WFO5001 // SetColorMode is experimental; it is the built-in dark mode of WinForms.
    /// <summary>
    /// The tray menu is WinForms: its colors are chosen once, before it is created, so a change
    /// in Settings reaches it at the next start.
    /// </summary>
    public static void ApplyToTrayMenu(ThemeChoice choice)
    {
        Forms.Application.SetColorMode(choice switch
        {
            ThemeChoice.Light => Forms.SystemColorMode.Classic,
            ThemeChoice.Dark => Forms.SystemColorMode.Dark,
            _ => Forms.SystemColorMode.System,
        });
    }
#pragma warning restore WFO5001
}
