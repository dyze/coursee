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

    private sealed class SlowSource(string name) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => name;

        public bool CanBeDisabled => true;

        public string Icon => string.Empty;

        public IReadOnlyList<Resource> GetResources()
        {
            Thread.Sleep(25);
            return [];
        }
    }

    /// <summary>Waits for the other sources sharing <paramref name="meeting"/>: only met when they are read at the same time.</summary>
    private sealed class MeetingSource(string name, Barrier meeting) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => name;

        public bool CanBeDisabled => true;

        public string Icon => string.Empty;

        public IReadOnlyList<Resource> GetResources() =>
            meeting.SignalAndWait(TimeSpan.FromSeconds(10)) ? [R("met", name)] : throw new TimeoutException("read alone");
    }

    private static Resource R(string name, string source) => new(name, @"C:\" + name, ResourceKind.Folder, source);

    [Fact]
    public void Load_ReadsTheSourcesInParallel()
    {
        using var meeting = new Barrier(2);

        var results = SourceLoader.Load([new MeetingSource("A", meeting), new MeetingSource("B", meeting)]);

        Assert.Equal(new[] { "A", "B" }, results.Select(r => r.SourceName));
        Assert.All(results, r => Assert.Null(r.Error));
    }

    [Fact]
    public void Load_ReturnsEachSourceInOrder()
    {
        var results = SourceLoader.Load([new FixedSource("A", R("a", "A")), new FixedSource("B", R("b", "B"))]);

        Assert.Equal(new[] { "A", "B" }, results.Select(r => r.SourceName));
        Assert.Equal("a", Assert.Single(results[0].Resources).Name);
        Assert.All(results, r => Assert.Null(r.Error));
    }

    [Fact]
    public void Load_TimesEachSource_FailingOnesToo()
    {
        var results = SourceLoader.Load([new SlowSource("Slow"), new FailingSource("Bad")]);

        Assert.True(results[0].Duration >= TimeSpan.FromMilliseconds(20));
        Assert.True(results[1].Duration >= TimeSpan.Zero);
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
    public void Merge_AnAppWithTheNameOfAnEarlierItem_IsListedOnce_AsThatItem()
    {
        var startMenu = new Resource("1Password", @"C:\1Password\1Password.exe", ResourceKind.File, "Start Menu", @"C:\Start\1Password.lnk");
        var app = new Resource("1password", @"shell:AppsFolder\1Password", ResourceKind.App, "Packaged apps");

        Assert.Equal(startMenu, Assert.Single(SourceLoader.Merge([[startMenu], [app]])));
    }

    [Fact]
    public void Merge_AGameWithTheNameOfAnEarlierShortcut_TakesItsPlace()
    {
        var desktop = new Resource("Notes", @"C:\notes.txt", ResourceKind.File, "Desktop");
        var shortcut = new Resource("Foundation", @"C:\GOG Galaxy\GalaxyClient.exe", ResourceKind.File, "Start Menu", @"C:\Start\Foundation.lnk");
        var game = new Resource("foundation", @"""C:\GOG Galaxy\GalaxyClient.exe"" /command=runGame /gameId=1", ResourceKind.Game, "GOG");

        Assert.Equal(new[] { desktop, game }, SourceLoader.Merge([[desktop, shortcut], [game]]));
    }

    [Theory]
    [InlineData(ResourceKind.File, null)]
    [InlineData(ResourceKind.Folder, @"C:\Start\Portal 2.lnk")]
    [InlineData(ResourceKind.App, null)]
    public void Merge_AGameWithTheNameOfAnEarlierItemThatIsNoShortcut_IsListedOnce_AsThatItem(ResourceKind kind, string? launchPath)
    {
        var earlier = new Resource("Portal 2", @"C:\Games\Portal 2", kind, "Custom", launchPath);
        var game = new Resource("portal 2", "steam://rungameid/620", ResourceKind.Game, "Steam");

        Assert.Equal(earlier, Assert.Single(SourceLoader.Merge([[earlier], [game]])));
    }

    [Fact]
    public void Merge_ASecondGameWithTheSameName_IsLeftOut()
    {
        var steam = new Resource("Portal 2", "steam://rungameid/620", ResourceKind.Game, "Steam");
        var gog = new Resource("Portal 2", @"""C:\g.exe""", ResourceKind.Game, "GOG");

        Assert.Equal(steam, Assert.Single(SourceLoader.Merge([[steam], [gog]])));
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
