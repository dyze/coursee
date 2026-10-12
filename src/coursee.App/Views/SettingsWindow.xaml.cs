using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Coursee.Core.Logging;
using Coursee.Core.Platform;
using Coursee.Core.Recents;
using Coursee.Core.Settings;
using Coursee.Core.Sources;
using Coursee.UI.Logging;

namespace Coursee.UI.Views;

/// <summary>
/// The settings, grouped in tabs. Each change is saved at once. A normal window: unlike the
/// launcher, it stays open when it loses the focus.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly RecentStore _recents;
    private readonly IAutoStart _autoStart;
    private readonly IHotkeyRegistrar _hotkeys;
    private readonly LogHub _logHub;
    private readonly ILog _log;
    private readonly SettingsCatalog _catalog;
    private readonly Action<string> _onSourceToggled;
    private readonly Action _openLog;
    private readonly Dictionary<string, CheckBox> _sourceCheckBoxes = [];
    private bool _capturingHotkey;
    private bool _loading;

    /// <param name="sources">All the sources; those that can be switched off get a checkbox.</param>
    /// <param name="onSourceToggled">Called with the source name after it was switched on or off.</param>
    /// <param name="openLog">Opens the log window.</param>
    public SettingsWindow(
        IReadOnlyList<IResourceSource> sources,
        SettingsStore settings,
        RecentStore recents,
        IAutoStart autoStart,
        IHotkeyRegistrar hotkeys,
        LogHub logHub,
        Action<string> onSourceToggled,
        Action openLog)
    {
        _settings = settings;
        _recents = recents;
        _autoStart = autoStart;
        _hotkeys = hotkeys;
        _logHub = logHub;
        _log = logHub.Filter;
        _onSourceToggled = onSourceToggled;
        _openLog = openLog;
        _catalog = new SettingsCatalog(sources);
        InitializeComponent();

        RecentsComboBox.ItemsSource = SettingsStore.RecentsCapacityChoices;
        foreach (var source in sources.Where(s => s.CanBeDisabled))
        {
            var name = source.Name;
            var checkBox = new CheckBox
            {
                Content = source.DisplayName,
                Tag = SettingsCatalog.SourceId(name),
                Margin = new Thickness(0, 10, 0, 0),
            };
            checkBox.Click += (_, _) => SourceCheckBox_Click(checkBox, name);
            SourcesSection.Children.Add(checkBox);
            _sourceCheckBoxes[name] = checkBox;
        }

        LoadValues();
        TabList.ItemsSource = _catalog.Tabs;
        TabList.SelectedIndex = 0;

        Closed += (_, _) => StopCapturingHotkey();
    }

    /// <summary>Selects a tab (one of <see cref="SettingsCatalog.Tabs"/>).</summary>
    public void ShowTab(string tab) => TabList.SelectedItem = tab;

    private void LoadValues()
    {
        _loading = true;
        AutoStartCheckBox.IsChecked = ReadAutoStart();
        RecentsComboBox.SelectedItem = _settings.RecentsCapacity;
        DebugLoggingCheckBox.IsChecked = _settings.DebugLogging;
        foreach (var (name, checkBox) in _sourceCheckBoxes)
        {
            checkBox.IsChecked = _settings.IsSourceEnabled(name);
        }

        HotkeyText.Text = _settings.Hotkey.ToString();
        _loading = false;
    }

    /// <summary>The panel of each tab; its Tag is the tab name.</summary>
    private IEnumerable<StackPanel> Sections => SectionsPanel.Children.OfType<StackPanel>();

    /// <summary>Shows the panel of the selected tab only.</summary>
    private void UpdateSections()
    {
        var tab = TabList.SelectedItem as string;
        foreach (var section in Sections)
        {
            section.Visibility = Equals(section.Tag, tab) ? Visibility.Visible : Visibility.Collapsed;
            foreach (UIElement child in section.Children)
            {
                // A tab shows its rows under the tab strip: the section title would repeat the tab name.
                child.Visibility = IsHeader(child) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }

    /// <summary>A section title: the only child of a section without a setting id.</summary>
    private static bool IsHeader(UIElement element) => element is FrameworkElement { Tag: null };

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

    private void TabList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSections();

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
            HotkeyText.Text = "Hold Ctrl, Alt or Win too...";
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
                ShowMessage(ex.Message);
            }
        }
        else
        {
            _log.Warn("Settings", $"Hotkey {gesture} is not available");
            ShowMessage($"The shortcut {gesture} is not available. Try another one.");
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
            Close();
            e.Handled = true;
        }
    }

    private void HotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _capturingHotkey = true;
        _hotkeys.Paused = true;
        HotkeyText.Text = "Press the new shortcut (Esc to cancel)...";
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
            ShowMessage(ex.Message);
            AutoStartCheckBox.IsChecked = !wanted;
        }
    }

    private void SourceCheckBox_Click(CheckBox checkBox, string sourceName)
    {
        var wanted = checkBox.IsChecked == true;

        try
        {
            _settings.SetSourceEnabled(sourceName, wanted);
            _log.Info("Settings", $"Source '{sourceName}' {(wanted ? "on" : "off")}");
            _onSourceToggled(sourceName);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", $"Cannot change source '{sourceName}'", ex);
            ShowMessage(ex.Message);
            checkBox.IsChecked = !wanted;
        }
    }

    private void RecentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || RecentsComboBox.SelectedItem is not int capacity)
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
            ShowMessage(ex.Message);
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
            ShowMessage(ex.Message);
            DebugLoggingCheckBox.IsChecked = !wanted;
        }
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e) => _openLog();

    private void ShowMessage(string message) => MessageBox.Show(this, message, "coursee");
}
