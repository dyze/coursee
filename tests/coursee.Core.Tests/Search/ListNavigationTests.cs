namespace Coursee.Core.Tests.Search;

public class ListNavigationTests
{
    [Theory]
    [InlineData(0, 10, 1, 1)]
    [InlineData(5, 10, -1, 4)]
    [InlineData(2, 10, 5, 7)]
    [InlineData(7, 10, -5, 2)]
    public void Move_MovesByTheStep(int current, int count, int step, int expected) =>
        Assert.Equal(expected, ListNavigation.Move(current, count, step));

    [Theory]
    [InlineData(8, 10, 5, 9)]
    [InlineData(2, 10, -5, 0)]
    [InlineData(9, 10, 1, 9)]
    [InlineData(0, 10, -1, 0)]
    [InlineData(3, 10, int.MaxValue, 9)]
    [InlineData(3, 10, int.MinValue, 0)]
    public void Move_StopsAtTheEnds(int current, int count, int step, int expected) =>
        Assert.Equal(expected, ListNavigation.Move(current, count, step));

    [Fact]
    public void Move_EmptyList_SelectsNothing() =>
        Assert.Equal(-1, ListNavigation.Move(0, 0, 1));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(-1, 0)]
    public void Move_NothingSelected_StartsFromTheFirstRow(int step, int expected) =>
        Assert.Equal(expected, ListNavigation.Move(-1, 10, step));

    [Fact]
    public void Move_SelectionPastTheEnd_StartsFromTheLastRow() =>
        Assert.Equal(3, ListNavigation.Move(12, 5, -1));

    [Fact]
    public void FirstAndLast()
    {
        Assert.Equal(0, ListNavigation.First(4));
        Assert.Equal(3, ListNavigation.Last(4));
        Assert.Equal(-1, ListNavigation.First(0));
        Assert.Equal(-1, ListNavigation.Last(0));
    }

    [Theory]
    [InlineData(8.0, 8)]
    [InlineData(8.7, 8)]
    [InlineData(0.4, 1)]
    [InlineData(0.0, 1)]
    [InlineData(double.NaN, 1)]
    [InlineData(double.PositiveInfinity, 1)]
    public void PageSize_IsTheWholeRowsThatFit_AtLeastOne(double visibleRows, int expected) =>
        Assert.Equal(expected, ListNavigation.PageSize(visibleRows));
}
