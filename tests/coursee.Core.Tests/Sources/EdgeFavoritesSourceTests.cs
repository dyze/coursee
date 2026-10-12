namespace Coursee.Core.Tests.Sources;

public sealed class EdgeFavoritesSourceTests : IDisposable
{
    private readonly string _profile = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    public EdgeFavoritesSourceTests()
    {
        Directory.CreateDirectory(_profile);
    }

    private string BookmarksPath => Path.Combine(_profile, "Bookmarks");

    private sealed class ListLog : ILog
    {
        public List<LogEntry> Entries { get; } = [];

        public void Write(LogEntry entry) => Entries.Add(entry);
    }

    private void WriteFavorites(params (string Name, string Url)[] links)
    {
        var children = string.Join(",", links.Select(l => $"{{\"type\":\"url\",\"name\":\"{l.Name}\",\"url\":\"{l.Url}\"}}"));
        File.WriteAllText(BookmarksPath, $"{{\"roots\":{{\"bookmark_bar\":{{\"type\":\"folder\",\"children\":[{children}]}}}}}}");
    }

    [Fact]
    public void ReturnsTheFavorites()
    {
        WriteFavorites(("Example", "https://example.com/"));

        var resource = Assert.Single(new EdgeFavoritesSource(_profile).GetResources());

        Assert.Equal(new Resource("Example", "https://example.com/", ResourceKind.Url, "Edge"), resource);
    }

    [Fact]
    public void NoProfileOrNoFile_MeansNoResources()
    {
        Assert.Empty(new EdgeFavoritesSource(null).GetResources());
        Assert.Empty(new EdgeFavoritesSource(_profile).GetResources());
    }

    [Fact]
    public void ReloadsWhenTheFileChanges()
    {
        WriteFavorites(("One", "https://one.example/"));
        var source = new EdgeFavoritesSource(_profile);
        Assert.Single(source.GetResources());

        WriteFavorites(("One", "https://one.example/"), ("Two", "https://two.example/"));
        File.SetLastWriteTimeUtc(BookmarksPath, DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(2, source.GetResources().Count);
    }

    [Fact]
    public void BrokenFile_KeepsThePreviousListAndLogsAWarning()
    {
        WriteFavorites(("One", "https://one.example/"));
        var log = new ListLog();
        var source = new EdgeFavoritesSource(_profile, log);
        Assert.Single(source.GetResources());

        File.WriteAllText(BookmarksPath, "{ half written");
        File.SetLastWriteTimeUtc(BookmarksPath, DateTime.UtcNow.AddMinutes(1));

        Assert.Single(source.GetResources());
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Warning && e.Category == "Edge");
    }

    [Fact]
    public void CanBeSwitchedOff()
    {
        var source = new EdgeFavoritesSource(_profile);

        Assert.True(source.CanBeDisabled);
        Assert.Equal("Edge favorites", source.DisplayName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_profile))
        {
            Directory.Delete(_profile, recursive: true);
        }
    }
}
