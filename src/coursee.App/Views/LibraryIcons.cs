using Coursee.Core.Icons;

namespace Coursee.UI.Views;

/// <summary>coursee's icons (<see cref="IconLibrary"/>) ready to draw, each parsed once. Used on the UI thread.</summary>
public static class LibraryIcons
{
    private static readonly Dictionary<string, IconVisual?> Visuals = new(StringComparer.Ordinal);

    /// <summary>The icon of this name; null for none, or a name this version does not have.</summary>
    public static IconVisual? Of(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (!Visuals.TryGetValue(name, out var visual))
        {
            // Drawn with WPF's even-odd rule, as they were designed.
            visual = IconLibrary.Find(name) is { } icon ? IconVisual.OfSvg(icon, nonZero: false) : null;
            Visuals[name] = visual;
        }

        return visual;
    }
}
