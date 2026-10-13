namespace Coursee.Core.Search;

/// <summary>
/// What raises a search result above its match score, by target (the key of an entry).
/// Every part is optional: without it, results are ranked by match only.
/// </summary>
/// <param name="UsageBonus">Points for an entry opened through coursee (see <c>Frecency.Bonus</c>), added to its match score.</param>
/// <param name="IsPinned">True for a pinned entry: when it matches, it is listed before the entries that are not pinned.</param>
public sealed record SearchBoosts(
    Func<string, int>? UsageBonus = null,
    Func<string, bool>? IsPinned = null)
{
    public static SearchBoosts None { get; } = new();
}
