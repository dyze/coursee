using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Coursee.Core.Entries;
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
    private readonly SourceCatalog _sources;
    private readonly Action _onSourcesChanged;
    private readonly Action<Window> _openLog;
    private readonly Button _addFolderButton;
    private readonly Dictionary<string, CheckBox> _quickAccessCheckBoxes = new(StringComparer.OrdinalIgnoreCase);
    private SettingsCatalog _catalog;
    private CheckBox? _quickAccessCheckBox;
    private bool _capturingHotkey;
    private bool _focusHotkey;
    private bool _loading;

    /// <param name="sources">All the sources; those that can be switched off get a checkbox, the user's folders a remove button too.</param>
    /// <param name="onSourcesChanged">Called after a source was switched on or off, or a folder added or removed.</param>
    public SettingsWindow(
        SourceCatalog sources,
        SettingsStore settings,
        RecentStore recents,
        EntryStore entries,
        CustomEntryStore customEntries,
        CustomEntryEditor customEntryEditor,
        IAutoStart autoStart,
        IHotkeyRegistrar hotkeys,
        LogHub logHub,
        Action onSourcesChanged,
        Action<Window> openLog)
    {
        _settings = settings;
        _recents = recents;
        _autoStart = autoStart;
        _hotkeys = hotkeys;
        _logHub = logHub;
        _log = logHub.Filter;
        _sources = sources;
        _onSourcesChanged = onSourcesChanged;
        _openLog = openLog;
        _catalog = new SettingsCatalog(sources.All);
        InitializeComponent();

        RecentsComboBox.ItemsSource = SettingsStore.RecentsCapacityChoices;
        ThemeComboBox.ItemsSource = ThemeOptions;
        LauncherPositionComboBox.ItemsSource = PositionOptions;
        _addFolderButton = new Button
        {
            Content = "Add folder...",
            Tag = SettingsCatalog.AddFolderId,
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(12, 4, 12, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            ToolTip = "Search the files and folders at the top of a folder, like the Desktop",
        };
        _addFolderButton.Click += AddFolderButton_Click;
        ShowSources();

        // Quick access is read in the background: a folder pinned or unpinned meanwhile shows up here.
        Action onChanged = () => Dispatcher.BeginInvoke(() => ShowSources());
        sources.Changed += onChanged;
        Closed += (_, _) => sources.Changed -= onChanged;

        EntriesHost.Content = new EntriesPanel(entries, customEntries, customEntryEditor, new SourceIcons(() => sources.All), _log, ShowMessage);

        var keyBindings = new ListCollectionView(KeyBindings.All.ToList());
        keyBindings.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Core.Settings.KeyBinding.Area)));
        KeyBindingsList.ItemsSource = keyBindings;

        LoadValues();
        TabList.ItemsSource = _catalog.Tabs;
        TabList.SelectedIndex = 0;

        Loaded += (_, _) => (_focusHotkey ? (UIElement)HotkeyButton : FindBox).Focus();
        Closed += (_, _) => StopCapturingHotkey();
    }

    /// <summary>
    /// Fills the Sources tab: a checkbox per source that can be switched off, with a remove button
    /// for the user's folders, then the Add folder button. The Quick access folders come together
    /// (see <see cref="SourceCatalog"/>), indented under a checkbox that switches them all.
    /// Called again when the sources change.
    /// </summary>
    private void ShowSources()
    {
        var switchable = _sources.All.Where(s => s.CanBeDisabled).ToList();
        _quickAccessCheckBoxes.Clear();
        _quickAccessCheckBox = null;
        _catalog = new SettingsCatalog(_sources.All, hasQuickAccess: switchable.Any(s => _sources.IsQuickAccess(s.Name)));

        // The section title and the Add folder button stay, so the button keeps the focus.
        var rows = SourcesSection.Children.OfType<UIElement>().Skip(1).Where(child => child != _addFolderButton).ToList();
        rows.ForEach(SourcesSection.Children.Remove);
        if (!SourcesSection.Children.Contains(_addFolderButton))
        {
            SourcesSection.Children.Add(_addFolderButton);
        }

        void AddRow(UIElement row) => SourcesSection.Children.Insert(SourcesSection.Children.Count - 1, row);

        foreach (var source in switchable)
        {
            var inQuickAccess = _sources.IsQuickAccess(source.Name);
            if (inQuickAccess && _quickAccessCheckBox is null)
            {
                AddRow(QuickAccessRow());
            }

            AddRow(SourceRow(source, inQuickAccess));
        }

        ShowQuickAccessState();

        if (IsLoaded)
        {
            UpdateSections();
        }
    }

    /// <summary>The group of the Quick access folders: ticked when all are on, filled when some are.</summary>
    private CheckBox QuickAccessRow()
    {
        var checkBox = new CheckBox
        {
            Content = SourceLabel(SourceIcons.QuickAccess, "Quick access"),
            Tag = SettingsCatalog.QuickAccessId,
            Margin = new Thickness(0, 10, 0, 0),
            ToolTip = "The folders pinned in Explorer's Quick access: unpin one there to remove it",
        };
        checkBox.Click += (_, _) => QuickAccessCheckBox_Click(checkBox);
        _quickAccessCheckBox = checkBox;
        return checkBox;
    }

    private void ShowQuickAccessState()
    {
        if (_quickAccessCheckBox is not null)
        {
            _quickAccessCheckBox.IsChecked = _settings.AreSourcesEnabled(_quickAccessCheckBoxes.Keys);
        }
    }

    /// <param name="inQuickAccess">Indented under the Quick access group.</param>
    private StackPanel SourceRow(IResourceSource source, bool inQuickAccess)
    {
        var name = source.Name;
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Tag = SettingsCatalog.SourceId(name),
            Margin = new Thickness(inQuickAccess ? 28 : 0, inQuickAccess ? 6 : 10, 0, 0),
        };

        var checkBox = new CheckBox
        {
            Content = SourceLabel(source.Icon, source.DisplayName),
            IsChecked = _settings.IsSourceEnabled(name),
            VerticalAlignment = VerticalAlignment.Center,
        };
        checkBox.Click += (_, _) => SourceCheckBox_Click(checkBox, name);
        row.Children.Add(checkBox);
        if (inQuickAccess)
        {
            _quickAccessCheckBoxes[name] = checkBox;
        }

        if (_sources.CanRemove(name))
        {
            var remove = new Button
            {
                Content = "\uE711", // Cancel
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(6, 3, 6, 3),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = $"Remove {source.DisplayName}",
            };
            System.Windows.Automation.AutomationProperties.SetName(remove, $"Remove {source.DisplayName}");
            remove.Click += (_, _) => RemoveFolder(name, source.DisplayName);
            row.Children.Add(remove);
        }

        return row;
    }

    /// <summary>The name of a source after its icon, the one its rows show in the launcher.</summary>
    private static StackPanel SourceLabel(string icon, string text)
    {
        var label = new StackPanel { Orientation = Orientation.Horizontal };
        label.Children.Add(new TextBlock
        {
            Text = icon,
            FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7,
        });
        label.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        return label;
    }

    /// <summary>Selects a tab (one of <see cref="SettingsCatalog.Tabs"/>).</summary>
    public void ShowTab(string tab) => TabList.SelectedItem = tab;

    private void LoadValues()
    {
        _loading = true;
        ShowAutoStart();
        RecentsComboBox.SelectedItem = _settings.RecentsCapacity;
        DebugLoggingCheckBox.IsChecked = _settings.DebugLogging;
        IgnoreAccentsCheckBox.IsChecked = _settings.IgnoreAccents;
        ThemeComboBox.SelectedItem = ThemeOptions.First(option => option.Value == _settings.Theme);
        LauncherPositionComboBox.SelectedItem = PositionOptions.First(option => option.Value == _settings.LauncherPosition);

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
        ShowTab(SettingsCatalog.KeysTab);

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

        var showEntries = !searching && tab == SettingsCatalog.EntriesTab;
        EntriesHost.Visibility = showEntries ? Visibility.Visible : Visibility.Collapsed;
        SectionsScroller.Visibility = showEntries ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>The Entries row of the search results: clears the search and shows that tab.</summary>
    private void ShowEntriesButton_Click(object sender, RoutedEventArgs e)
    {
        FindBox.Clear();
        ShowTab(SettingsCatalog.EntriesTab);
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
        else if (e.Key == Key.Tab && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            SwitchTab(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Ctrl+Tab / Ctrl+Shift+Tab: the next or previous tab, wrapping around. A search in progress
    /// is cleared, so that the tab is shown. The focus goes to the tab strip, since the control
    /// that had it may now be hidden.
    /// </summary>
    private void SwitchTab(int step)
    {
        FindBox.Clear();

        var count = _catalog.Tabs.Count;
        var index = (Math.Max(TabList.SelectedIndex, 0) + step + count) % count;
        TabList.SelectedIndex = index;
        if (TabList.ItemContainerGenerator.ContainerFromIndex(index) is UIElement tab)
        {
            tab.Focus();
        }
    }

    /// <summary>Down in the Find box: to the first setting shown (of the tab, or of the search results).</summary>
    private void FindBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.None)
        {
            var shown = EntriesHost.IsVisible ? (UIElement)EntriesHost : SectionsPanel;
            shown.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
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
            _onSourcesChanged();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", $"Cannot change source '{sourceName}'", ex);
            ShowMessage(ex.Message);
            checkBox.IsChecked = !wanted;
        }

        ShowQuickAccessState();
    }

    /// <summary>Switches every Quick access folder; a partly ticked group is switched on.</summary>
    private void QuickAccessCheckBox_Click(CheckBox checkBox)
    {
        var wanted = checkBox.IsChecked == true;

        try
        {
            _settings.SetSourcesEnabled(_quickAccessCheckBoxes.Keys, wanted);
            _log.Info("Settings", $"Quick access folders {(wanted ? "on" : "off")}");
            _onSourcesChanged();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", "Cannot change the Quick access folders", ex);
            ShowMessage(ex.Message);
        }

        foreach (var (name, child) in _quickAccessCheckBoxes)
        {
            child.IsChecked = _settings.IsSourceEnabled(name);
        }

        ShowQuickAccessState();
    }

    private void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Add a folder to search" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var folder = dialog.FolderName;
        try
        {
            switch (_sources.AddFolder(folder))
            {
                case AddFolderResult.Added:
                    _log.Info("Settings", $"Folder source added: {folder}");
                    _onSourcesChanged();
                    break;
                case AddFolderResult.AlreadyAdded:
                    ShowMessage($"{folder} is already searched.");
                    break;
                case AddFolderResult.NetworkFolder:
                    ShowMessage($"{folder} is on the network. Network folders are not searched: listing them can block coursee when the network is slow.");
                    break;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", $"Cannot add folder source {folder}", ex);
            ShowMessage(ex.Message);
        }
    }

    /// <summary>Removes a folder the user added. Its pins, aliases and recents stay, like those of a removed shortcut.</summary>
    private void RemoveFolder(string sourceName, string displayName)
    {
        try
        {
            if (_sources.RemoveFolder(sourceName))
            {
                _log.Info("Settings", $"Folder source removed: {displayName}");
                _onSourcesChanged();
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", $"Cannot remove folder source {displayName}", ex);
            ShowMessage(ex.Message);
        }

        // Its row goes, with the focused button.
        _addFolderButton.Focus();
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

    private void IgnoreAccentsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        var wanted = IgnoreAccentsCheckBox.IsChecked == true;

        try
        {
            _settings.SetIgnoreAccents(wanted);
            _log.Info("Settings", $"Ignore accents when searching {(wanted ? "on" : "off")}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", "Cannot change ignoring accents", ex);
            ShowMessage(ex.Message);
            IgnoreAccentsCheckBox.IsChecked = !wanted;
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

    /// <summary>Opens the log window in front of this dialog, which stays open (to switch debug logging on and watch, say).</summary>
    private void OpenLogButton_Click(object sender, RoutedEventArgs e) => _openLog(this);

    private void ShowMessage(string message) => MessageDialog.Inform(this, message);
}
