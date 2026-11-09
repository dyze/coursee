using System.Windows;
using System.Windows.Media;
using Coursee.Core.Icons;

namespace Coursee.UI.Views;

/// <summary>
/// An icon ready to draw (<see cref="EntryIconView"/>): an image, or a path drawn in the colour of
/// the text around it, on its view box. Frozen: shared by every row that shows it.
/// </summary>
public sealed record IconVisual(ImageSource? Image, Geometry? Geometry, Rect ViewBox)
{
    public static IconVisual OfImage(ImageSource image) => new(image, null, Rect.Empty);

    /// <summary>The visual of an SVG; null when WPF cannot read its path data.</summary>
    /// <param name="nonZero">
    /// Fills by SVG's default rule (nonzero), for the user's SVGs. coursee's own icons are drawn
    /// with WPF's (even-odd), as they were designed.
    /// </param>
    public static IconVisual? OfSvg(SvgIcon icon, bool nonZero)
    {
        try
        {
            var geometry = Geometry.Parse((nonZero ? "F1 " : string.Empty) + icon.PathData);
            geometry.Freeze();
            var box = icon.ViewBox;
            return new IconVisual(null, geometry, new Rect(box.X, box.Y, box.Width, box.Height));
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return null;
        }
    }
}
