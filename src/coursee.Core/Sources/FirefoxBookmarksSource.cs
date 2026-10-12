using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Microsoft.Data.Sqlite;

namespace Coursee.Core.Sources;

/// <summary>
/// Exposes the bookmarks of a Firefox profile. Firefox keeps <c>places.sqlite</c> locked
/// while it runs, and its newest changes sit in the <c>-wal</c> file, so both files are
/// copied to a temporary folder and read from there. The result is cached until one of
/// the two files changes.
/// </summary>
public sealed class FirefoxBookmarksSource : IResourceSource
{
    public const string SourceName = "Firefox";

    private const string DatabaseFile = "places.sqlite";
    private const string WalFile = "places.sqlite-wal";

    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https", "ftp", "file" };

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

        var wal = Path.Combine(_profileDirectory, WalFile);
        var stamp = Stamp(database) + "|" + Stamp(wal);
        if (stamp == _cachedStamp)
        {
            return _cached;
        }

        try
        {
            _cached = Read(database, wal);
            _cachedStamp = stamp;
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            // Keep the previous list; the next call will try again.
            _log.Warn(Name, $"Cannot read the bookmarks: {ex.GetType().Name}: {ex.Message}");
        }

        return _cached;
    }

    private static string Stamp(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? $"{info.LastWriteTimeUtc.Ticks}:{info.Length}" : "-";
    }

    private IReadOnlyList<Resource> Read(string database, string wal)
    {
        var temp = Path.Combine(Path.GetTempPath(), "coursee-firefox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        try
        {
            var copy = Path.Combine(temp, DatabaseFile);
            File.Copy(database, copy);
            if (File.Exists(wal))
            {
                File.Copy(wal, copy + "-wal");
            }

            using var connection = new SqliteConnection($"Data Source={copy};Pooling=False");
            connection.Open();

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
                if (!IsOpenable(url) || !seen.Add(url!))
                {
                    continue;
                }

                var title = FirstNonEmpty(
                    reader.IsDBNull(0) ? null : reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    url!);

                resources.Add(new Resource(title, url!, ResourceKind.Url, Name));
            }

            return resources;
        }
        finally
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A leftover temp folder is harmless.
            }
        }
    }

    private static bool IsOpenable(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && AllowedSchemes.Contains(uri.Scheme);

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(v => !string.IsNullOrWhiteSpace(v))!;
}
