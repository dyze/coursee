using System.Windows;
using System.Windows.Interop;
using Coursee.UI.Platform;

namespace Coursee.UI.Views;

/// <summary>A message or a yes / no question, in the app's theme, centered on its owner.</summary>
public partial class MessageDialog : Window
{
    private MessageDialog(Window owner, string message, bool question)
    {
        InitializeComponent();
        Owner = owner;
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

    public static void Inform(Window owner, string message) =>
        new MessageDialog(owner, message, question: false).ShowDialog();

    /// <summary>True when the user answered Yes.</summary>
    public static bool Confirm(Window owner, string question) =>
        new MessageDialog(owner, question, question: true).ShowDialog() == true;

    private void YesButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        NativeWindows.RemoveMinimizeMaximizeButtons(new WindowInteropHelper(this).Handle);
}
