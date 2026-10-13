namespace Coursee.Core.Tests.Recents;

public class FrecencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 10)]
    [InlineData(0.9, 10)]
    [InlineData(1, 5)]
    [InlineData(6, 5)]
    [InlineData(7, 2)]
    [InlineData(29, 2)]
    [InlineData(30, 1)]
    [InlineData(400, 1)]
    public void Points_DependOnTheAgeOfTheOpen(double days, int expected)
    {
        Assert.Equal(expected, Frecency.Points(Now.AddDays(-days), Now));
    }

    [Fact]
    public void Score_AddsUpEveryOpen()
    {
        var opens = new[] { Now.AddHours(-1), Now.AddDays(-3), Now.AddDays(-60) };

        Assert.Equal(10 + 5 + 1, Frecency.Score(opens, Now));
    }

    [Fact]
    public void Bonus_IsCapped()
    {
        var opens = Enumerable.Range(0, 5).Select(i => Now.AddMinutes(-i));

        Assert.Equal(Frecency.MaxBonus, Frecency.Bonus(opens, Now));
    }

    [Fact]
    public void Bonus_OfNoOpen_IsZero()
    {
        Assert.Equal(0, Frecency.Bonus([], Now));
    }
}
