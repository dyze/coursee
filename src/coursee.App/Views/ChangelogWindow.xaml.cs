using System.Windows;

namespace Coursee.UI.Views;

/// <summary>The changelog, opened from Settings > About: a dialog over Settings, which can be resized.</summary>
public partial class ChangelogWindow : Window
{
    private ChangelogWindow(Window owner, string text)
    {
        InitializeComponent();
        Owner = owner;
        ChangelogBox.Text = text;
        Loaded += (_, _) => ChangelogBox.Focus();
    }

    public static void Show(Window owner, string text) => new ChangelogWindow(owner, text).ShowDialog();
}
