using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Coursee.Core;

namespace Coursee.UI;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<IResourceSource> _sources;
    private readonly RecentStore _recents;
    private readonly IAutoStart _autoStart;
    private IReadOnlyList<Resource> _all = [];

    public MainWindow(IReadOnlyList<IResourceSource> sources, RecentStore recents, IAutoStart autoStart)
    {
        _sources = sources;
        _recents = recents;
        _autoStart = autoStart;
        InitializeComponent();
        VersionText.Text = $"coursee {AppVersion.Of(typeof(MainWindow).Assembly)}";
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            ShowLauncher();
        }
    }

    private void ShowLauncher()
    {
        _all = _sources.SelectMany(s => s.GetResources()).ToList();
        SearchBox.Text = string.Empty;
        Refresh();
        ShowSearch();
        Show();
        Activate();
        SearchBox.Focus();
    }

    private bool IsSettingsOpen => SettingsPanel.Visibility == Visibility.Visible;

    private void ShowSearch()
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
        ResultsList.Visibility = Visibility.Visible;
        SearchBox.IsEnabled = true;
        SearchBox.Focus();
    }

    private void ShowSettings()
    {
        AutoStartCheckBox.IsChecked = ReadAutoStart();
        ResultsList.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Visible;
        SearchBox.IsEnabled = false;
        Focus();
    }

    private bool ReadAutoStart()
    {
        try
        {
            return _autoStart.IsEnabled;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return false;
        }
    }

    private void Refresh()
    {
        var query = SearchBox.Text;
        IReadOnlyList<Resource> items = string.IsNullOrWhiteSpace(query)
            ? _recents.Items
            : SearchEngine.Search(_all, query);

        ResultsList.ItemsSource = items;
        if (items.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
        }
    }

    private void OpenSelected()
    {
        if (ResultsList.SelectedItem is not Resource resource)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(resource.Target) { UseShellExecute = true });
            _recents.Add(resource);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "coursee");
            return;
        }

        Hide();
    }

    private void MoveSelection(int delta)
    {
        var count = ResultsList.Items.Count;
        if (count == 0)
        {
            return;
        }

        ResultsList.SelectedIndex = Math.Clamp(ResultsList.SelectedIndex + delta, 0, count - 1);
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (IsSettingsOpen)
            {
                ShowSearch();
            }
            else
            {
                Hide();
            }

            e.Handled = true;
            return;
        }

        if (IsSettingsOpen && e.Key != Key.Q)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                OpenSelected();
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Q when Keyboard.Modifiers == ModifierKeys.Control:
                Application.Current.Shutdown();
                e.Handled = true;
                break;
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Refresh();

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsSettingsOpen)
        {
            ShowSearch();
        }
        else
        {
            ShowSettings();
        }
    }

    private void AutoStartCheckBox_Click(object sender, RoutedEventArgs e)
    {
        var wanted = AutoStartCheckBox.IsChecked == true;

        try
        {
            _autoStart.SetEnabled(wanted);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            MessageBox.Show(ex.Message, "coursee");
            AutoStartCheckBox.IsChecked = !wanted;
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e) => Hide();
}
