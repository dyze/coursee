using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Coursee.Core.Icons;
using Coursee.Core.Search;
using Coursee.UI.Platform;
using Microsoft.Win32;

namespace Coursee.UI.Views;

/// <summary>What the icon picker chose: one of coursee's icons, a file of the user's, or neither (the entry's own icon back).</summary>
public sealed record IconPick(string? LibraryName, string? FilePath)
{
    public static IconPick Own { get; } = new(null, null);
}

/// <summary>Chooses the icon of an entry (Ctrl+I in the launcher). <see cref="Pick"/> is set when it closes with OK.</summary>
public partial class IconPickerWindow : Window
{
    /// <summary>An icon of the list.</summary>
    public sealed record LibraryIcon(string Name, IconVisual Visual);

    private readonly IReadOnlyList<LibraryIcon> _icons;

    /// <param name="entryName">The entry's name, shown on top.</param>
    /// <param name="current">The icon the entry has; one of coursee's is selected.</param>
    public IconPickerWindow(Window owner, string entryName, EntryIcon? current)
    {
        InitializeComponent();
        Owner = owner;
        EntryText.Text = $"Icon of {entryName}";
        OwnIconButton.IsEnabled = current is not null;
        _icons = IconLibrary.Names
            .Select(name => LibraryIcons.Of(name) is { } visual ? new LibraryIcon(name, visual) : null)
            .OfType<LibraryIcon>()
            .ToList();
        IconList.ItemsSource = _icons;
        if (current is { Kind: EntryIconKind.Library })
        {
            IconList.SelectedItem = _icons.FirstOrDefault(icon => icon.Name == current.Name);
            Loaded += (_, _) => IconList.ScrollIntoView(IconList.SelectedItem);
        }

        Loaded += (_, _) => FilterBox.Focus();
    }

    /// <summary>The choice; null when cancelled.</summary>
    public IconPick? Pick { get; private set; }

    private void Choose(IconPick pick)
    {
        Pick = pick;
        DialogResult = true;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (IconList.SelectedItem is LibraryIcon icon)
        {
            Choose(new IconPick(icon.Name, null));
        }
    }

    private void IconList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OkButton_Click(sender, e);

    private void OwnIconButton_Click(object sender, RoutedEventArgs e) => Choose(IconPick.Own);

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose an icon",
            Filter = "Icons (ico, png, jpg, bmp, gif, exe, dll, svg)|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.exe;*.dll;*.svg|All files|*.*",
        };
        if (dialog.ShowDialog(this) == true)
        {
            Choose(new IconPick(null, dialog.FileName));
        }
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterPlaceholder.Visibility = FilterBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var filter = TextFolding.Fold(FilterBox.Text.Trim());
        var shown = _icons.Where(icon => icon.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        IconList.ItemsSource = shown;
        if (IconList.SelectedItem is null && shown.Count > 0)
        {
            IconList.SelectedIndex = 0;
        }
    }

    /// <summary>Down goes to the icons, at the selected one.</summary>
    private void FilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && IconList.Items.Count > 0)
        {
            IconList.SelectedIndex = Math.Max(IconList.SelectedIndex, 0);
            (IconList.ItemContainerGenerator.ContainerFromIndex(IconList.SelectedIndex) as ListBoxItem)?.Focus();
            e.Handled = true;
        }
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        NativeWindows.RemoveMinimizeMaximizeButtons(new WindowInteropHelper(this).Handle);
}
