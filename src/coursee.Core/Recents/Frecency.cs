namespace Coursee.Core.Recents;

/// <summary>
/// How much an entry was used through coursee, from the times it was opened: recent opens count
/// more than old ones, and every open adds up. Orders the recent list and gives search results
/// a bonus.
/// </summary>
public static class Frecency
{
    /// <summary>The most a search result gains from being used: a strong name match still beats a weak match.</summary>
    public const int MaxBonus = 25;

    /// <summary>How many open times are kept for each entry.</summary>
    public const int MaxOpens = 10;

    /// <summary>The points of each open, by its age.</summary>
    public static int Points(DateTimeOffset opened, DateTimeOffset now)
    {
        var age = now - opened;
        if (age < TimeSpan.FromDays(1))
        {
            return 10;
        }

        if (age < TimeSpan.FromDays(7))
        {
            return 5;
        }

        return age < TimeSpan.FromDays(30) ? 2 : 1;
    }

    /// <summary>The total of the points of every open: orders the recent list.</summary>
    public static int Score(IEnumerable<DateTimeOffset> opens, DateTimeOffset now) =>
        opens.Sum(opened => Points(opened, now));

    /// <summary>The score, capped at <see cref="MaxBonus"/>: added to a search result's match score.</summary>
    public static int Bonus(IEnumerable<DateTimeOffset> opens, DateTimeOffset now) =>
        Math.Min(MaxBonus, Score(opens, now));
}
