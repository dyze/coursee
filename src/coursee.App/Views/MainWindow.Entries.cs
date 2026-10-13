using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Coursee.Core.Logging;
using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>The launcher's actions on what the user sets on entries: pins, hidden entries, aliases, custom entries.</summary>
public partial class MainWindow
{
    /// <summary>Ctrl+P or the pin icon: pins or unpins the selected entry. The list is reordered, the entry stays selected.</summary>
    private void TogglePinSelected()
    {
        if (Selected is not { } resource || !ResourceActions.CanPin(resource))
        {
            return;
        }

        bool pinned;
        try
        {
            pinned = _entries.TogglePin(resource.Target);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Entries", "Cannot save the pinned entries", ex);
            ShowMessage(ex.Message);
            return;
        }

        _log.Info("Action", $"{(pinned ? "Pinned" : "Unpinned")}: {resource.Target} ({resource.Source})");
        Refresh(keepSelection: true);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        SelectRowOf(sender);
        TogglePinSelected();
        SearchBox.Focus();
    }

    /// <summary>
    /// Ctrl+H or the hide icon: hides the selected entry from results and recents (restored in
    /// Settings > Entries). The selection stays at the same place in the list.
    /// </summary>
    private void HideSelected()
    {
        if (Selected is not { } resource || !ResourceActions.CanHide(resource))
        {
            return;
        }

        var index = ResultsList.SelectedIndex;
        try
        {
            if (!_entries.Hide(resource))
            {
                return;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Entries", "Cannot save the hidden entries", ex);
            ShowMessage(ex.Message);
            return;
        }

        _log.Info("Action", $"Hidden: {resource.Name} -> {resource.Target} ({resource.Source})");
        RefreshAt(index);
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        SelectRowOf(sender);
        HideSelected();
        SearchBox.Focus();
    }

    /// <summary>F2 or the alias icon: opens the row's box on the selected row, filled with its alias.</summary>
    private void StartAliasEdit()
    {
        if (ResultsList.SelectedItem is ResultRow { CanSetAlias: true } row)
        {
            StartEdit(row, RowEdit.Alias, row.Alias ?? string.Empty);
        }
    }

    /// <summary>
    /// Ctrl+N or the + button, when nothing matched: opens the row's box on the typed text, to name
    /// the custom entry it becomes (the typed text by default).
    /// </summary>
    private void StartCustomEntry()
    {
        if (ResultsList.SelectedItem is ResultRow row && ResourceActions.CanMakeCustomEntry(row.Resource))
        {
            StartEdit(row, RowEdit.CustomEntryName, row.Resource.Name);
        }
    }

    private void StartEdit(ResultRow row, RowEdit edit, string text)
    {
        _editingRow = row;
        row.Edit = edit;

        // The box exists once the row is laid out again.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_editingRow != row || EditBoxOf(row) is not { } box)
            {
                return;
            }

            box.Text = text;
            box.SelectAll();
            box.Focus();
        });
    }

    private TextBox? EditBoxOf(ResultRow row) =>
        ResultsList.ItemContainerGenerator.ContainerFromItem(row) is ListBoxItem item
            ? FindChild<TextBox>(item)
            : null;

    /// <summary>Enter in the row's box: saves what it was opened for, then closes it.</summary>
    private void CommitEdit()
    {
        if (_editingRow is not { } row)
        {
            return;
        }

        var text = (EditBoxOf(row)?.Text ?? string.Empty).Trim();
        var edit = row.Edit;
        try
        {
            if (edit == RowEdit.CustomEntryName)
            {
                SaveCustomEntry(row.Resource, text);
            }
            else
            {
                SaveAlias(row.Resource.Target, text);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Entries", edit == RowEdit.CustomEntryName ? "Cannot save the custom entries" : "Cannot save the aliases", ex);
            EndEdit();
            ShowMessage(ex.Message);
            return;
        }

        EndEdit();

        // The new entry is listed once the sources are read again.
        if (edit == RowEdit.CustomEntryName)
        {
            RefreshResources();
        }
    }

    /// <summary>An empty alias removes it.</summary>
    private void SaveAlias(string target, string alias)
    {
        var movedFrom = _entries.SetAlias(target, alias);
        if (alias.Length == 0)
        {
            _log.Info("Action", $"Alias removed: {target}");
        }
        else
        {
            var moved = movedFrom is null ? string.Empty : $" (moved from {movedFrom})";
            _log.Info("Action", $"Alias set: {alias} -> {target}{moved}");
        }
    }

    private void SaveCustomEntry(Resource typed, string name)
    {
        var entry = _customEntries.Create(typed, name);
        _log.Info("Action", $"Custom entry created: {entry.Name} -> {entry.Target}");
    }

    /// <summary>Closes the row's box (Esc, or the box lost the focus) and goes back to the search box.</summary>
    private void EndEdit()
    {
        if (_editingRow is null)
        {
            return;
        }

        // Cleared first: moving the focus back makes the box lose it, which ends the edit again.
        _editingRow.Edit = RowEdit.None;
        _editingRow = null;
        SearchBox.Focus();
        Refresh(keepSelection: true);
    }

    private void AliasButton_Click(object sender, RoutedEventArgs e)
    {
        SelectRowOf(sender);
        StartAliasEdit();
    }

    /// <summary>The + button, shown when nothing matches: the only row is the typed text.</summary>
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        ResultsList.SelectedIndex = 0;
        StartCustomEntry();
    }

    private void EditBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => EndEdit();
}
