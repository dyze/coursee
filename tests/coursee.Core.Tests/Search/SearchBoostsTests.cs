namespace Coursee.Core.Tests.Search;

public class SearchBoostsTests
{
    private static Resource R(string name, string? target = null) =>
        new(name, target ?? @"C:\" + name, ResourceKind.Folder, "test");

    /// <summary>Every entry whose name is in <paramref name="used"/> has the highest usage bonus.</summary>
    private static SearchBoosts Used(params string[] used) =>
        new(UsageBonus: target => used.Any(name => target.EndsWith(@"\" + name)) ? Frecency.MaxBonus : 0);

    private static IEnumerable<string> Names(IEnumerable<Resource> resources) => resources.Select(r => r.Name);

    [Fact]
    public void UsedEntry_RanksFirst_AmongEqualMatches()
    {
        var resources = new[] { R("Docs a"), R("Docs b") };

        var result = SearchEngine.Search(resources, "docs", Used("Docs b"));

        Assert.Equal(new[] { "Docs b", "Docs a" }, Names(result));
    }

    [Fact]
    public void UsedEntry_BeatsTheNextMatchLevel()
    {
        // Word start (80) + bonus beats name start (100).
        var resources = new[] { R("Docs"), R("My docs") };

        var result = SearchEngine.Search(resources, "docs", Used("My docs"));

        Assert.Equal(new[] { "My docs", "Docs" }, Names(result));
    }

    [Fact]
    public void StrongNameMatch_BeatsAWeakMatchOfAUsedEntry()
    {
        // Name start (100) beats letters in order (10) + bonus.
        var resources = new[] { R("Fax invoice"), R("Firefox") };

        var result = SearchEngine.Search(resources, "fi", Used("Fax invoice"));

        Assert.Equal(new[] { "Firefox", "Fax invoice" }, Names(result));
    }

    [Fact]
    public void NameMatch_BeatsATargetMatchOfAUsedEntry()
    {
        // Name contains (60) beats target contains (30) + bonus.
        var resources = new[] { R("Notes", @"D:\alpha\notes"), R("Subalpha") };

        var result = SearchEngine.Search(resources, "alpha", Used("notes"));

        Assert.Equal(new[] { "Subalpha", "Notes" }, Names(result));
    }

    [Fact]
    public void Bonus_DoesNotMakeANonMatchingEntryMatch()
    {
        var result = SearchEngine.Search([R("Docs")], "zzz", Used("Docs"));

        Assert.Empty(result);
    }

    [Fact]
    public void Bonus_IsAddedOnce_NotPerWord()
    {
        // 100 + 80 beats 60 + 80 + bonus; with a bonus per word, the used entry would win.
        var resources = new[] { R("Prepaid salary"), R("Report sales") };

        var result = SearchEngine.Search(resources, "rep sal", Used("Prepaid salary"));

        Assert.Equal(new[] { "Report sales", "Prepaid salary" }, Names(result));
    }
}
