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

    [Theory]
    [InlineData("evene", "Événements")]
    [InlineData("parametres", "Paramètres")]
    [InlineData("PARAM", "Paramètres")]
    public void UnaccentedQuery_FindsAccentedName(string query, string name)
    {
        var result = SearchEngine.Search([R(name)], query);

        Assert.Equal(new[] { name }, result.Select(r => r.Name));
    }

    [Fact]
    public void AccentedQuery_FindsUnaccentedName()
    {
        var result = SearchEngine.Search([R("Parametres")], "paramètres");

        Assert.Single(result);
    }

    [Fact]
    public void AccentsDoNotChangeTheRanking()
    {
        // Once accents are ignored, "eve" starts the second name and a word of the first.
        var resources = new[] { R("Les événements"), R("Événements") };

        var result = SearchEngine.Search(resources, "eve");

        Assert.Equal(new[] { "Événements", "Les événements" }, result.Select(r => r.Name));
    }

    [Fact]
    public void Target_IsMatchedWithoutAccents()
    {
        var result = SearchEngine.Search([R("Notes", @"C:\Users\René\Documents")], "rene");

        Assert.Single(result);
    }

    [Fact]
    public void Subsequence_IgnoresAccents()
    {
        var result = SearchEngine.Search([R("Éditeur de texte")], "edt");

        Assert.Single(result);
    }

    [Fact]
    public void AccentsKept_UnaccentedQuery_DoesNotFindAccentedName()
    {
        var result = SearchEngine.Search([R("Événements")], "evene", ignoreAccents: false);

        Assert.Empty(result);
    }

    [Fact]
    public void AccentsKept_AccentedQuery_FindsAccentedName_IgnoringCase()
    {
        var result = SearchEngine.Search([R("Événements"), R("Evenements")], "éVÉ", ignoreAccents: false);

        Assert.Equal(new[] { "Événements" }, result.Select(r => r.Name));
    }

    [Fact]
    public void AccentsKept_TargetIsMatchedAsIs()
    {
        var resources = new[] { R("Notes", @"C:\Users\René"), R("Other", @"C:\Users\Rene") };

        var result = SearchEngine.Search(resources, "rené", ignoreAccents: false);

        Assert.Equal(new[] { "Notes" }, result.Select(r => r.Name));
    }
}
