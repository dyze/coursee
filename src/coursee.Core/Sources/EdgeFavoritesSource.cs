using System.Text.Json;
using Coursee.Core.Logging;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// Exposes the favorites (bookmarks) of an Edge profile, read from its <c>Bookmarks</c> JSON
/// file. Edge does not lock the file. The result is cached until the file changes.
/// </summary>
public sealed class EdgeFavoritesSource : IResourceSource
{
    public const string SourceName = "Edge";

    private readonly string? _profileDirectory;
    private readonly ILog _log;
    private string? _cachedStamp;
    private IReadOnlyList<Resource> _cached = [];

    public EdgeFavoritesSource(string? profileDirectory, ILog? log = null)
    {
        _profileDirectory = profileDirectory;
        _log = log ?? NullLog.Instance;
    }

    public string Name => SourceName;

    public string DisplayName => "Edge favorites";

    public bool CanBeDisabled => true;

    public IReadOnlyList<Resource> GetResources()
    {
        if (string.IsNullOrEmpty(_profileDirectory))
        {
            return [];
        }

        var file = new FileInfo(Path.Combine(_profileDirectory, ChromiumBookmarks.BookmarksFile));
        if (!file.Exists)
        {
            return [];
        }

        var stamp = $"{file.LastWriteTimeUtc.Ticks}:{file.Length}";
        if (stamp == _cachedStamp)
        {
            return _cached;
        }

        try
        {
            _cached = ChromiumBookmarks.Parse(ChromiumBookmarks.ReadShared(file.FullName), Name, Browsers.Edge);
            _cachedStamp = stamp;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Keep the previous list (Edge may be writing the file); the next call will try again.
            _log.Warn(Name, $"Cannot read the favorites: {ex.GetType().Name}: {ex.Message}");
        }

        return _cached;
    }
}
