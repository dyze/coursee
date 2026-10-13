namespace Coursee.Core.Tests.Entries;

public class EntryFilterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyQuery_MatchesEverything(string? query)
    {
        Assert.True(EntryFilter.Matches(query, "Wiki", "https://wiki"));
    }

    [Fact]
    public void AWord_IsFoundInAnyText_IgnoringCase()
    {
        Assert.True(EntryFilter.Matches("WIKI", "Team wiki", "https://intranet"));
        Assert.True(EntryFilter.Matches("intranet", "Team wiki", "https://intranet"));
        Assert.False(EntryFilter.Matches("mail", "Team wiki", "https://intranet"));
    }

    [Fact]
    public void EveryWord_MustBeFound_InTheSameOrAnotherText()
    {
        Assert.True(EntryFilter.Matches("team intranet", "Team wiki", "https://intranet"));
        Assert.False(EntryFilter.Matches("team mail", "Team wiki", "https://intranet"));
    }

    [Fact]
    public void Accents_AreIgnored()
    {
        Assert.True(EntryFilter.Matches("parametres", "Paramètres"));
        Assert.True(EntryFilter.Matches("événements", "Evenements"));
    }

    [Fact]
    public void NoText_MatchesOnlyAnEmptyQuery()
    {
        Assert.True(EntryFilter.Matches(string.Empty));
        Assert.False(EntryFilter.Matches("wiki"));
    }
}
