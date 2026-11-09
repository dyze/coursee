using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Coursee.Core.Icons;

/// <summary>
/// An SVG made of plain paths, such as the icons of <see cref="IconLibrary"/>: drawn in one colour
/// (the theme's), so a fill, a stroke or a gradient is not kept. An SVG with anything else to draw
/// (shapes, text, images, transforms) is refused: rendering it needs a full SVG library.
/// </summary>
/// <param name="ViewBox">The drawing's area: <c>x y width height</c>.</param>
/// <param name="PathData">The <c>d</c> of every path, joined, in the SVG path syntax.</param>
public sealed record SvgIcon(SvgViewBox ViewBox, string PathData)
{
    private static readonly HashSet<string> Ignored = new(StringComparer.Ordinal)
    {
        "svg", "g", "path", "title", "desc", "defs", "metadata", "style",
        "linearGradient", "radialGradient", "stop", "clipPath", "mask",
    };

    /// <summary>The icon of an SVG text; null when it is not an SVG of plain paths.</summary>
    public static SvgIcon? Read(string svg)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(svg, LoadOptions.None);
        }
        catch (XmlException)
        {
            return null;
        }

        var root = document.Root;
        if (root?.Name.LocalName != "svg" || root.Descendants().Any(IsUnsupported))
        {
            return null;
        }

        var paths = root.Descendants()
            .Where(element => element.Name.LocalName == "path" && !IsInDefinitions(element))
            .Select(element => element.Attribute("d")?.Value.Trim())
            .Where(data => !string.IsNullOrEmpty(data))
            .ToList();
        return paths.Count == 0 || ViewBoxOf(root) is not { } viewBox
            ? null
            : new SvgIcon(viewBox, string.Join(" ", paths));
    }

    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>Something drawn that is not a plain path. An editor's own elements (Inkscape's) draw nothing.</summary>
    private static bool IsUnsupported(XElement element) =>
        element.Name.NamespaceName is "" or SvgNamespace
        && !IsInDefinitions(element)
        && (!Ignored.Contains(element.Name.LocalName) || element.Attribute("transform") is not null);

    private static bool IsInDefinitions(XElement element) =>
        element.Ancestors().Any(ancestor => ancestor.Name.LocalName is "defs" or "clipPath" or "mask");

    /// <summary>The <c>viewBox</c>, else <c>0 0 width height</c>.</summary>
    private static SvgViewBox? ViewBoxOf(XElement root)
    {
        if (root.Attribute("viewBox")?.Value is { } text)
        {
            var parts = text.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 4 && parts.Select(Number).ToArray() is [{ } x, { } y, { } width, { } height] && width > 0 && height > 0
                ? new SvgViewBox(x, y, width, height)
                : null;
        }

        return Number(root.Attribute("width")?.Value) is { } w && Number(root.Attribute("height")?.Value) is { } h && w > 0 && h > 0
            ? new SvgViewBox(0, 0, w, h)
            : null;
    }

    /// <summary>A number, with an optional <c>px</c> unit.</summary>
    private static double? Number(string? text)
    {
        var value = text?.Trim();
        if (value?.EndsWith("px", StringComparison.Ordinal) == true)
        {
            value = value[..^2];
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}

/// <summary>The area an SVG draws in.</summary>
public sealed record SvgViewBox(double X, double Y, double Width, double Height);
