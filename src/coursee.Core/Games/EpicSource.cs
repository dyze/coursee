using System.Text.Json;
using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Coursee.Core.Sources;

namespace Coursee.Core.Games;

/// <summary>
/// The games installed with the Epic Games Launcher, which keeps a JSON manifest per install
/// (<c>*.item</c>) in its <c>Manifests</c> folder. A game opens through the launcher
/// (<c>com.epicgames.launcher://apps/...?action=launch</c>). Add-ons, engines and unfinished
/// installs are left out. These files are not documented: one that cannot be read is logged and
/// skipped.
/// </summary>
public sealed class EpicSource : IResourceSource
{
    public const string SourceName = "Epic Games";

    private readonly string? _manifestsFolder;
    private readonly ILog _log;

    /// <param name="manifestsFolder">The launcher's <c>Manifests</c> folder (see <see cref="ManifestsFolderIn"/>).</param>
    public EpicSource(string? manifestsFolder, ILog? log = null)
    {
        _manifestsFolder = manifestsFolder;
        _log = log ?? NullLog.Instance;
    }

    public string Name => SourceName;

    public string DisplayName => "Epic Games";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.Epic;

    /// <summary>Where the launcher keeps its manifests, in <c>%ProgramData%</c>.</summary>
    public static string ManifestsFolderIn(string programData) =>
        Path.Combine(programData, "Epic", "EpicGamesLauncher", "Data", "Manifests");

    public IReadOnlyList<Resource> GetResources()
    {
        if (string.IsNullOrEmpty(_manifestsFolder) || !Directory.Exists(_manifestsFolder))
        {
            return [];
        }

        return Directory.EnumerateFiles(_manifestsFolder, "*.item")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(ReadGame)
            .OfType<Resource>()
            .DistinctBy(game => game.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private Resource? ReadGame(string manifest)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifest));
            var root = document.RootElement;
            var name = Text(root, "DisplayName");
            var app = Text(root, "AppName");
            var catalog = Text(root, "CatalogNamespace");
            var item = Text(root, "CatalogItemId");
            if (name is null || app is null || catalog is null || item is null || !IsGame(root, app))
            {
                return null;
            }

            var link = $"com.epicgames.launcher://apps/{Uri.EscapeDataString(catalog)}%3A{Uri.EscapeDataString(item)}%3A{Uri.EscapeDataString(app)}?action=launch&silent=true";
            return new Resource(name, link, ResourceKind.Game, Name);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            _log.Warn(Name, $"Cannot read {manifest}: {ex.Message}");
            return null;
        }
    }

    /// <summary>A whole install of a game: not an add-on of another one, nor an engine or a tool.</summary>
    private static bool IsGame(JsonElement root, string app)
    {
        if (root.TryGetProperty("bIsIncompleteInstall", out var incomplete) && incomplete.ValueKind == JsonValueKind.True)
        {
            return false;
        }

        if (Text(root, "MainGameAppName") is { } mainGame && !string.Equals(mainGame, app, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !root.TryGetProperty("AppCategories", out var categories)
            || categories.ValueKind != JsonValueKind.Array
            || categories.EnumerateArray().Any(category => category.ValueKind == JsonValueKind.String && category.GetString() == "games");
    }

    /// <summary>A string property that is not blank, else null.</summary>
    private static string? Text(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;
}
