using Coursee.Core.Resources;

namespace Coursee.Core.Search;

/// <summary>
/// In-memory search over resources. Every whitespace-separated token of the query
/// must match; a higher score means a better match. Targets only, no content search.
/// Matching ignores case and accents.
/// </summary>
public static class SearchEngine
{
    private const int NamePrefix = 100;
    private const int NameWordStart = 80;
    private const int NameContains = 60;
    private const int TargetContains = 30;
    private const int NameSubsequence = 10;

    public static IReadOnlyList<Resource> Search(IEnumerable<Resource> resources, string query, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var tokens = TextFolding.Fold(query).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return resources
            .Select(resource => (Resource: resource, Score: Score(resource, tokens)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Resource.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.Resource)
            .ToList();
    }

    private static int Score(Resource resource, string[] tokens)
    {
        var name = TextFolding.Fold(resource.Name);
        var target = TextFolding.Fold(resource.Target);
        var total = 0;
        foreach (var token in tokens)
        {
            var score = ScoreToken(name, target, token);
            if (score == 0)
            {
                return 0;
            }

            total += score;
        }

        return total;
    }

    private static int ScoreToken(string name, string target, string token)
    {
        var index = name.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index == 0)
        {
            return NamePrefix;
        }

        if (index > 0)
        {
            return IsWordStart(name, index) ? NameWordStart : NameContains;
        }

        if (target.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            return TargetContains;
        }

        return IsSubsequence(name, token) ? NameSubsequence : 0;
    }

    private static bool IsWordStart(string text, int index) =>
        index == 0 || !char.IsLetterOrDigit(text[index - 1]);

    private static bool IsSubsequence(string text, string token)
    {
        var position = 0;
        foreach (var c in text)
        {
            if (char.ToUpperInvariant(c) == char.ToUpperInvariant(token[position]))
            {
                position++;
                if (position == token.Length)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
