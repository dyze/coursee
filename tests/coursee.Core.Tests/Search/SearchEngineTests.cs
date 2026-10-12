namespace Coursee.Core.Tests.Search;

public class SearchEngineTests
{
    private static Resource R(string name, string target = @"C:\data") =>
        new(name, target, ResourceKind.Folder, "test");

    [Fact]
    public void EmptyQuery_ReturnsNothing()
    {
        var result = SearchEngine.Search([R("Docs")], "  ");

        Assert.Empty(result);
    }

    [Fact]
    public void NoMatch_ReturnsNothing()
    {
        var result = SearchEngine.Search([R("Docs")], "zzz");

        Assert.Empty(result);
    }

    [Fact]
    public void Prefix_RanksBeforeWordStart_BeforeContains()
    {
        var resources = new[] { R("Redocs"), R("Project Docs"), R("Docs") };

        var result = SearchEngine.Search(resources, "docs");

        Assert.Equal(new[] { "Docs", "Project Docs", "Redocs" }, result.Select(r => r.Name));
    }

    [Fact]
    public void AllTokensMustMatch()
    {
        var resources = new[] { R("Project Docs"), R("Docs") };

        var result = SearchEngine.Search(resources, "proj docs");

        Assert.Equal(new[] { "Project Docs" }, result.Select(r => r.Name));
    }

    [Fact]
    public void MatchesTarget_WhenNameDoesNot()
    {
        var resources = new[] { R("Notes", @"D:\work\alpha"), R("Other", @"D:\misc") };

        var result = SearchEngine.Search(resources, "alpha");

        Assert.Equal(new[] { "Notes" }, result.Select(r => r.Name));
    }

    [Fact]
    public void SearchIsCaseInsensitive()
    {
        var result = SearchEngine.Search([R("Photos")], "PHO");

        Assert.Single(result);
    }

    [Fact]
    public void Subsequence_MatchesAsWeakFallback()
    {
        var result = SearchEngine.Search([R("Download Manager")], "dlm");

        Assert.Single(result);
    }

    [Fact]
    public void Limit_IsRespected()
    {
        var resources = Enumerable.Range(0, 10).Select(i => R($"item{i}"));

        var result = SearchEngine.Search(resources, "item", limit: 3);

        Assert.Equal(3, result.Count);
    }
}
