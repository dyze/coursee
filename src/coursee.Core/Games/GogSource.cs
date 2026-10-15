using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Coursee.Core.Sources;

namespace Coursee.Core.Games;

/// <summary>
/// Reads what GOG registers (<c>HKLM\SOFTWARE\WOW6432Node\GOG.com</c>): one set of raw values per
/// installed game (<c>gameName</c>, <c>gameID</c>, <c>exe</c>, <c>path</c>...), and GOG Galaxy's program.
/// </summary>
public interface IGogGamesReader
{
    IReadOnlyList<IReadOnlyDictionary<string, string>> Games();

    /// <summary>The full path of <c>GalaxyClient.exe</c>, or null when GOG Galaxy is not installed.</summary>
    string? GalaxyClient();
}

/// <summary>
/// The games installed from GOG, with GOG Galaxy or GOG's offline installers. With GOG Galaxy
/// installed, a game starts through it, as the shortcuts GOG Galaxy makes do
/// (<c>GalaxyClient.exe /command=runGame /gameId=... /path=...</c>). Without it, a game starts from
/// its own program (GOG games have no DRM), with the arguments GOG registered.
/// Add-ons (registered with <c>dependsOn</c>) are left out.
/// </summary>
public sealed class GogSource : IResourceSource
{
    public const string SourceName = "GOG";

    private readonly IGogGamesReader _reader;
    private readonly ILog _log;

    public GogSource(IGogGamesReader reader, ILog? log = null)
    {
        _reader = reader;
        _log = log ?? NullLog.Instance;
    }

    public string Name => SourceName;

    public string DisplayName => "GOG games";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.Gog;

    public IReadOnlyList<Resource> GetResources()
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> games;
        string? galaxy;
        try
        {
            games = _reader.Games();
            galaxy = _reader.GalaxyClient();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            _log.Warn(Name, $"Cannot read the GOG games: {ex.GetType().Name}: {ex.Message}");
            return [];
        }

        return games
            .Select(game => AsGame(game, galaxy))
            .OfType<Resource>()
            .DistinctBy(game => game.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Resource? AsGame(IReadOnlyDictionary<string, string> values, string? galaxy)
    {
        var name = Value(values, "gameName");
        var program = Value(values, "exe");
        if (name is null || program is null || Value(values, "dependsOn") is not null)
        {
            return null;
        }

        return new Resource(name, ThroughGalaxy(values, galaxy) ?? FromProgram(values, program), ResourceKind.Game, SourceName);
    }

    /// <summary>The command line of GOG Galaxy's own shortcuts, or null without GOG Galaxy or a game ID.</summary>
    private static string? ThroughGalaxy(IReadOnlyDictionary<string, string> values, string? galaxy)
    {
        var id = Value(values, "gameID");
        if (galaxy is null || id is null)
        {
            return null;
        }

        var folder = Value(values, "path");
        return $"\"{galaxy}\" /command=runGame /gameId={id}" + (folder is null ? string.Empty : $" /path=\"{folder}\"");
    }

    private static string FromProgram(IReadOnlyDictionary<string, string> values, string program)
    {
        var arguments = Value(values, "launchParam");
        return $"\"{program}\"" + (arguments is null ? string.Empty : " " + arguments);
    }

    /// <summary>A value that is not blank (the registry often holds empty ones), else null.</summary>
    private static string? Value(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
}
