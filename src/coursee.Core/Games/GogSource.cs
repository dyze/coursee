using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Coursee.Core.Sources;

namespace Coursee.Core.Games;

/// <summary>
/// Reads the games GOG installs register (<c>HKLM\SOFTWARE\WOW6432Node\GOG.com\Games</c>): one set
/// of raw values per game (<c>gameName</c>, <c>exe</c>, <c>launchParam</c>...).
/// </summary>
public interface IGogGamesReader
{
    IReadOnlyList<IReadOnlyDictionary<string, string>> Games();
}

/// <summary>
/// The games installed from GOG, with GOG Galaxy or GOG's offline installers. GOG Galaxy has no
/// documented link to start a game, and GOG games have no DRM, so a game starts from its own
/// program, with the arguments GOG registered. Add-ons (registered with <c>dependsOn</c>) are left out.
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
        try
        {
            games = _reader.Games();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            _log.Warn(Name, $"Cannot read the GOG games: {ex.GetType().Name}: {ex.Message}");
            return [];
        }

        return games
            .Select(AsGame)
            .OfType<Resource>()
            .DistinctBy(game => game.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Resource? AsGame(IReadOnlyDictionary<string, string> values)
    {
        var name = Value(values, "gameName");
        var program = Value(values, "exe");
        if (name is null || program is null || Value(values, "dependsOn") is not null)
        {
            return null;
        }

        var arguments = Value(values, "launchParam");
        var commandLine = $"\"{program}\"" + (arguments is null ? string.Empty : " " + arguments);
        return new Resource(name, commandLine, ResourceKind.Game, SourceName);
    }

    /// <summary>A value that is not blank (the registry often holds empty ones), else null.</summary>
    private static string? Value(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
}
