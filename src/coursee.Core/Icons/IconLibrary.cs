namespace Coursee.Core.Icons;

/// <summary>
/// The icons shipped with coursee, by name (<c>game</c>, <c>code</c>, <c>star</c>…), that an entry
/// can be given: the SVGs of <c>Icons/Svg</c>, embedded, read once.
/// </summary>
public static class IconLibrary
{
    private const string Prefix = "icons/";

    private static readonly Lazy<IReadOnlyDictionary<string, SvgIcon>> Icons = new(Read);

    /// <summary>Every icon's name, in alphabetical order.</summary>
    public static IReadOnlyList<string> Names { get; } = Icons.Value.Keys.Order(StringComparer.Ordinal).ToList();

    /// <summary>The icon of this name; null when there is none (a name from a newer version, say).</summary>
    public static SvgIcon? Find(string name) => Icons.Value.GetValueOrDefault(name);

    private static Dictionary<string, SvgIcon> Read()
    {
        var assembly = typeof(IconLibrary).Assembly;
        var icons = new Dictionary<string, SvgIcon>(StringComparer.Ordinal);
        foreach (var resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith(Prefix, StringComparison.Ordinal)))
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
            if (SvgIcon.Read(reader.ReadToEnd()) is { } icon)
            {
                icons[Path.GetFileNameWithoutExtension(resource[Prefix.Length..])] = icon;
            }
        }

        return icons;
    }
}
