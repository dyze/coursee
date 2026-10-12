using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Coursee.Core;
using Coursee.Core.Logging;
using Coursee.Core.Recents;
using Coursee.Core.Resources;
using Coursee.Core.Search;
using Coursee.Core.Settings;
using Coursee.Core.Sources;

namespace Coursee.UI.Views;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<IResourceSource> _sources;
    private readonly RecentStore _recents;
    private readonly SettingsStore _settings;
    private readonly ILog _log;
    private readonly Action _openSettings;
    private readonly SourceMonitor _monitor;
    private IReadOnlyList<Resource> _all = [];
    private bool _showingDialog;
    private bool _refreshing;
    private bool _refreshAgain;

    /// <param name="openSettings">Opens the Settings window (gear button).</param>
    public MainWindow(
        IReadOnlyList<IResourceSource> sources,
        RecentStore recents,
        SettingsStore settings,
        ILog log,
        Action openSettings)
    {
        _log = log;
        _monitor = new SourceMonitor(_log);
        _sources = sources;
        _recents = recents;
        _settings = settings;
        _openSettings = openSettings;
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

    /// <summary>Shows the launcher when asked by another copy of coursee (never hides it).</summary>
    public void ShowFromOutside()
    {
        if (IsVisible)
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
        if (IsVisible && !string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            Refresh(keepSelection: true);
        }
    }

    /// <param name="keepSelection">True when the list is rebuilt under the user (background refresh):
    /// the selected row stays selected. Otherwise the best match is selected.</param>
    private void Refresh(bool keepSelection = false)
    {
        var selectedTarget = keepSelection ? (ResultsList.SelectedItem as Resource)?.Target : null;
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
            var keep = selectedTarget is null
                ? -1
                : items.ToList().FindIndex(r => string.Equals(r.Target, selectedTarget, StringComparison.OrdinalIgnoreCase));
            ResultsList.SelectedIndex = Math.Max(keep, 0);
        }

        UpdateActionText();
    }

    private void UpdateActionText()
    {
        ActionText.Text = ResultsList.SelectedItem switch
        {
            Resource resource => ResourceActions.Hint(resource, inRecents: IsShowingRecents),
            _ => string.IsNullOrWhiteSpace(SearchBox.Text)
                ? "Type to search, or type a path, URL or command"
                : "No match",
        };
    }

    /// <summary>The recent list is shown (search box empty), so Del can remove the selected item.</summary>
    private bool IsShowingRecents => string.IsNullOrWhiteSpace(SearchBox.Text);

    private const int ErrorCancelled = 1223;

    private static void Launch(Resource resource, bool asAdministrator)
    {
        ProcessStartInfo start;
        if (resource.Kind == ResourceKind.Command)
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
        if (ResultsList.SelectedItem is not Resource resource)
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
        catch (System.ComponentModel.Win32Exception ex) when (asAdministrator && ex.NativeErrorCode == ErrorCancelled)
        {
            // The user said no to the Windows confirmation: nothing to report.
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
        if (ResultsList.SelectedItem is not Resource resource || !ResourceActions.CanShowInFolder(resource))
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

    /// <summary>Del on the recent list: forgets the selected item and keeps the selection in place.</summary>
    private void RemoveSelectedFromRecents()
    {
        if (!IsShowingRecents || ResultsList.SelectedItem is not Resource resource)
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
        Refresh();
        if (ResultsList.Items.Count > 0)
        {
            ResultsList.SelectedIndex = Math.Min(index, ResultsList.Items.Count - 1);
        }
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
            Hide();
            e.Handled = true;
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
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Refresh();

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();

    private void ResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateActionText();

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _openSettings();
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

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_showingDialog)
        {
            return;
        }

        Hide();
    }
}
