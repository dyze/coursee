using Coursee.Core.Logging;
using Coursee.Core.Sources;
using Microsoft.Data.Sqlite;

namespace Coursee.Core.Icons;

/// <summary>
/// The icon a web site shows, as the browsers keep it on this PC (<see cref="BrowserSiteIcons"/>):
/// nothing is downloaded. A site is known by its host, so a bookmark, a typed URL and a custom
/// entry of the same site share it. A cache is read again when it changed, checked at most every
/// <see cref="CheckInterval"/>: the browser writes it often. Thread-safe.
/// </summary>
public sealed class SiteIcons(IReadOnlyList<BrowserSiteIcons> caches, ILog? log = null, TimeProvider? clock = null)
{
    public const string LogCategory = "Site icons";

    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    private readonly ILog _log = log ?? NullLog.Instance;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _lock = new();
    private readonly Dictionary<string, (string Stamp, IReadOnlyDictionary<string, byte[]> Icons)> _read = new(StringComparer.Ordinal);
    private DateTimeOffset? _checked;

    /// <summary>
    /// The site's icon (PNG, ICO, JPEG, GIF or BMP data); null when no browser kept one. The browser
    /// the URL opens in is asked first.
    /// </summary>
    public byte[]? IconFor(string url, string? browser = null)
    {
        if (HostOf(url) is not { } host)
        {
            return null;
        }

        lock (_lock)
        {
            ReadChanged();
            foreach (var cache in caches.OrderBy(cache => cache.Browser == browser ? 0 : 1))
            {
                if (_read.TryGetValue(cache.Browser, out var read) && read.Icons.TryGetValue(host, out var icon))
                {
                    return icon;
                }
            }
        }

        return null;
    }

    /// <summary>The site of a web address: its host, lower case, without <c>www.</c>; null for anything else.</summary>
    public static string? HostOf(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal) ? host["www.".Length..] : host;
    }

    private void ReadChanged()
    {
        var now = _clock.GetUtcNow();
        if (_checked is { } last && now - last < CheckInterval)
        {
            return;
        }

        _checked = now;
        foreach (var cache in caches)
        {
            if (cache.Database is not { } database || !File.Exists(database))
            {
                continue;
            }

            var stamp = SqliteSnapshot.Stamp(database);
            if (_read.TryGetValue(cache.Browser, out var read) && read.Stamp == stamp)
            {
                continue;
            }

            try
            {
                var icons = SqliteSnapshot.Read(database, cache.Read);
                _read[cache.Browser] = (stamp, icons);
                _log.Debug(LogCategory, $"{icons.Count} sites read from {cache.Browser}");
            }
            catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException or InvalidCastException)
            {
                // Keep what was read before; the next check tries again.
                _log.Warn(LogCategory, $"Cannot read {cache.Browser}'s site icons: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
