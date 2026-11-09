using Coursee.Core.Resources;
using Microsoft.Data.Sqlite;

namespace Coursee.Core.Icons;

/// <summary>
/// The icons a browser keeps of the sites it showed, in its profile: Firefox's <c>favicons.sqlite</c>,
/// Chromium's (Edge's) <c>Favicons</c>. One icon per site (<see cref="SiteIcons.HostOf"/>): the size
/// closest above <see cref="WantedPixels"/>, else the largest. These files are not documented.
/// </summary>
public sealed class BrowserSiteIcons
{
    /// <summary>A 32-pixel row icon at 200 % scaling.</summary>
    public const int WantedPixels = 64;

    // Each query gives a page (or, for Firefox's site-wide icons, the icon's own URL), the icon's width and its data.
    private const string FirefoxQuery = """
        SELECT p.page_url, i.width, i.data
        FROM moz_pages_w_icons p
        JOIN moz_icons_to_pages ip ON ip.page_id = p.id
        JOIN moz_icons i ON i.id = ip.icon_id
        WHERE i.data IS NOT NULL
        UNION ALL
        SELECT icon_url, width, data FROM moz_icons WHERE root = 1 AND data IS NOT NULL
        """;

    private const string ChromiumQuery = """
        SELECT m.page_url, b.width, b.image_data
        FROM icon_mapping m
        JOIN favicon_bitmaps b ON b.icon_id = m.icon_id
        WHERE b.image_data IS NOT NULL
        """;

    private readonly string _query;

    private BrowserSiteIcons(string browser, string? database, string query)
    {
        Browser = browser;
        Database = database;
        _query = query;
    }

    /// <summary>The browser (<see cref="Browsers"/>), as a bookmark's <see cref="Resource.Browser"/> names it.</summary>
    public string Browser { get; }

    /// <summary>The cache's file; null without a profile.</summary>
    public string? Database { get; }

    /// <param name="profile">The Firefox profile's folder; null when there is none.</param>
    public static BrowserSiteIcons Firefox(string? profile) =>
        new(Browsers.Firefox, profile is null ? null : Path.Combine(profile, "favicons.sqlite"), FirefoxQuery);

    /// <param name="profile">The Edge profile's folder (Chromium's format); null when there is none.</param>
    public static BrowserSiteIcons Edge(string? profile) =>
        new(Browsers.Edge, profile is null ? null : Path.Combine(profile, "Favicons"), ChromiumQuery);

    /// <summary>The best icon of each site, read from a copy of the cache (<see cref="Sources.SqliteSnapshot"/>).</summary>
    public IReadOnlyDictionary<string, byte[]> Read(SqliteConnection connection)
    {
        var best = new Dictionary<string, (int Width, byte[] Data)>(StringComparer.Ordinal);
        using var command = connection.CreateCommand();
        command.CommandText = _query;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || SiteIcons.HostOf(reader.GetString(0)) is not { } host)
            {
                continue;
            }

            var width = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            if (best.TryGetValue(host, out var kept) && !IsBetter(width, kept.Width))
            {
                continue;
            }

            var data = (byte[])reader.GetValue(2);
            if (IsBitmap(data))
            {
                best[host] = (width, data);
            }
        }

        return best.ToDictionary(pair => pair.Key, pair => pair.Value.Data, StringComparer.Ordinal);
    }

    /// <summary>The smallest at or above <see cref="WantedPixels"/> (sharp, not heavy), else the largest.</summary>
    public static bool IsBetter(int width, int kept) =>
        width >= WantedPixels ? kept < WantedPixels || width < kept : kept < WantedPixels && width > kept;

    /// <summary>
    /// An image WPF decodes: PNG, ICO, JPEG, GIF, BMP. Firefox also keeps SVG icons, which would
    /// need an SVG library.
    /// </summary>
    public static bool IsBitmap(byte[] data) => data switch
    {
        [0x89, (byte)'P', (byte)'N', (byte)'G', ..] => true,
        [0, 0, 1, 0, ..] => true,
        [0xFF, 0xD8, ..] => true,
        [(byte)'G', (byte)'I', (byte)'F', ..] => true,
        [(byte)'B', (byte)'M', ..] => true,
        _ => false,
    };
}
