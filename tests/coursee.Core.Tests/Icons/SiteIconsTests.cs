using Coursee.Core.Tests.Recents;
using Microsoft.Data.Sqlite;

namespace Coursee.Core.Tests.Icons;

public sealed class SiteIconsTests : IDisposable
{
    private static readonly byte[] SmallPng = [0x89, (byte)'P', (byte)'N', (byte)'G', 16];
    private static readonly byte[] LargePng = [0x89, (byte)'P', (byte)'N', (byte)'G', 64];
    private static readonly byte[] Ico = [0, 0, 1, 0, 1];
    private static readonly byte[] Svg = "<svg/>"u8.ToArray();

    private readonly string _firefox = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _edge = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ManualClock _clock = new();
    private readonly RingLog _log = new(100);

    public SiteIconsTests()
    {
        Directory.CreateDirectory(_firefox);
        Directory.CreateDirectory(_edge);
        Execute(FirefoxDatabase, """
            CREATE TABLE moz_pages_w_icons (id INTEGER PRIMARY KEY, page_url TEXT);
            CREATE TABLE moz_icons (id INTEGER PRIMARY KEY, icon_url TEXT, width INTEGER, root INTEGER, data BLOB);
            CREATE TABLE moz_icons_to_pages (page_id INTEGER, icon_id INTEGER);
            """);
        Execute(EdgeDatabase, """
            CREATE TABLE icon_mapping (id INTEGER PRIMARY KEY, page_url TEXT, icon_id INTEGER);
            CREATE TABLE favicon_bitmaps (id INTEGER PRIMARY KEY, icon_id INTEGER, width INTEGER, image_data BLOB);
            """);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_firefox, recursive: true);
        Directory.Delete(_edge, recursive: true);
    }

    private string FirefoxDatabase => Path.Combine(_firefox, "favicons.sqlite");

    private string EdgeDatabase => Path.Combine(_edge, "Favicons");

    private SiteIcons Icons() => new([BrowserSiteIcons.Firefox(_firefox), BrowserSiteIcons.Edge(_edge)], _log, _clock);

    private static void Execute(string database, string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        command.ExecuteNonQuery();
    }

    private static int _ids;

    private void AddFirefoxIcon(string pageUrl, int width, byte[] data)
    {
        var id = Interlocked.Increment(ref _ids);
        Execute(
            FirefoxDatabase,
            "INSERT INTO moz_pages_w_icons (id, page_url) VALUES ($id, $page);" +
            "INSERT INTO moz_icons (id, icon_url, width, root, data) VALUES ($id, 'https://x/icon.png', $width, 0, $data);" +
            "INSERT INTO moz_icons_to_pages (page_id, icon_id) VALUES ($id, $id);",
            ("$id", id), ("$page", pageUrl), ("$width", width), ("$data", data));
    }

    private void AddFirefoxSiteIcon(string iconUrl, byte[] data) =>
        Execute(
            FirefoxDatabase,
            "INSERT INTO moz_icons (icon_url, width, root, data) VALUES ($url, 32, 1, $data);",
            ("$url", iconUrl), ("$data", data));

    private void AddEdgeIcon(string pageUrl, int width, byte[] data)
    {
        var id = Interlocked.Increment(ref _ids);
        Execute(
            EdgeDatabase,
            "INSERT INTO icon_mapping (page_url, icon_id) VALUES ($page, $id);" +
            "INSERT INTO favicon_bitmaps (icon_id, width, image_data) VALUES ($id, $width, $data);",
            ("$id", id), ("$page", pageUrl), ("$width", width), ("$data", data));
    }

    [Theory]
    [InlineData("https://www.GitHub.com/dyze/coursee", "github.com")]
    [InlineData("http://wiki.local:8080/page?q=1", "wiki.local")]
    [InlineData("ftp://files.example.com", null)]
    [InlineData("not a url", null)]
    [InlineData("C:/folder", null)]
    public void HostOf_IsTheSite(string url, string? expected)
    {
        Assert.Equal(expected, SiteIcons.HostOf(url));
    }

    [Theory]
    [InlineData(64, 16, true)]
    [InlineData(64, 128, true)]
    [InlineData(128, 64, false)]
    [InlineData(32, 16, true)]
    [InlineData(32, 64, false)]
    [InlineData(16, 32, false)]
    public void IsBetter_PrefersTheSmallestAt64OrAbove_ElseTheLargest(int width, int kept, bool expected)
    {
        Assert.Equal(expected, BrowserSiteIcons.IsBetter(width, kept));
    }

    [Fact]
    public void IsBitmap_KnowsWhatWpfDecodes()
    {
        Assert.True(BrowserSiteIcons.IsBitmap(SmallPng));
        Assert.True(BrowserSiteIcons.IsBitmap(Ico));
        Assert.True(BrowserSiteIcons.IsBitmap([0xFF, 0xD8, 0]));
        Assert.True(BrowserSiteIcons.IsBitmap("GIF89a"u8.ToArray()));
        Assert.True(BrowserSiteIcons.IsBitmap("BM.."u8.ToArray()));
        Assert.False(BrowserSiteIcons.IsBitmap(Svg));
        Assert.False(BrowserSiteIcons.IsBitmap([]));
    }

    [Fact]
    public void AnyPageOfTheSite_GivesItsBestIcon()
    {
        AddFirefoxIcon("https://github.com/a", 16, SmallPng);
        AddFirefoxIcon("https://www.github.com/b", 64, LargePng);
        AddFirefoxIcon("https://github.com/c", 32, Svg);

        Assert.Equal(LargePng, Icons().IconFor("https://github.com/dyze/coursee"));
    }

    [Fact]
    public void FirefoxsSiteWideIcon_CountsForItsSite()
    {
        AddFirefoxSiteIcon("https://example.com/favicon.ico", Ico);

        Assert.Equal(Ico, Icons().IconFor("https://example.com/page"));
    }

    [Fact]
    public void TheBrowserTheLinkOpensIn_IsAskedFirst()
    {
        AddFirefoxIcon("https://github.com/", 64, LargePng);
        AddEdgeIcon("https://github.com/", 16, SmallPng);

        var icons = Icons();

        Assert.Equal(SmallPng, icons.IconFor("https://github.com/", Browsers.Edge));
        Assert.Equal(LargePng, icons.IconFor("https://github.com/", Browsers.Firefox));
        Assert.Equal(LargePng, icons.IconFor("https://github.com/"));
    }

    [Fact]
    public void ASiteNoBrowserShowed_OrNotAWebAddress_HasNone()
    {
        AddEdgeIcon("https://github.com/", 16, SmallPng);

        Assert.Null(Icons().IconFor("https://example.com/"));
        Assert.Null(Icons().IconFor(@"C:\file.txt"));
    }

    [Fact]
    public void WithoutProfiles_ThereIsNone()
    {
        var icons = new SiteIcons([BrowserSiteIcons.Firefox(null), BrowserSiteIcons.Edge(Path.Combine(_edge, "missing"))]);

        Assert.Null(icons.IconFor("https://github.com/"));
    }

    [Fact]
    public void AChangedCache_IsReadAgain_AtTheNextCheck()
    {
        var icons = Icons();
        Assert.Null(icons.IconFor("https://github.com/"));

        AddEdgeIcon("https://github.com/", 16, SmallPng);
        File.SetLastWriteTimeUtc(EdgeDatabase, DateTime.UtcNow.AddMinutes(1));
        Assert.Null(icons.IconFor("https://github.com/"));

        _clock.Advance(SiteIcons.CheckInterval);
        Assert.Equal(SmallPng, icons.IconFor("https://github.com/"));
    }

    [Fact]
    public void AnUnreadableCache_IsLogged_AndTheOtherStillCounts()
    {
        File.WriteAllText(FirefoxDatabase, "not a database");
        AddEdgeIcon("https://github.com/", 16, SmallPng);

        Assert.Equal(SmallPng, Icons().IconFor("https://github.com/"));
        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Warning && entry.Category == SiteIcons.LogCategory);
    }
}
