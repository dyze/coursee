using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Microsoft.Data.Sqlite;

namespace Coursee.Core.Sources;

/// <summary>
/// Exposes the bookmarks of a Firefox profile, read from <c>places.sqlite</c> through a copy
/// (<see cref="SqliteSnapshot"/>). The result is cached until the file or its <c>-wal</c> changes.
/// </summary>
public sealed class FirefoxBookmarksSource : IResourceSource
{
    public const string SourceName = "Firefox";

    private const string DatabaseFile = "places.sqlite";

    private readonly string? _profileDirectory;
    private readonly ILog _log;
    private string? _cachedStamp;
    private IReadOnlyList<Resource> _cached = [];

    public FirefoxBookmarksSource(string? profileDirectory, ILog? log = null)
    {
        _profileDirectory = profileDirectory;
        _log = log ?? NullLog.Instance;
    }

    public string Name => SourceName;

    public string DisplayName => "Firefox bookmarks";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.Firefox;

    public IReadOnlyList<Resource> GetResources()
    {
        if (string.IsNullOrEmpty(_profileDirectory))
        {
            return [];
        }

        var database = Path.Combine(_profileDirectory, DatabaseFile);
        if (!File.Exists(database))
        {
            return [];
        }

        var stamp = SqliteSnapshot.Stamp(database);
        if (stamp == _cachedStamp)
        {
            return _cached;
        }

        try
        {
            _cached = SqliteSnapshot.Read(database, Read);
            _cachedStamp = stamp;
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            // Keep the previous list; the next call will try again.
            _log.Warn(Name, $"Cannot read the bookmarks: {ex.GetType().Name}: {ex.Message}");
        }

        return _cached;
    }

    private IReadOnlyList<Resource> Read(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT b.title, p.title, p.url
            FROM moz_bookmarks b
            JOIN moz_places p ON b.fk = p.id
            WHERE b.type = 1
            ORDER BY b.id
            """;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var url = reader.IsDBNull(2) ? null : reader.GetString(2);
            if (!BookmarkUrls.IsOpenable(url) || !seen.Add(url!))
            {
                continue;
            }

            var title = FirstNonEmpty(
                reader.IsDBNull(0) ? null : reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                url!);

            resources.Add(new Resource(title, url!, ResourceKind.Url, Name, Browser: Browsers.Firefox));
        }

        return resources;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(v => !string.IsNullOrWhiteSpace(v))!;
}
