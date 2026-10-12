namespace Coursee.Core.Search;

/// <summary>Moves the selection in the result list (Up / Down, Page Up / Page Down, first / last).</summary>
public static class ListNavigation
{
    /// <summary>
    /// The index selected after moving <paramref name="step"/> rows from <paramref name="current"/>
    /// (negative: up), stopping at the first and last row. -1 when the list is empty; with
    /// nothing selected, the move starts from the first row.
    /// </summary>
    public static int Move(int current, int count, int step)
    {
        if (count <= 0)
        {
            return -1;
        }

        var from = Math.Clamp(current, 0, count - 1);
        return (int)Math.Clamp((long)from + step, 0, count - 1);
    }

    /// <summary>The first row (Ctrl+Home); -1 when the list is empty.</summary>
    public static int First(int count) => count > 0 ? 0 : -1;

    /// <summary>The last row (Ctrl+End); -1 when the list is empty.</summary>
    public static int Last(int count) => count - 1;

    /// <summary>
    /// How many rows Page Up / Page Down move: the rows that fit in the list, at least one.
    /// </summary>
    /// <param name="visibleRows">The height of the list in rows, possibly fractional or unknown (0).</param>
    public static int PageSize(double visibleRows) =>
        double.IsFinite(visibleRows) ? Math.Max(1, (int)Math.Floor(visibleRows)) : 1;
}
