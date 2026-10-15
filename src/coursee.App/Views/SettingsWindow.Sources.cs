using System.Windows;
using System.Windows.Controls;
using Coursee.Core.Logging;
using Coursee.Core.Settings;
using Coursee.Core.Sources;

namespace Coursee.UI.Views;

/// <summary>Settings > Sources: a checkbox per source, grouped (see <see cref="SourceGroups"/>), and the Add folder button.</summary>
public partial class SettingsWindow
{
    /// <summary>A group's checkbox, and the checkboxes of its sources by name.</summary>
    private sealed record GroupRows(CheckBox Header, Dictionary<string, CheckBox> Members);

    private readonly Dictionary<SourceGroup, GroupRows> _groups = [];

    /// <summary>
    /// Fills the Sources tab: a checkbox per source that can be switched off, with a remove button
    /// for the user's folders. The sources of a group come together, at the place of the first one,
    /// indented under a checkbox that switches them all. The Add folder button is on the row of the
    /// Folders group, or alone when there is none. Called again when the sources change.
    /// </summary>
    private void ShowSources()
    {
        var switchable = _sources.All.Where(s => s.CanBeDisabled).ToList();
        var groupOf = switchable.ToDictionary(
            s => s.Name,
            s => SourceGroups.Of(s.Name, _sources.IsQuickAccess(s.Name), _sources.CanRemove(s.Name)),
            StringComparer.OrdinalIgnoreCase);
        _groups.Clear();
        _catalog = new SettingsCatalog(_sources.All, groupOf.Values.OfType<SourceGroup>().Distinct());

        // Only the section title stays.
        var rows = SourcesSection.Children.OfType<UIElement>().Skip(1).ToList();
        rows.ForEach(SourcesSection.Children.Remove);
        (_addFolderButton.Parent as Panel)?.Children.Remove(_addFolderButton);

        StackPanel? foldersRow = null;
        foreach (var source in switchable)
        {
            if (groupOf[source.Name] is not { } group)
            {
                SourcesSection.Children.Add(SourceRow(source, members: null));
                continue;
            }

            if (_groups.ContainsKey(group))
            {
                continue;
            }

            var groupRows = new GroupRows(GroupRow(group), new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase));
            _groups[group] = groupRows;
            if (group == SourceGroups.Folders)
            {
                // The group's checkbox and the button that adds to it, on one row.
                foldersRow = new StackPanel { Orientation = Orientation.Horizontal, Tag = groupRows.Header.Tag, Margin = groupRows.Header.Margin };
                groupRows.Header.Margin = new Thickness(0);
                groupRows.Header.VerticalAlignment = VerticalAlignment.Center;
                foldersRow.Children.Add(groupRows.Header);
                SourcesSection.Children.Add(foldersRow);
            }
            else
            {
                SourcesSection.Children.Add(groupRows.Header);
            }

            foreach (var member in switchable.Where(s => groupOf[s.Name] == group))
            {
                SourcesSection.Children.Add(SourceRow(member, groupRows.Members));
            }
        }

        // Next to the group of the folders it adds to, else on its own row.
        if (foldersRow is not null)
        {
            _addFolderButton.Margin = new Thickness(10, 0, 0, 0);
            foldersRow.Children.Add(_addFolderButton);
        }
        else
        {
            _addFolderButton.Margin = new Thickness(0, 14, 0, 0);
            SourcesSection.Children.Add(_addFolderButton);
        }

        ShowGroupStates();

        if (IsLoaded)
        {
            UpdateSections();
        }
    }

    /// <summary>The checkbox of a group: ticked when all its sources are on, filled when some are.</summary>
    private CheckBox GroupRow(SourceGroup group)
    {
        var checkBox = new CheckBox
        {
            Content = SourceLabel(group.Icon, group.Name),
            Tag = SettingsCatalog.GroupId(group.Name),
            Margin = new Thickness(0, 10, 0, 0),
            ToolTip = group.ToolTip,
        };
        checkBox.Click += (_, _) => GroupCheckBox_Click(group, checkBox);
        return checkBox;
    }

    private void ShowGroupStates()
    {
        foreach (var (header, members) in _groups.Values)
        {
            header.IsChecked = _settings.AreSourcesEnabled(members.Keys);
        }
    }

    /// <param name="members">The checkboxes of its group, when it has one: the row is indented under it.</param>
    private StackPanel SourceRow(IResourceSource source, Dictionary<string, CheckBox>? members)
    {
        var name = source.Name;
        var inGroup = members is not null;
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Tag = SettingsCatalog.SourceId(name),
            Margin = new Thickness(inGroup ? 28 : 0, inGroup ? 6 : 10, 0, 0),
        };

        var checkBox = new CheckBox
        {
            Content = SourceLabel(source.Icon, source.DisplayName),
            IsChecked = _settings.IsSourceEnabled(name),
            VerticalAlignment = VerticalAlignment.Center,
        };
        checkBox.Click += (_, _) => SourceCheckBox_Click(checkBox, name);
        row.Children.Add(checkBox);
        members?.Add(name, checkBox);

        if (_sources.CanRemove(name))
        {
            var remove = new Button
            {
                Content = "", // Cancel
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

        ShowGroupStates();
    }

    /// <summary>Switches every source of a group; a partly ticked group is switched on.</summary>
    private void GroupCheckBox_Click(SourceGroup group, CheckBox checkBox)
    {
        var wanted = checkBox.IsChecked == true;
        var members = _groups[group].Members;

        try
        {
            _settings.SetSourcesEnabled(members.Keys, wanted);
            _log.Info("Settings", $"{group.Name} sources {(wanted ? "on" : "off")}");
            _onSourcesChanged();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _log.Error("Settings", $"Cannot change the {group.Name} sources", ex);
            ShowMessage(ex.Message);
        }

        foreach (var (name, child) in members)
        {
            child.IsChecked = _settings.IsSourceEnabled(name);
        }

        ShowGroupStates();
    }
}
