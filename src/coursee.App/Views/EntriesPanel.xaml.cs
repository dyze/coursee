using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Coursee.Core.Entries;
using Coursee.Core.Logging;
using Coursee.Core.Sources;

namespace Coursee.UI.Views;

/// <summary>
/// Settings > Entries: what the user set on entries, to undo or change it. Hidden entries (restore),
/// custom entries (edit, delete), aliases (edit, remove) and pins (unpin). Each change is saved at once.
/// </summary>
public partial class EntriesPanel : UserControl
{
    private readonly EntryStore _entries;
    private readonly CustomEntryStore _customEntries;
    private readonly CustomEntryEditor _customEntryEditor;
    private readonly SourceIcons _sourceIcons;
    private readonly ILog _log;
    private readonly Action<string> _showMessage;

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
        Reload();
    }

    private void Reload()
    {
        Show(HiddenList, NoHiddenText, _entries.Hidden.Select(entry => new HiddenEntryRow(entry, _sourceIcons.LabelOf(entry.Source))).ToList());
        Show(CustomList, NoCustomText, _customEntries.All.Select(entry => new CustomEntryRow(entry)).ToList());
        Show(AliasList, NoAliasesText, _entries.Aliases.Select(alias => new AliasRow(alias)).ToList());
        Show(PinList, NoPinsText, _entries.Pinned);
    }

    private static void Show<T>(ItemsControl list, UIElement emptyText, IReadOnlyList<T> items)
    {
        list.ItemsSource = items;
        emptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
