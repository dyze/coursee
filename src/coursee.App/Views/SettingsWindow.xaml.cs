using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Coursee.Core.Logging;
using Coursee.Core.Platform;
using Coursee.Core.Recents;
using Coursee.Core.Settings;
using Coursee.Core.Sources;
using Coursee.UI.Logging;
using Coursee.UI.Platform;

namespace Coursee.UI.Views;

/// <summary>
/// The settings, grouped in tabs. Each change is saved at once. Shown as a dialog over the
/// launcher, which is still there when it closes.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>A choice of a combo box; the box shows <see cref="ToString"/>.</summary>
    private sealed record Option<T>(T Value, string Label)
    {
        public override string ToString() => Label;
    }

    private static readonly Option<ThemeChoice>[] ThemeOptions =
    [
        new(ThemeChoice.System, "Follow Windows"),
        new(ThemeChoice.Light, "Light"),
        new(ThemeChoice.Dark, "Dark"),
    ];

    private static readonly Option<LauncherPosition>[] PositionOptions =
    [
        new(LauncherPosition.Centered, "Centered on the mouse's monitor"),
        new(LauncherPosition.AtMouse, "At the mouse"),
        new(LauncherPosition.LastPosition, "Where I left it"),
    ];

    private readonly SettingsStore _settings;
    private readonly RecentStore _recents;
    private readonly IAutoStart _autoStart;
    private readonly IHotkeyRegistrar _hotkeys;
    private readonly LogHub _logHub;
    private readonly ILog _log;
    private readonly SettingsCatalog _catalog;
    private readonly Action<string> _onSourceToggled;
    private readonly Dictionary<string, CheckBox> _sourceCheckBoxes = [];
    private bool _capturingHotkey;
    private bool _focusHotkey;
    private bool _loading;

    /// <param name="sources">All the sources; those that can be switched off get a checkbox.</param>
    /// <param name="onSourceToggled">Called with the source name after it was switched on or off.</param>
    public SettingsWindow(
        IReadOnlyList<IResourceSource> sources,
        SettingsStore settings,
        RecentStore recents,
        IAutoStart autoStart,
        IHotkeyRegistrar hotkeys,
        LogHub logHub,
        Action<string> onSourceToggled)
    {
        _settings = settings;
        _recents = recents;
        _autoStart = autoStart;
        _hotkeys = hotkeys;
        _logHub = logHub;
        _log = logHub.Filter;
        _onSourceToggled = onSourceToggled;
        _catalog = new SettingsCatalog(sources);
        InitializeComponent();

        RecentsComboBox.ItemsSource = SettingsStore.RecentsCapacityChoices;
        ThemeComboBox.ItemsSource = ThemeOptions;
        LauncherPositionComboBox.ItemsSource = PositionOptions;
        foreach (var source in sources.Where(s => s.CanBeDisabled))
        {
            var name = source.Name;
            var checkBox = new CheckBox
            {
                Content = SourceLabel(source),
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

        Loaded += (_, _) => (_focusHotkey ? (UIElement)HotkeyButton : FindBox).Focus();
        Closed += (_, _) => StopCapturingHotkey();
    }

    /// <summary>The name of a source after its icon, the one its rows show in the launcher.</summary>
    private static StackPanel SourceLabel(IResourceSource source)
    {
        var label = new StackPanel { Orientation = Orientation.Horizontal };
        label.Children.Add(new TextBlock
        {
            Text = source.Icon,
            FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7,
        });
        label.Children.Add(new TextBlock { Text = source.DisplayName, VerticalAlignment = VerticalAlignment.Center });
        return label;
    }

    /// <summary>True when the window was closed by "Open log window": the caller then opens it.</summary>
    public bool LogWindowRequested { get; private set; }

    /// <summary>Selects a tab (one of <see cref="SettingsCatalog.Tabs"/>).</summary>
    public void ShowTab(string tab) => TabList.SelectedItem = tab;

    private void LoadValues()
    {
        _loading = true;
        ShowAutoStart();
        RecentsComboBox.SelectedItem = _settings.RecentsCapacity;
        DebugLoggingCheckBox.IsChecked = _settings.DebugLogging;
        ThemeComboBox.SelectedItem = ThemeOptions.First(option => option.Value == _settings.Theme);
        LauncherPositionComboBox.SelectedItem = PositionOptions.First(option => option.Value == _settings.LauncherPosition);
        foreach (var (name, checkBox) in _sourceCheckBoxes)
        {
            checkBox.IsChecked = _settings.IsSourceEnabled(name);
        }

        ShowHotkey();
        _loading = false;
    }

    /// <summary>
    /// The hotkey could not be registered: explain why, on top of the window, and put the focus
    /// on the button that changes it. The banner goes away once a hotkey works.
    /// </summary>
    public void ShowHotkeyProblem()
    {
        HotkeyBannerText.Text =
            $"The hotkey {_settings.Hotkey} is used by another application or by Windows, so coursee has no hotkey. "
            + "Choose another one with Change. Meanwhile, the coursee icon in the notification area opens the launcher.";
        HotkeyBanner.Visibility = Visibility.Visible;
        ShowTab(SettingsCatalog.GeneralTab);

        _focusHotkey = true;
        if (IsLoaded)
        {
            HotkeyButton.Focus();
        }
    }

    /// <summary>The saved hotkey, marked when it is not registered.</summary>
    private void ShowHotkey() =>
        HotkeyText.Text = _hotkeys.Current is null
            ? $"{_settings.Hotkey} (not active)"
            : _settings.Hotkey.ToString();

    /// <summary>The panel of each tab; its Tag is the tab name.</summary>
    private IEnumerable<StackPanel> Sections => SectionsPanel.Children.OfType<StackPanel>();

    /// <summary>
    /// Without a search, shows the panel of the selected tab only. With one, hides the tab strip
    /// and shows the matching rows of every tab, under the name of their tab.
    /// </summary>
    private void UpdateSections()
    {
        var query = FindBox.Text;
        var searching = !string.IsNullOrWhiteSpace(query);
        var matches = _catalog.Find(query).Select(entry => entry.Id).ToHashSet();
        var tab = TabList.SelectedItem as string;

        FindPlaceholder.Visibility = query.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        TabList.Visibility = searching ? Visibility.Collapsed : Visibility.Visible;

        foreach (var section in Sections)
        {
            var anyShown = false;
            foreach (UIElement child in section.Children)
            {
                if (IsHeader(child))
                {
                    // Under the tab strip, the section title would repeat the tab name.
                    child.Visibility = searching ? Visibility.Visible : Visibility.Collapsed;
                    continue;
                }

                var shown = !searching || (child is FrameworkElement { Tag: string id } && matches.Contains(id));
                child.Visibility = shown ? Visibility.Visible : Visibility.Collapsed;
                anyShown |= shown;
            }

            var visible = searching ? anyShown : Equals(section.Tag, tab);
            section.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        NoMatchText.Visibility = searching && matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>A section title: the only child of a section without a setting id.</summary>
    private static bool IsHeader(UIElement element) => element is FrameworkElement { Tag: null };

    /// <summary>
    /// Ticked when Windows starts this copy. When it starts another one, says which: ticking the
    /// box then makes Windows start this copy instead.
    /// </summary>
    private void ShowAutoStart()
    {
        AutoStartStatus status;
        string? program;
        try
        {
            status = _autoStart.Status;
            program = _autoStart.RegisteredProgram;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            status = AutoStartStatus.Off;
            program = null;
        }

        AutoStartCheckBox.IsChecked = status is AutoStartStatus.ThisCopy or AutoStartStatus.ThisCopyOutdated;
        AutoStartNote.Text = $"Windows starts another copy: {program}. Tick the box to start this one instead.";
        AutoStartNote.Visibility = status == AutoStartStatus.OtherCopy ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>A dialog needs no minimize or maximize button; it can still be resized.</summary>
    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        NativeWindows.RemoveMinimizeMaximizeButtons(new System.Windows.Interop.WindowInteropHelper(this).Handle);

    private void TabList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSections();

    private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            UpdateSections();
        }
    }

    private void StopCapturingHotkey()
    {
        _capturingHotkey = false;
        _hotkeys.Paused = false;
        ShowHotkey();
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
            HotkeyBanner.Visibility = Visibility.Collapsed;
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
            // First Esc clears the search, the next one closes the window.
            if (FindBox.Text.Length > 0)
            {
                FindBox.Clear();
            }
            else
            {
                Close();
            }

            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FindBox.Focus();
            FindBox.SelectAll();
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
        }

        ShowAutoStart();
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

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThemeComboBox.SelectedItem is not Option<ThemeChoice> option || option.Value == _settings.Theme)
        {
            return;
        }

        try
        {
            _settings.SetTheme(option.Value);
            AppTheme.Apply(option.Value);
            _log.Info("Settings", $"Theme: {option.Value}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", "Cannot change the theme", ex);
            ShowMessage(ex.Message);
        }
    }

    private void LauncherPositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading
            || LauncherPositionComboBox.SelectedItem is not Option<LauncherPosition> option
            || option.Value == _settings.LauncherPosition)
        {
            return;
        }

        try
        {
            _settings.SetLauncherPosition(option.Value);
            _log.Info("Settings", $"Launcher position: {option.Value}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", "Cannot change the launcher position", ex);
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

    /// <summary>
    /// The log window would open behind this dialog and the launcher (both stay on top), and
    /// would close with them. So the dialog closes, and the caller opens the log window.
    /// </summary>
    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogWindowRequested = true;
        Close();
    }

    private void ShowMessage(string message) => MessageBox.Show(this, message, "coursee");
}
