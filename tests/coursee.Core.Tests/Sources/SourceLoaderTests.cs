namespace Coursee.Core.Tests.Sources;

public class SourceLoaderTests
{
    private sealed class FixedSource(string name, params Resource[] resources) : IResourceSource
    {
        public string Name => name;

        public IReadOnlyList<Resource> GetResources() => resources;
    }

    private sealed class FailingSource(string name) : IResourceSource
    {
        public string Name => name;

        public IReadOnlyList<Resource> GetResources() => throw new InvalidOperationException("boom");
    }

    private static Resource R(string name, string source) => new(name, @"C:\" + name, ResourceKind.Folder, source);

    [Fact]
    public void Load_ReturnsEachSourceInOrder()
    {
        var results = SourceLoader.Load([new FixedSource("A", R("a", "A")), new FixedSource("B", R("b", "B"))]);

        Assert.Equal(new[] { "A", "B" }, results.Select(r => r.SourceName));
        Assert.Equal("a", Assert.Single(results[0].Resources).Name);
        Assert.All(results, r => Assert.Null(r.Error));
    }

    [Fact]
    public void FailingSource_IsReportedAndDoesNotStopTheOthers()
    {
        var results = SourceLoader.Load([new FailingSource("Bad"), new FixedSource("Good", R("g", "Good"))]);

        Assert.Equal(2, results.Count);
        Assert.Empty(results[0].Resources);
        Assert.IsType<InvalidOperationException>(results[0].Error);
        Assert.Equal("g", Assert.Single(results[1].Resources).Name);
    }

    [Fact]
    public void Merge_ListsAnItemInTwoSourcesOnce_FirstSourceWins()
    {
        var desktop = new Resource("Firefox", @"C:\ff\firefox.exe", ResourceKind.File, "Desktop", @"C:\Desktop\Firefox.lnk");
        var startMenu = new Resource("firefox", @"C:\FF\Firefox.exe", ResourceKind.File, "Start Menu", @"C:\Start\Firefox.lnk");

        var merged = SourceLoader.Merge([[desktop], [startMenu]]);

        Assert.Equal(desktop, Assert.Single(merged));
    }

    [Fact]
    public void Merge_KeepsSameTargetWithDifferentNames()
    {
        var work = new Resource("Chrome (work)", @"C:\chrome.exe", ResourceKind.File, "Desktop", @"C:\D\work.lnk");
        var home = new Resource("Chrome (home)", @"C:\chrome.exe", ResourceKind.File, "Desktop", @"C:\D\home.lnk");

        Assert.Equal(new[] { work, home }, SourceLoader.Merge([[work, home]]));
    }

    [Fact]
    public void Merge_KeepsTheOrder()
    {
        var merged = SourceLoader.Merge([[R("b", "A"), R("a", "A")], [R("c", "B")]]);

        Assert.Equal(new[] { "b", "a", "c" }, merged.Select(r => r.Name));
    }

    [Fact]
    public void NoSources_MeansNoResults()
    {
        Assert.Empty(SourceLoader.Load([]));
    }
}
