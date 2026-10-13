namespace Coursee.Core.Search;

/// <summary>
/// What raises a search result above its match score, by target (the key of an entry).
/// Every part is optional: without it, results are ranked by match only.
/// </summary>
/// <param name="UsageBonus">Points for an entry opened through coursee (see <c>Frecency.Bonus</c>), added to its match score.</param>
public sealed record SearchBoosts(Func<string, int>? UsageBonus = null)
{
    public static SearchBoosts None { get; } = new();
}
