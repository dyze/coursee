using Coursee.Core.Resources;

namespace Coursee.Core.Entries;

/// <summary>
/// An entry the user hid from results and recents: that exact row, known by its source, name and
/// target (ignoring case), so one of two shortcuts to the same program can be hidden alone.
/// </summary>
public sealed record HiddenEntry(string Source, string Name, string Target)
{
    public static HiddenEntry Of(Resource resource) => new(resource.Source, resource.Name, resource.Target);

    public bool Matches(Resource resource) => Matches(Of(resource));

    public bool Matches(HiddenEntry other) =>
        Same(Source, other.Source) && Same(Name, other.Name) && Same(Target, other.Target);

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
