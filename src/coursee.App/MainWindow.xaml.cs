using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Coursee.Core;

namespace Coursee.UI;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<IResourceSource> _sources;
    private readonly RecentStore _recents;
    private readonly SettingsStore _settings;
    private readonly IAutoStart _autoStart;
    private readonly IHotkeyRegistrar _hotkeys;
    private readonly LogHub _logHub;
    private readonly ILog _log;
    private readonly SourceMonitor _monitor;
    private LogWindow? _logWindow;
    private IReadOnlyList<Resource> _all = [];
    private bool _capturingHotkey;
    private bool _loadingSettings;

    public MainWindow(
        IReadOnlyList<IResourceSource> sources,
        RecentStore recents,
        SettingsStore settings,
        IAutoStart autoStart,
        IHotkeyRegistrar hotkeys,
        LogHub logHub)
    {
        _logHub = logHub;
        _log = logHub.Filter;
        _monitor = new SourceMonitor(_log);
        _sources = sources;
        _recents = recents;
        _settings = settings;
        _autoStart = autoStart;
        _hotkeys = hotkeys;
        InitializeComponent();
        RecentsComboBox.ItemsSource = SettingsStore.RecentsCapacityChoices;
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
        _log.Debug("Window", "Shown");
        _all = LoadResources();
        SearchBox.Text = string.Empty;
        Refresh();
        ShowSearch();
        Show();
        Activate();
        SearchBox.Focus();
    }

    private List<Resource> LoadResources()
    {
        var all = new List<Resource>();
        foreach (var source in _sources.Where(s => _settings.IsSourceEnabled(s.Name)))
        {
            try
            {
                var resources = source.GetResources();
                _monitor.Observe(source.Name, resources);
                all.AddRange(resources);
            }
            catch (Exception ex)
            {
                _log.Error(source.Name, "Source failed", ex);
            }
        }

        return all;
    }

    private bool IsSettingsOpen => SettingsPanel.Visibility == Visibility.Visible;

    private void ShowSearch()
    {
        StopCapturingHotkey();
        SettingsPanel.Visibility = Visibility.Collapsed;
        ActionText.Visibility = Visibility.Visible;
        ResultsList.Visibility = Visibility.Visible;
        SearchBox.IsEnabled = true;
        SearchBox.Focus();
    }

    private void ShowSettings()
    {
        AutoStartCheckBox.IsChecked = ReadAutoStart();
        FirefoxCheckBox.IsChecked = _settings.IsSourceEnabled(FirefoxBookmarksSource.SourceName);
        RunHistoryCheckBox.IsChecked = _settings.IsSourceEnabled(RunHistorySource.SourceName);
        DebugLoggingCheckBox.IsChecked = _settings.DebugLogging;

        _loadingSettings = true;
        RecentsComboBox.SelectedItem = _settings.RecentsCapacity;
        _loadingSettings = false;

        StopCapturingHotkey();
        ActionText.Visibility = Visibility.Collapsed;
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
        IReadOnlyList<Resource> items;

        if (string.IsNullOrWhiteSpace(query))
        {
            items = _recents.Items;
        }
        else
        {
            items = SearchEngine.Search(_all, query);

            // Nothing matches: offer what the Windows Run dialog would do with the typed text.
            if (items.Count == 0 && FallbackClassifier.Classify(query) is { } fallback)
            {
                items = [fallback];
            }
        }

        ResultsList.ItemsSource = items;
        if (items.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
        }

        UpdateActionText();
    }

    private void UpdateActionText()
    {
        ActionText.Text = ResultsList.SelectedItem switch
        {
            Resource { Source: FallbackClassifier.SourceName } typed => $"Enter: {typed.ActionLabel} (what you typed)",
            Resource resource => $"Enter: {resource.ActionLabel}",
            _ => string.IsNullOrWhiteSpace(SearchBox.Text)
                ? "Type to search, or type a path, URL or command"
                : "No match",
        };
    }

    private static void Launch(Resource resource)
    {
        if (resource.Kind == ResourceKind.Command)
        {
            var (fileName, arguments) = CommandLine.Split(resource.Target);
            Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
        }
        else
        {
            Process.Start(new ProcessStartInfo(resource.Target) { UseShellExecute = true });
        }
    }

    private void OpenSelected()
    {
        if (ResultsList.SelectedItem is not Resource resource)
        {
            return;
        }

        var typed = resource.Source == FallbackClassifier.SourceName;
        try
        {
            Launch(resource);
            _recents.Add(resource);
            _log.Info(
                "Action",
                $"{resource.ActionLabel}: {resource.Target} ({(typed ? "typed" : resource.Source)}, {resource.Kind})");
        }
        catch (Exception ex)
        {
            _log.Error("Action", $"{resource.ActionLabel} failed for {resource.Target}", ex);
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

    private void StopCapturingHotkey()
    {
        _capturingHotkey = false;
        _hotkeys.Paused = false;
        HotkeyText.Text = _settings.Hotkey.ToString();
    }

    private void CaptureHotkey(KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            StopCapturingHotkey();
            return;
        }

        if (key is Key.None or Key.DeadCharProcessed or Key.ImeProcessed
            or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var gesture = new HotkeyGesture(
            Ctrl: modifiers.HasFlag(ModifierKeys.Control),
            Alt: modifiers.HasFlag(ModifierKeys.Alt),
            Shift: modifiers.HasFlag(ModifierKeys.Shift),
            Win: modifiers.HasFlag(ModifierKeys.Windows),
            Key: key.ToString());

        if (!gesture.IsValid)
        {
            HotkeyText.Text = "Hold Ctrl, Alt, Shift or Win too...";
            return;
        }

        if (_hotkeys.TryRegister(gesture))
        {
            try
            {
                _settings.SetHotkey(gesture);
                _log.Info("Settings", $"Hotkey changed to {gesture}");
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                MessageBox.Show(ex.Message, "coursee");
            }
        }
        else
        {
            _log.Warn("Settings", $"Hotkey {gesture} is not available");
            MessageBox.Show($"The shortcut {gesture} is not available. Try another one.", "coursee");
        }

        StopCapturingHotkey();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturingHotkey)
        {
            CaptureHotkey(e);
            return;
        }

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

    private void ResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateActionText();

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
            _log.Info("Settings", $"Start with Windows {(wanted ? "on" : "off")}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            _log.Error("Settings", "Cannot change start with Windows", ex);
            MessageBox.Show(ex.Message, "coursee");
            AutoStartCheckBox.IsChecked = !wanted;
        }
    }

    private void SourceCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.CheckBox { Tag: string sourceName } checkBox)
        {
            return;
        }

        var wanted = checkBox.IsChecked == true;

        try
        {
            _settings.SetSourceEnabled(sourceName, wanted);
            _monitor.Reset(sourceName);
            _log.Info("Settings", $"Source '{sourceName}' {(wanted ? "on" : "off")}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", $"Cannot change source '{sourceName}'", ex);
            MessageBox.Show(ex.Message, "coursee");
            checkBox.IsChecked = !wanted;
        }
    }

    private void HotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _capturingHotkey = true;
        _hotkeys.Paused = true;
        HotkeyText.Text = "Press the new shortcut (Esc to cancel)...";
        Focus();
    }

    private void RecentsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loadingSettings || RecentsComboBox.SelectedItem is not int capacity)
        {
            return;
        }

        try
        {
            _settings.SetRecentsCapacity(capacity);
            _recents.SetCapacity(capacity);
            _log.Info("Settings", $"Recent items to keep: {capacity}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", "Cannot change recent items", ex);
            MessageBox.Show(ex.Message, "coursee");
        }
    }

    private void DebugLoggingCheckBox_Click(object sender, RoutedEventArgs e)
    {
        var wanted = DebugLoggingCheckBox.IsChecked == true;

        try
        {
            _settings.SetDebugLogging(wanted);
            _logHub.SetDebug(wanted);
            _log.Info("Settings", $"Detailed (debug) logging {(wanted ? "on" : "off")}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", "Cannot change debug logging", ex);
            MessageBox.Show(ex.Message, "coursee");
            DebugLoggingCheckBox.IsChecked = !wanted;
        }
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        if (_logWindow is { IsLoaded: true })
        {
            _logWindow.WindowState = WindowState.Normal;
            _logWindow.Activate();
            return;
        }

        _logWindow = new LogWindow(_logHub.Ring, _logHub.Directory);
        _logWindow.Closed += (_, _) => _logWindow = null;
        _logWindow.Show();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        StopCapturingHotkey();
        Hide();
    }
}
