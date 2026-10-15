namespace Coursee.Core.Tests.Sources;

public class SourceLoaderTests
{
    private sealed class FixedSource(string name, params Resource[] resources) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => name;

        public bool CanBeDisabled => true;

        public string Icon => string.Empty;

        public IReadOnlyList<Resource> GetResources() => resources;
    }

    private sealed class FailingSource(string name) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => name;

        public bool CanBeDisabled => true;

        public string Icon => string.Empty;

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

    [Theory]
    [InlineData(ResourceKind.App, @"shell:AppsFolder\1Password")]
    [InlineData(ResourceKind.Game, "steam://rungameid/620")]
    public void Merge_AnAppOrGameWithTheNameOfAnEarlierItem_IsListedOnce(ResourceKind kind, string target)
    {
        var startMenu = new Resource("Portal 2", @"C:\Games\portal2.exe", ResourceKind.File, "Start Menu", @"C:\Start\Portal 2.lnk");
        var app = new Resource("portal 2", target, kind, "Packaged apps");

        Assert.Equal(startMenu, Assert.Single(SourceLoader.Merge([[startMenu], [app]])));
    }

    [Fact]
    public void Merge_AnAppOrGameWithANewName_IsListed()
    {
        var shortcut = new Resource("Notes", @"C:\notes.txt", ResourceKind.File, "Desktop");
        var app = new Resource("Calculator", @"shell:AppsFolder\Calculator", ResourceKind.App, "Packaged apps");
        var game = new Resource("Portal 2", "steam://rungameid/620", ResourceKind.Game, "Steam");

        Assert.Equal(new[] { shortcut, app, game }, SourceLoader.Merge([[shortcut], [app], [game]]));
    }

    [Fact]
    public void Merge_OtherKinds_StayWhenOnlyTheNameIsTheSame()
    {
        var work = new Resource("Docs", @"C:\work\docs", ResourceKind.Folder, "Desktop");
        var bookmark = new Resource("Docs", "https://docs.example.com", ResourceKind.Url, "Firefox");

        Assert.Equal(new[] { work, bookmark }, SourceLoader.Merge([[work], [bookmark]]));
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
