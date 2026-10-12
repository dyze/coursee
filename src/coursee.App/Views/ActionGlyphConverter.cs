using System.Globalization;
using System.Windows.Data;
using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>The Segoe Fluent Icons glyph that shows a <see cref="ResourceAction"/> in the result list.</summary>
public sealed class ActionGlyphConverter : IValueConverter
{
    public static string Glyph(ResourceAction action) => action switch
    {
        ResourceAction.OpenFile => "\uE8A5",      // Document
        ResourceAction.OpenFolder => "\uE8B7",    // Folder
        ResourceAction.OpenUrl => "\uE774",       // Globe
        ResourceAction.OpenInBrowser => "\uE8A7", // OpenInNewWindow
        ResourceAction.RunCommand => "\uE756",    // CommandPrompt
        ResourceAction.OpenSetting => "\uE713",   // Setting
        _ => "\uE8CE",                            // MapDrive
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ResourceAction action ? Glyph(action) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
