using System.Windows;
using System.Windows.Interop;
using Coursee.UI.Platform;

namespace Coursee.UI.Views;

/// <summary>
/// A message (with an option, for a notice) or a yes / no question, in the app's theme, centered on its owner.
/// Without an owner (at startup, before any window), it is centered on the screen, on top, with a taskbar button.
/// </summary>
public partial class MessageDialog : Window
{
    private MessageDialog(Window? owner, string message, bool question)
    {
        InitializeComponent();
        if (owner is null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;
            Topmost = true;
        }
        else
        {
            Owner = owner;
        }

        MessageText.Text = message;
        YesButton.Content = question ? "Yes" : "OK";

        // A message has a single button, which also closes on Esc.
        if (!question)
        {
            NoButton.Visibility = Visibility.Collapsed;
            YesButton.IsCancel = true;
        }

        Loaded += (_, _) => YesButton.Focus();
    }

    public static void Inform(Window? owner, string message) =>
        new MessageDialog(owner, message, question: false).ShowDialog();

    /// <summary>
    /// A warning, with one option to tick or untick (<paramref name="option"/>, starting at
    /// <paramref name="isChecked"/>). Returns the option's state when closed, by OK or Esc.
    /// </summary>
    public static bool Notice(Window owner, string message, string option, bool isChecked)
    {
        var dialog = new MessageDialog(owner, message, question: false);
        dialog.WarningIcon.Visibility = Visibility.Visible;
        dialog.OptionCheckBox.Content = option;
        dialog.OptionCheckBox.IsChecked = isChecked;
        dialog.OptionCheckBox.Visibility = Visibility.Visible;
        dialog.ShowDialog();
        return dialog.OptionCheckBox.IsChecked == true;
    }

    /// <summary>True when the user answered Yes.</summary>
    public static bool Confirm(Window? owner, string question) =>
        new MessageDialog(owner, question, question: true).ShowDialog() == true;

    private void YesButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        NativeWindows.RemoveMinimizeMaximizeButtons(new WindowInteropHelper(this).Handle);
}
