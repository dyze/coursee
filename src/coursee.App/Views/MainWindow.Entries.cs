using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>The launcher's actions on what the user sets on entries: pins, hidden entries, aliases.</summary>
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

    /// <summary>F2 or the alias icon: opens the alias box on the selected row, filled with its alias.</summary>
    private void StartAliasEdit()
    {
        if (ResultsList.SelectedItem is not ResultRow row || !row.CanSetAlias)
        {
            return;
        }

        _aliasRow = row;
        row.IsEditingAlias = true;

        // The box exists once the row is laid out again.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_aliasRow != row || AliasBoxOf(row) is not { } box)
            {
                return;
            }

            box.Text = row.Alias ?? string.Empty;
            box.SelectAll();
            box.Focus();
        });
    }

    private TextBox? AliasBoxOf(ResultRow row) =>
        ResultsList.ItemContainerGenerator.ContainerFromItem(row) is ListBoxItem item
            ? FindChild<TextBox>(item)
            : null;

    /// <summary>Enter in the alias box: saves the alias (an empty one removes it), then closes the box.</summary>
    private void CommitAlias()
    {
        if (_aliasRow is not { } row)
        {
            return;
        }

        var alias = (AliasBoxOf(row)?.Text ?? string.Empty).Trim();
        var target = row.Resource.Target;
        try
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
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Entries", "Cannot save the aliases", ex);
            EndAliasEdit();
            ShowMessage(ex.Message);
            return;
        }

        EndAliasEdit();
    }

    /// <summary>Closes the alias box (Esc, or the box lost the focus) and goes back to the search box.</summary>
    private void EndAliasEdit()
    {
        if (_aliasRow is null)
        {
            return;
        }

        // Cleared first: moving the focus back makes the box lose it, which ends the edit again.
        _aliasRow.IsEditingAlias = false;
        _aliasRow = null;
        SearchBox.Focus();
        Refresh(keepSelection: true);
    }

    private void AliasButton_Click(object sender, RoutedEventArgs e)
    {
        SelectRowOf(sender);
        StartAliasEdit();
    }

    private void AliasBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => EndAliasEdit();
}
