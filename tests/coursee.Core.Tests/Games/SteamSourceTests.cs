namespace Coursee.Core.Tests.Games;

public sealed class SteamSourceTests : IDisposable
{
    private readonly string _steam = Path.Combine(Path.GetTempPath(), "coursee-tests", Guid.NewGuid().ToString("N"), "Steam");
    private readonly RingLog _log = new(100);

    public SteamSourceTests() => Directory.CreateDirectory(Path.Combine(_steam, "steamapps"));

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_steam)!, recursive: true);

    private IReadOnlyList<Resource> Load() => new SteamSource(_steam, _log).GetResources();

    private static void WriteManifest(string library, string id, string name, string? stateFlags = "4")
    {
        var flags = stateFlags is null ? string.Empty : $"\"StateFlags\" \"{stateFlags}\"";
        Directory.CreateDirectory(Path.Combine(library, "steamapps"));
        File.WriteAllText(
            Path.Combine(library, "steamapps", $"appmanifest_{id}.acf"),
            $"\"AppState\" {{ \"appid\" \"{id}\" \"name\" \"{name}\" {flags} }}");
    }

    private void WriteLibraries(string content) =>
        File.WriteAllText(Path.Combine(_steam, "steamapps", "libraryfolders.vdf"), content);

    private static string Escaped(string path) => path.Replace("\\", "\\\\");

    [Fact]
    public void AnInstalledGame_OpensThroughSteam()
    {
        WriteManifest(_steam, "620", "Portal 2");

        Assert.Equal(new Resource("Portal 2", "steam://rungameid/620", ResourceKind.Game, "Steam"), Assert.Single(Load()));
    }

    [Fact]
    public void GamesOfEveryLibrary_AreListed()
    {
        var library = Path.Combine(Path.GetDirectoryName(_steam)!, "SteamLibrary");
        WriteManifest(_steam, "620", "Portal 2");
        WriteManifest(library, "570", "Dota 2");
        WriteLibraries($$"""
            "libraryfolders"
            {
                "0" { "path" "{{Escaped(_steam)}}" }
                "1" { "path" "{{Escaped(library)}}" "apps" { "570" "1" } }
            }
            """);

        Assert.Equal(new[] { "Portal 2", "Dota 2" }, Load().Select(game => game.Name));
    }

    [Fact]
    public void TheOldLibraryFormat_IsRead()
    {
        var library = Path.Combine(Path.GetDirectoryName(_steam)!, "Old");
        WriteManifest(library, "570", "Dota 2");
        WriteLibraries($$"""
            "LibraryFolders"
            {
                "TimeNextStatsReport" "1700000000"
                "1" "{{Escaped(library)}}"
            }
            """);

        Assert.Equal("Dota 2", Assert.Single(Load()).Name);
    }

    [Theory]
    [InlineData("1026")]
    [InlineData("2")]
    public void AGameBeingDownloaded_IsLeftOut(string flags)
    {
        WriteManifest(_steam, "620", "Portal 2", flags);

        Assert.Empty(Load());
    }

    [Fact]
    public void AManifestWithoutFlags_CountsAsInstalled()
    {
        WriteManifest(_steam, "620", "Portal 2", stateFlags: null);

        Assert.Single(Load());
    }

    [Theory]
    [InlineData("Steamworks Common Redistributables")]
    [InlineData("Steam Linux Runtime 3.0 (sniper)")]
    [InlineData("Proton 9.0")]
    public void Tools_AreLeftOut(string name)
    {
        WriteManifest(_steam, "228980", name);

        Assert.Empty(Load());
    }

    [Fact]
    public void AnUnreadableManifest_IsLoggedAndSkipped()
    {
        WriteManifest(_steam, "620", "Portal 2");
        File.WriteAllText(Path.Combine(_steam, "steamapps", "appmanifest_1.acf"), "\"AppState\" {");

        Assert.Equal("Portal 2", Assert.Single(Load()).Name);
        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("appmanifest_1.acf"));
    }

    [Fact]
    public void AManifestWithoutIdOrName_IsSkipped()
    {
        File.WriteAllText(Path.Combine(_steam, "steamapps", "appmanifest_1.acf"), "\"AppState\" { \"appid\" \"1\" }");
        File.WriteAllText(Path.Combine(_steam, "steamapps", "appmanifest_2.acf"), "\"Other\" { }");

        Assert.Empty(Load());
    }

    [Fact]
    public void AnUnreadableLibraryList_IsLogged_AndSteamsOwnFolderIsStillRead()
    {
        WriteManifest(_steam, "620", "Portal 2");
        WriteLibraries("\"libraryfolders\" {");

        Assert.Single(Load());
        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("libraryfolders.vdf"));
    }

    [Fact]
    public void AGameInTwoListedLibraries_IsListedOnce()
    {
        WriteManifest(_steam, "620", "Portal 2");
        WriteLibraries($$"""
            "libraryfolders" { "0" { "path" "{{Escaped(_steam)}}" } "1" { "path" "relative" } }
            """);

        Assert.Single(Load());
    }

    [Fact]
    public void SteamNotInstalled_GivesNoGames()
    {
        Assert.Empty(new SteamSource(null).GetResources());
        Assert.Empty(new SteamSource(Path.Combine(_steam, "missing")).GetResources());
    }

    [Fact]
    public void TheSource_IsSwitchable_WithItsOwnIcon()
    {
        var source = new SteamSource(null);

        Assert.Equal("Steam", source.Name);
        Assert.Equal("Steam games", source.DisplayName);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.Steam, source.Icon);
    }
}
