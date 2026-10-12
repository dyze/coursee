using System.Globalization;
using System.Windows.Data;
using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>The Segoe Fluent Icons glyph that shows a <see cref="ResourceAction"/> in the result list.</summary>
public sealed class ActionGlyphConverter : IValueConverter
{
    public static string Glyph(ResourceAction action) => action switch
    {
        ResourceAction.OpenFile => "",      // Document
        ResourceAction.OpenFolder => "",    // Folder
        ResourceAction.OpenUrl => "",       // Globe
        ResourceAction.OpenInBrowser => "", // OpenInNewWindow
        ResourceAction.RunCommand => "",    // CommandPrompt
        _ => "",                            // MapDrive
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ResourceAction action ? Glyph(action) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
