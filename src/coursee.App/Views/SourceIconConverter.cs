using System.Globalization;
using System.Windows.Data;
using Coursee.Core.Sources;

namespace Coursee.UI.Views;

/// <summary>
/// From a source name (<c>Resource.Source</c>): the glyph of that source, or with the parameter
/// <c>Label</c>, its name as shown to the user (the tooltip of the glyph).
/// </summary>
public sealed class SourceIconConverter(SourceIcons icons) : IValueConverter
{
    public const string LabelParameter = "Label";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string source)
        {
            return string.Empty;
        }

        return Equals(parameter, LabelParameter) ? icons.LabelOf(source) : icons.IconOf(source);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
