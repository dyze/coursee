using Microsoft.Data.Sqlite;

namespace Coursee.Core.Tests;

public sealed class FirefoxBookmarksSourceTests : IDisposable
{
    private readonly string _profile = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    public FirefoxBookmarksSourceTests()
    {
        Directory.CreateDirectory(_profile);
        Execute("""
            CREATE TABLE moz_places (id INTEGER PRIMARY KEY, url TEXT, title TEXT);
            CREATE TABLE moz_bookmarks (id INTEGER PRIMARY KEY, type INTEGER, fk INTEGER, title TEXT);
            """);
    }

    private string Database => Path.Combine(_profile, "places.sqlite");

    private static string Q(string? value) => value is null ? "NULL" : "'" + value.Replace("'", "''") + "'";

    private void Execute(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void AddBookmark(int id, string? title, string url, string? placeTitle = null, int type = 1)
    {
        Execute(
            $"INSERT INTO moz_places (id, url, title) VALUES ({id}, {Q(url)}, {Q(placeTitle)});" +
            $"INSERT INTO moz_bookmarks (id, type, fk, title) VALUES ({id}, {type}, {id}, {Q(title)});");
    }

    [Fact]
    public void ReturnsBookmarksWithTitleAndUrl()
    {
        AddBookmark(1, "Docs", "https://example.com/docs");

        var result = new FirefoxBookmarksSource(_profile).GetResources();

        Assert.Equal(
            new Resource("Docs", "https://example.com/docs", ResourceKind.Url, "Firefox"),
            Assert.Single(result));
    }

    [Fact]
    public void TitleFallsBackToPageTitleThenToUrl()
    {
        AddBookmark(1, null, "https://a.example/", placeTitle: "Page A");
        AddBookmark(2, "", "https://b.example/");

        var result = new FirefoxBookmarksSource(_profile).GetResources();

        Assert.Equal(new[] { "Page A", "https://b.example/" }, result.Select(r => r.Name));
    }

    [Fact]
    public void SkipsFoldersSmartQueriesAndBookmarklets()
    {
        AddBookmark(1, "Folder", "https://folder.example/", type: 2);
        AddBookmark(2, "Recent tags", "place:sort=14&type=6");
        AddBookmark(3, "Bookmarklet", "javascript:alert(1)");
        AddBookmark(4, "Config", "about:config");
        AddBookmark(5, "Good", "https://good.example/");

        var result = new FirefoxBookmarksSource(_profile).GetResources();

        Assert.Equal(new[] { "Good" }, result.Select(r => r.Name));
    }

    [Fact]
    public void SameUrlBookmarkedTwice_AppearsOnce()
    {
        AddBookmark(1, "First", "https://dup.example/");
        AddBookmark(2, "Second", "https://dup.example/");

        var result = new FirefoxBookmarksSource(_profile).GetResources();

        Assert.Equal(new[] { "First" }, result.Select(r => r.Name));
    }

    [Fact]
    public void NoProfileOrNoDatabase_MeansNoResources()
    {
        Assert.Empty(new FirefoxBookmarksSource(null).GetResources());
        Assert.Empty(new FirefoxBookmarksSource(Path.Combine(_profile, "missing")).GetResources());
    }

    [Fact]
    public void ReloadsWhenTheDatabaseChanges()
    {
        AddBookmark(1, "One", "https://one.example/");
        var source = new FirefoxBookmarksSource(_profile);
        Assert.Single(source.GetResources());

        AddBookmark(2, "Two", "https://two.example/");
        File.SetLastWriteTimeUtc(Database, DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(new[] { "One", "Two" }, source.GetResources().Select(r => r.Name));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_profile))
        {
            Directory.Delete(_profile, recursive: true);
        }
    }
}
