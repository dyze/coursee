using Coursee.Core.Search;

namespace Coursee.Core.Entries;

/// <summary>The filter of the lists in Settings > Entries.</summary>
public static class EntryFilter
{
    /// <summary>
    /// True when every word of <paramref name="query"/> is found in one of <paramref name="texts"/>
    /// (name, target, alias...), ignoring case and accents. An empty query matches everything.
    /// </summary>
    public static bool Matches(string? query, params string[] texts)
    {
        var words = TextFolding.Fold(query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var folded = texts.Select(TextFolding.Fold).ToList();
        return words.All(word => folded.Any(text => text.Contains(word, StringComparison.OrdinalIgnoreCase)));
    }
}
