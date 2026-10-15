using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Coursee.Core.Sources;

namespace Coursee.Core.Games;

/// <summary>
/// The games installed with Steam. Steam lists its libraries (one per drive, say) in
/// <c>steamapps\libraryfolders.vdf</c>, and each installed game in an <c>appmanifest_&lt;id&gt;.acf</c>
/// of a library's <c>steamapps</c> folder. A game opens through Steam (<c>steam://rungameid/&lt;id&gt;</c>).
/// These files are not documented: one that cannot be read is logged and skipped.
/// </summary>
public sealed class SteamSource : IResourceSource
{
    public const string SourceName = "Steam";

    /// <summary>Tools Steam installs as apps, which are not games.</summary>
    private static readonly string[] ToolPrefixes = ["Steamworks Common Redistributables", "Steam Linux Runtime", "Proton"];

    /// <summary>The <c>StateFlags</c> bit of a game whose files are all there.</summary>
    private const int FullyInstalled = 4;

    private readonly string? _steamFolder;
    private readonly ILog _log;

    /// <param name="steamFolder">Where Steam is installed; null when it is not.</param>
    public SteamSource(string? steamFolder, ILog? log = null)
    {
        _steamFolder = steamFolder;
        _log = log ?? NullLog.Instance;
    }

    public string Name => SourceName;

    public string DisplayName => "Steam games";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.Steam;

    public IReadOnlyList<Resource> GetResources()
    {
        if (string.IsNullOrEmpty(_steamFolder) || !Directory.Exists(_steamFolder))
        {
            return [];
        }

        return Libraries(_steamFolder)
            .Select(library => Path.Combine(library, "steamapps"))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "appmanifest_*.acf"))
            .Select(ReadGame)
            .OfType<Resource>()
            .DistinctBy(game => game.Target)
            .ToList();
    }

    /// <summary>Steam's own folder, then the libraries of <c>libraryfolders.vdf</c>.</summary>
    private IEnumerable<string> Libraries(string steamFolder)
    {
        var libraries = new List<string> { steamFolder };
        var file = Path.Combine(steamFolder, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(file))
        {
            return libraries;
        }

        try
        {
            var folders = VdfNode.Parse(File.ReadAllText(file))["libraryfolders"]?.Children.Values ?? [];
            foreach (var entry in folders)
            {
                // "0" { "path" "D:\\SteamLibrary" ... } since 2021, "1" "D:\\SteamLibrary" before.
                var path = entry.Value ?? entry.ValueOf("path");
                if (!string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path))
                {
                    libraries.Add(path);
                }
            }
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            _log.Warn(Name, $"Cannot read {file}: {ex.Message}");
        }

        return libraries.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private Resource? ReadGame(string manifest)
    {
        try
        {
            var state = VdfNode.Parse(File.ReadAllText(manifest))["AppState"];
            var id = state?.ValueOf("appid");
            var name = state?.ValueOf("name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || !IsInstalled(state!) || IsTool(name))
            {
                return null;
            }

            return new Resource(name.Trim(), $"steam://rungameid/{id.Trim()}", ResourceKind.Game, Name);
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            _log.Warn(Name, $"Cannot read {manifest}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Not being downloaded or half removed. A manifest without flags counts as installed.</summary>
    private static bool IsInstalled(VdfNode state) =>
        !int.TryParse(state.ValueOf("StateFlags"), out var flags) || (flags & FullyInstalled) != 0;

    private static bool IsTool(string name) =>
        ToolPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
