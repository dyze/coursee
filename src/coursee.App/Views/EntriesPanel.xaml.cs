using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Coursee.Core.Entries;
using Coursee.Core.Logging;
using Coursee.Core.Sources;

namespace Coursee.UI.Views;

/// <summary>
/// Settings > Entries: what the user set on entries, to undo or change it, one list at a time:
/// hidden entries (restore), custom entries (edit, delete), aliases (edit, remove) and pins (unpin).
/// A filter narrows every list. Each change is saved at once.
/// </summary>
public partial class EntriesPanel : UserControl
{
    /// <summary>One list of the panel, shown when its tab in the strip is selected.</summary>
    private sealed record Group(string Title, ItemsControl List, string EmptyText);

    private readonly EntryStore _entries;
    private readonly CustomEntryStore _customEntries;
    private readonly CustomEntryEditor _customEntryEditor;
    private readonly SourceIcons _sourceIcons;
    private readonly ILog _log;
    private readonly Action<string> _showMessage;
    private readonly Group[] _groups;
    private bool _reloading;

    /// <param name="sourceIcons">Names the source of each hidden entry.</param>
    public EntriesPanel(
        EntryStore entries,
        CustomEntryStore customEntries,
        CustomEntryEditor customEntryEditor,
        SourceIcons sourceIcons,
        ILog log,
        Action<string> showMessage)
    {
        _entries = entries;
        _customEntries = customEntries;
        _customEntryEditor = customEntryEditor;
        _sourceIcons = sourceIcons;
        _log = log;
        _showMessage = showMessage;
        InitializeComponent();

        _groups =
        [
            new("Hidden", HiddenList, "None. Ctrl+H in the launcher hides the selected entry."),
            new("Custom", CustomList, "None. Ctrl+N or + in the launcher makes the typed text one."),
            new("Aliases", AliasList, "None. F2 in the launcher gives the selected entry an alias."),
            new("Pins", PinList, "None. Ctrl+P in the launcher pins the selected entry."),
        ];
        Reload();
    }

    /// <summary>Fills every list with what matches the filter, counts them in the strip, and keeps the selected one.</summary>
    private void Reload()
    {
        var filter = FilterBox.Text;
        HiddenList.ItemsSource = _entries.Hidden
            .Select(entry => new HiddenEntryRow(entry, _sourceIcons.LabelOf(entry.Source)))
            .Where(row => EntryFilter.Matches(filter, row.Name, row.Entry.Target, row.SourceLabel))
            .ToList();
        CustomList.ItemsSource = _customEntries.All
            .Where(entry => EntryFilter.Matches(filter, entry.Name, entry.Target))
            .Select(entry => new CustomEntryRow(entry))
            .ToList();
        AliasList.ItemsSource = _entries.Aliases
            .Where(alias => EntryFilter.Matches(filter, alias.Alias, alias.Target))
            .Select(alias => new AliasRow(alias))
            .ToList();
        PinList.ItemsSource = _entries.Pinned.Where(target => EntryFilter.Matches(filter, target)).ToList();

        var selected = Math.Max(GroupList.SelectedIndex, 0);
        _reloading = true;
        GroupList.ItemsSource = _groups.Select(group => $"{group.Title} ({group.List.Items.Count})").ToList();
        GroupList.SelectedIndex = selected;
        _reloading = false;
        ShowSelectedGroup();
    }

    private void ShowSelectedGroup()
    {
        var selected = _groups[Math.Max(GroupList.SelectedIndex, 0)];
        foreach (var group in _groups)
        {
            group.List.Visibility = group == selected ? Visibility.Visible : Visibility.Collapsed;
        }

        EmptyText.Text = string.IsNullOrWhiteSpace(FilterBox.Text) ? selected.EmptyText : "Nothing matches the filter.";
        EmptyText.Visibility = selected.List.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Also raised while InitializeComponent runs, before the groups exist.
        if (!_reloading && _groups is not null)
        {
            ShowSelectedGroup();
        }
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterPlaceholder.Visibility = FilterBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_groups is not null)
        {
            Reload();
        }
    }

    /// <summary>
    /// Runs a change and shows the lists again when it says so (true). A failure to save is logged
    /// and shown, and the lists are shown again as saved.
    /// </summary>
    private void Change(Func<bool> change, string failure)
    {
        try
        {
            if (!change())
            {
                return;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Entries", failure, ex);
            _showMessage(ex.Message);
        }

        Reload();
    }

    private static T RowOf<T>(object sender) => (T)((FrameworkElement)sender).DataContext;

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var entry = RowOf<HiddenEntryRow>(sender).Entry;
        Change(
            () =>
            {
                if (_entries.Restore(entry))
                {
                    _log.Info("Entries", $"Restored: {entry.Name} -> {entry.Target} ({entry.Source})");
                }

                return true;
            },
            "Cannot save the hidden entries");
    }

    private void SaveCustom(CustomEntryRow row) => Change(
        () =>
        {
            CustomEntry? edited;
            try
            {
                edited = _customEntryEditor.Edit(row.Entry, row.Name, row.Target);
            }
            catch (ArgumentException)
            {
                _showMessage("A custom entry needs a target: a URL, folder, file or command.");
                return false;
            }

            if (edited is null)
            {
                _showMessage($"Another custom entry already opens {row.Target.Trim()}.");
                return false;
            }

            _log.Info("Entries", $"Custom entry changed: {row.Entry.Name} -> {row.Entry.Target} is now {edited.Name} -> {edited.Target}");
            return true;
        },
        "Cannot save the custom entries");

    private void SaveCustomButton_Click(object sender, RoutedEventArgs e) => SaveCustom(RowOf<CustomEntryRow>(sender));

    private void CustomBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveCustom(RowOf<CustomEntryRow>(sender));
            e.Handled = true;
        }
    }

    private void DeleteCustomButton_Click(object sender, RoutedEventArgs e)
    {
        var entry = RowOf<CustomEntryRow>(sender).Entry;
        var answer = MessageBox.Show(
            Window.GetWindow(this)!,
            $"Delete the custom entry \"{entry.Name}\"?",
            "coursee",
            MessageBoxButton.YesNo);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        Change(
            () =>
            {
                if (_customEntries.Delete(entry))
                {
                    _log.Info("Entries", $"Custom entry deleted: {entry.Name} -> {entry.Target}");
                }

                return true;
            },
            "Cannot save the custom entries");
    }

    /// <summary>An empty alias removes it; an alias another entry had moves to this one.</summary>
    private void SaveAlias(string target, string alias) => Change(
        () =>
        {
            var cleaned = alias.Trim();
            var movedFrom = _entries.SetAlias(target, cleaned);
            if (cleaned.Length == 0)
            {
                _log.Info("Entries", $"Alias removed: {target}");
            }
            else
            {
                var moved = movedFrom is null ? string.Empty : $" (moved from {movedFrom})";
                _log.Info("Entries", $"Alias set: {cleaned} -> {target}{moved}");
            }

            return true;
        },
        "Cannot save the aliases");

    private void SaveAliasButton_Click(object sender, RoutedEventArgs e)
    {
        var row = RowOf<AliasRow>(sender);
        SaveAlias(row.Target, row.Alias);
    }

    private void AliasBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var row = RowOf<AliasRow>(sender);
            SaveAlias(row.Target, row.Alias);
            e.Handled = true;
        }
    }

    private void RemoveAliasButton_Click(object sender, RoutedEventArgs e) => SaveAlias(RowOf<AliasRow>(sender).Target, string.Empty);

    private void UnpinButton_Click(object sender, RoutedEventArgs e)
    {
        var target = RowOf<string>(sender);
        Change(
            () =>
            {
                if (_entries.IsPinned(target))
                {
                    _entries.TogglePin(target);
                    _log.Info("Entries", $"Unpinned: {target}");
                }

                return true;
            },
            "Cannot save the pinned entries");
    }
}
