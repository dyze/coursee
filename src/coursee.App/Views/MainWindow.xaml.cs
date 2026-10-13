using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Coursee.Core;
using Coursee.Core.Entries;
using Coursee.Core.Logging;
using Coursee.Core.Platform;
using Coursee.Core.Recents;
using Coursee.Core.Resources;
using Coursee.Core.Search;
using Coursee.Core.Settings;
using Coursee.Core.Sources;
using Coursee.UI.Platform;

namespace Coursee.UI.Views;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<IResourceSource> _sources;
    private readonly RecentStore _recents;
    private readonly EntryStore _entries;
    private readonly CustomEntryEditor _customEntries;
    private readonly SettingsStore _settings;
    private readonly ILog _log;
    private readonly IBrowserLocator _browsers;
    private readonly Func<SettingsWindow> _createSettings;
    private readonly SourceMonitor _monitor;
    private IReadOnlyList<Resource> _all = [];

    /// <summary>The row whose text box is open (alias, or name of a new custom entry), or null.</summary>
    private ResultRow? _editingRow;
    private SettingsWindow? _settingsDialog;
    private bool _showingDialog;
    private bool _refreshing;
    private bool _refreshAgain;

    /// <param name="browsers">Finds the browser a bookmark came from, to open it there.</param>
    /// <param name="createSettings">Creates the Settings window, shown as a dialog over the launcher.</param>
    public MainWindow(
        IReadOnlyList<IResourceSource> sources,
        RecentStore recents,
        EntryStore entries,
        CustomEntryEditor customEntries,
        SettingsStore settings,
        ILog log,
        IBrowserLocator browsers,
        Func<SettingsWindow> createSettings)
    {
        _log = log;
        _browsers = browsers;
        _monitor = new SourceMonitor(_log);
        _sources = sources;
        _recents = recents;
        _entries = entries;
        _customEntries = customEntries;
        _settings = settings;
        _createSettings = createSettings;

        // Before the XAML, which uses it in the rows of the list.
        Resources.Add("SourceIcon", new SourceIconConverter(new SourceIcons(sources)));
        InitializeComponent();
        VersionText.Text = $"coursee {AppVersion.Of(typeof(MainWindow).Assembly)}";
    }

    public void Toggle()
    {
        if (_settingsDialog is not null)
        {
            // Settings is open over the launcher: the hotkey brings it to the front.
            _settingsDialog.Activate();
        }
        else if (IsVisible)
        {
            Hide();
        }
        else
        {
            ShowLauncher();
        }
    }

    /// <summary>Shows the launcher when asked by another copy of coursee (never hides it).</summary>
    public void ShowFromOutside()
    {
        if (_settingsDialog is not null)
        {
            _settingsDialog.Activate();
        }
        else if (IsVisible)
        {
            Activate();
            SearchBox.Focus();
        }
        else
        {
            ShowLauncher();
        }
    }

    private void ShowLauncher()
    {
        _log.Debug("Window", "Shown");

        // Show the list from the last refresh at once; the fresh one replaces it when ready.
        SearchBox.Text = string.Empty;
        Refresh();
        WindowPlacer.PlaceLauncher(this, _settings.LauncherPosition, _log);
        Show();
        Activate();
        SearchBox.Focus();
        RefreshResources();
    }

    /// <summary>A source was switched on or off in Settings: forget what it had, and reload.</summary>
    public void OnSourceToggled(string sourceName)
    {
        _monitor.Reset(sourceName);
        RefreshResources();
    }

    /// <summary>
    /// Reloads the enabled sources on a background thread, then updates the list. Only one
    /// refresh runs at a time; a request made meanwhile runs once the current one ends.
    /// </summary>
    public async void RefreshResources()
    {
        if (_refreshing)
        {
            _refreshAgain = true;
            return;
        }

        _refreshing = true;
        try
        {
            do
            {
                _refreshAgain = false;
                var enabled = _sources.Where(s => _settings.IsSourceEnabled(s.Name)).ToList();
                var results = await Task.Run(() => SourceLoader.Load(enabled));
                ApplyResults(results);
            }
            while (_refreshAgain);
        }
        catch (Exception ex)
        {
            _log.Error("App", "Refreshing the sources failed", ex);
        }
        finally
        {
            _refreshing = false;
        }
    }

    /// <summary>Runs on the UI thread: tracks and logs changes, then updates the visible list.</summary>
    private void ApplyResults(IReadOnlyList<SourceResult> results)
    {
        var lists = new List<IReadOnlyList<Resource>>();
        foreach (var result in results)
        {
            // Switched off while this refresh was running.
            if (!_settings.IsSourceEnabled(result.SourceName))
            {
                continue;
            }

            if (result.Error is not null)
            {
                _log.Error(result.SourceName, "Source failed", result.Error);
                continue;
            }

            _monitor.Observe(result.SourceName, result.Resources);
            lists.Add(result.Resources);
        }

        _all = SourceLoader.Merge(lists);

        // Not under an open row box: the list is rebuilt when it closes.
        if (IsVisible && !string.IsNullOrWhiteSpace(SearchBox.Text) && _editingRow is null)
        {
            Refresh(keepSelection: true);
        }
    }

    /// <param name="keepSelection">True when the list is rebuilt under the user (background refresh):
    /// the selected row stays selected. Otherwise the best match is selected.</param>
    private void Refresh(bool keepSelection = false)
    {
        // A new list has no row box open.
        _editingRow = null;

        var selectedTarget = keepSelection ? Selected?.Target : null;
        var query = SearchBox.Text;
        IReadOnlyList<Resource> items;

        if (string.IsNullOrWhiteSpace(query))
        {
            items = _entries.Visible(_recents.Items);
        }
        else
        {
            items = SearchEngine.Search(
                _entries.Visible(_all),
                query,
                new SearchBoosts(
                    UsageBonus: _recents.UsageBonus,
                    IsPinned: _entries.IsPinned,
                    AliasTarget: _entries.TargetOfAlias),
                ignoreAccents: _settings.IgnoreAccents);

            // Nothing matches: offer what the Windows Run dialog would do with the typed text.
            if (items.Count == 0 && FallbackClassifier.Classify(query) is { } fallback)
            {
                items = [fallback];
            }
        }

        // Nothing matched: the typed text can become a custom entry.
        AddButton.Visibility = items is [var only] && ResourceActions.CanMakeCustomEntry(only)
            ? Visibility.Visible
            : Visibility.Collapsed;

        ResultsList.ItemsSource = items
            .Select(r => new ResultRow(r, _entries.IsPinned(r.Target), _entries.AliasOf(r.Target)))
            .ToList();
        if (items.Count > 0)
        {
            var keep = selectedTarget is null
                ? -1
                : items.ToList().FindIndex(r => string.Equals(r.Target, selectedTarget, StringComparison.OrdinalIgnoreCase));
            ResultsList.SelectedIndex = Math.Max(keep, 0);
        }

        // A new list (new search, launcher shown again) starts at the top; a background refresh keeps the scroll.
        if (!keepSelection)
        {
            FindChild<ScrollViewer>(ResultsList)?.ScrollToTop();
        }

        UpdateActionText();
    }

    private void UpdateActionText()
    {
        if (ResultsList.SelectedItem is ResultRow row)
        {
            ActionText.Text = ResourceActions.Hint(row.Resource);
            KeysText.Text = ResourceActions.KeysHint(row.Resource, inRecents: IsShowingRecents, isPinned: row.IsPinned);
            return;
        }

        ActionText.Text = string.IsNullOrWhiteSpace(SearchBox.Text)
            ? "Type to search, or type a path, URL or command"
            : "No match";
        KeysText.Text = string.Empty;
    }

    /// <summary>The resource of the selected row, if any.</summary>
    private Resource? Selected => (ResultsList.SelectedItem as ResultRow)?.Resource;

    /// <summary>The recent list is shown (search box empty), so Del can remove the selected item.</summary>
    private bool IsShowingRecents => string.IsNullOrWhiteSpace(SearchBox.Text);

    private const int ErrorCancelled = 1223;

    private void Launch(Resource resource, bool asAdministrator)
    {
        if (ResourceActions.BrowserExecutable(resource, _browsers) is { } browser)
        {
            // The URL is an argument of its own: nothing in it can change the command line.
            var inBrowser = new ProcessStartInfo(browser) { UseShellExecute = false };
            inBrowser.ArgumentList.Add(resource.Target);
            Process.Start(inBrowser);
            return;
        }

        if (resource.Kind == ResourceKind.Url && resource.Browser is not null)
        {
            _log.Warn("Action", $"{resource.Browser} not found; opening in the default browser");
        }

        ProcessStartInfo start;
        // A Windows setting is a command too: an ms-settings: page, a console, or a program with arguments.
        if (resource.Kind is ResourceKind.Command or ResourceKind.Setting)
        {
            var (fileName, arguments) = CommandLine.Split(resource.Target);
            start = new ProcessStartInfo(fileName, arguments);
        }
        else
        {
            // For a shortcut this is the .lnk, so its arguments and working folder are kept.
            start = new ProcessStartInfo(resource.OpenTarget);
        }

        start.UseShellExecute = true;
        if (asAdministrator)
        {
            start.Verb = "runas";
        }

        Process.Start(start);
    }

    /// <summary>Enter (or Ctrl+Shift+Enter, elevated): opens the selected item, remembers it, hides.</summary>
    private void OpenSelected(bool asAdministrator = false)
    {
        if (Selected is not { } resource)
        {
            return;
        }

        if (asAdministrator && !ResourceActions.CanRunAsAdministrator(resource))
        {
            return;
        }

        var label = asAdministrator ? "Run as administrator" : resource.ActionLabel;
        var typed = resource.Source == FallbackClassifier.SourceName;
        try
        {
            Launch(resource, asAdministrator);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            // The user said no to the Windows confirmation (Run as administrator, or a tool that
            // always asks, such as the Registry Editor): nothing to report.
            _log.Info("Action", $"{label} cancelled: {resource.Target}");
            return;
        }
        catch (Exception ex)
        {
            // Stay open so the user can correct the text or pick another item.
            _log.Error("Action", $"{label} failed for {resource.Target}", ex);
            ShowMessage(ex.Message);
            return;
        }

        _log.Info(
            "Action",
            $"{label}: {resource.Target} ({(typed ? "typed" : resource.Source)}, {resource.Kind})");

        // The resource is open: failing to remember it is only worth a log entry.
        try
        {
            _recents.Add(resource);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Warn("Recents", $"Cannot save the recent items: {ex.GetType().Name}: {ex.Message}");
        }

        Hide();
    }

    /// <summary>Ctrl+Enter: opens the containing folder in Explorer with the item selected.</summary>
    private void ShowSelectedInFolder()
    {
        if (Selected is not { } resource || !ResourceActions.CanShowInFolder(resource))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", ResourceActions.ShowInFolderArguments(resource)));
        }
        catch (Exception ex)
        {
            _log.Error("Action", $"Show in folder failed for {resource.Target}", ex);
            ShowMessage(ex.Message);
            return;
        }

        _log.Info("Action", $"Show in folder: {resource.Target} ({resource.Source})");
        Hide();
    }

    /// <summary>
    /// Ctrl+C with no text selected in the search box: copies the selected item's target (the path,
    /// URL or command shown under its name). The launcher stays open; the second line under the search
    /// box says it was copied until the selection or the text changes.
    /// </summary>
    private void CopySelected()
    {
        if (Selected is not { } resource)
        {
            return;
        }

        try
        {
            Clipboard.SetText(resource.Target);
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            // Another application holds the clipboard.
            _log.Warn("Action", $"Copy failed for {resource.Target}: {ex.Message}");
            ShowMessage($"The clipboard is busy, try again. ({ex.Message})");
            return;
        }

        _log.Info("Action", $"Copied: {resource.Target} ({resource.Source})");
        KeysText.Text = $"Copied: {resource.Target}";
    }

    /// <summary>The icons of a row act on that row: select it first.</summary>
    private void SelectRowOf(object sender)
    {
        if (sender is FrameworkElement { DataContext: ResultRow row })
        {
            ResultsList.SelectedItem = row;
        }
    }

    /// <summary>The action icon of a row: opens it, like Enter (also on a row that is not selected).</summary>
    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        SelectRowOf(sender);
        OpenSelected();
    }

    /// <summary>Del on the recent list: forgets the selected item and keeps the selection in place.</summary>
    private void RemoveSelectedFromRecents()
    {
        if (!IsShowingRecents || Selected is not { } resource)
        {
            return;
        }

        var index = ResultsList.SelectedIndex;
        try
        {
            if (!_recents.Remove(resource.Target))
            {
                return;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Recents", "Cannot save the recent items", ex);
            ShowMessage(ex.Message);
            return;
        }

        _log.Info("Action", $"Removed from recents: {resource.Target}");
        RefreshAt(index);
    }

    /// <summary>Rebuilds the list after an item left it, and selects the row now at its place.</summary>
    private void RefreshAt(int index)
    {
        Refresh();
        if (ResultsList.Items.Count > 0)
        {
            ResultsList.SelectedIndex = Math.Min(index, ResultsList.Items.Count - 1);
        }
    }

    private void MoveSelection(int delta) =>
        Select(ListNavigation.Move(ResultsList.SelectedIndex, ResultsList.Items.Count, delta));

    private void Select(int index)
    {
        if (index < 0)
        {
            return;
        }

        ResultsList.SelectedIndex = index;
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    /// <summary>Page Up / Page Down: as many rows as the list shows.</summary>
    private int PageSize()
    {
        // The list scrolls by item, so the height of its viewport is counted in rows.
        var viewer = FindChild<ScrollViewer>(ResultsList);
        return ListNavigation.PageSize(viewer?.ViewportHeight ?? 0);
    }

    private static T? FindChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
            {
                return found;
            }

            if (FindChild<T>(child) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // A row box is open: Enter saves, Esc cancels, every other key is typed in it.
        if (_editingRow is not null)
        {
            if (e.Key == Key.Enter)
            {
                CommitEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                EndEdit();
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Escape)
        {
            // First Esc clears the search (the recent list is shown again), the next one hides.
            if (SearchBox.Text.Length > 0)
            {
                SearchBox.Clear();
            }
            else
            {
                Hide();
            }

            e.Handled = true;
            return;
        }

        // Reached with Tab: Enter opens Settings rather than the selected item.
        if (SettingsButton.IsKeyboardFocused && e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            ShowSettings();
            return;
        }

        switch (e.Key)
        {
            case Key.Enter when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                OpenSelected(asAdministrator: true);
                e.Handled = true;
                break;
            case Key.Enter when Keyboard.Modifiers == ModifierKeys.Control:
                ShowSelectedInFolder();
                e.Handled = true;
                break;
            case Key.Enter when Keyboard.Modifiers == ModifierKeys.None:
                OpenSelected();
                e.Handled = true;
                break;
            // With text selected in the search box, Ctrl+C is left to it and copies that text.
            case Key.C when Keyboard.Modifiers == ModifierKeys.Control
                            && SearchBox.SelectionLength == 0
                            && ResultsList.SelectedItem is not null:
                CopySelected();
                e.Handled = true;
                break;
            case Key.F2 when Keyboard.Modifiers == ModifierKeys.None:
                StartAliasEdit();
                e.Handled = true;
                break;
            case Key.P when Keyboard.Modifiers == ModifierKeys.Control:
                TogglePinSelected();
                e.Handled = true;
                break;
            case Key.H when Keyboard.Modifiers == ModifierKeys.Control:
                HideSelected();
                e.Handled = true;
                break;
            case Key.N when Keyboard.Modifiers == ModifierKeys.Control:
                StartCustomEntry();
                e.Handled = true;
                break;
            case Key.Delete when IsShowingRecents && ResultsList.SelectedItem is not null:
                RemoveSelectedFromRecents();
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
            case Key.PageDown:
                MoveSelection(PageSize());
                e.Handled = true;
                break;
            case Key.PageUp:
                MoveSelection(-PageSize());
                e.Handled = true;
                break;

            // Home and End alone stay with the search box (start / end of the text).
            case Key.Home when Keyboard.Modifiers == ModifierKeys.Control:
                Select(ListNavigation.First(ResultsList.Items.Count));
                e.Handled = true;
                break;
            case Key.End when Keyboard.Modifiers == ModifierKeys.Control:
                Select(ListNavigation.Last(ResultsList.Items.Count));
                e.Handled = true;
                break;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // A double click in the row box selects a word there.
        if (_editingRow is null)
        {
            OpenSelected();
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateActionText();

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => ShowSettings();

    /// <summary>
    /// Shows the launcher, then Settings as a dialog over it (gear button, tray menu, hotkey
    /// problem at start). Closing Settings goes back to the launcher. Returns at once when
    /// Settings is already open.
    /// </summary>
    /// <param name="tab">The tab to show, or null for the first one.</param>
    /// <param name="hotkeyProblem">True when the hotkey could not be registered: Settings explains it.</param>
    public void ShowSettings(string? tab = null, bool hotkeyProblem = false)
    {
        if (_settingsDialog is not null)
        {
            _settingsDialog.Activate();
            return;
        }

        ShowFromOutside();

        var dialog = _createSettings();
        dialog.Owner = this;
        if (tab is not null)
        {
            dialog.ShowTab(tab);
        }

        if (hotkeyProblem)
        {
            dialog.ShowHotkeyProblem();
        }

        _log.Debug("Window", "Settings shown");

        // The launcher hides when it loses the focus; not while its dialog is open.
        _settingsDialog = dialog;
        _showingDialog = true;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            _showingDialog = false;
            _settingsDialog = null;
        }

        Activate();
        SearchBox.Focus();
    }

    /// <summary>
    /// Shows a message owned by the launcher. Without this, the message box would take the focus,
    /// the launcher would hide itself (it hides when deactivated), and the message would end up alone.
    /// </summary>
    private void ShowMessage(string message)
    {
        _showingDialog = true;
        try
        {
            MessageBox.Show(this, message, "coursee");
        }
        finally
        {
            _showingDialog = false;
        }

        Activate();
        SearchBox.Focus();
    }

    /// <summary>The launcher has no title bar: it is moved by dragging its empty area.</summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();

            // Windows lets a window be dragged past the screen edge; the launcher must stay whole.
            WindowPlacer.KeepOnScreen(this);
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_showingDialog)
        {
            return;
        }

        Hide();
    }
}
