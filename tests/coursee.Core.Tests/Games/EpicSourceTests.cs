namespace Coursee.Core.Tests.Games;

public sealed class EpicSourceTests : IDisposable
{
    private readonly string _manifests = Path.Combine(Path.GetTempPath(), "coursee-tests", Guid.NewGuid().ToString("N"), "Manifests");
    private readonly RingLog _log = new(100);

    public EpicSourceTests() => Directory.CreateDirectory(_manifests);

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_manifests)!, recursive: true);

    private IReadOnlyList<Resource> Load() => new EpicSource(_manifests, _log).GetResources();

    private void Write(string file, string json) => File.WriteAllText(Path.Combine(_manifests, file), json);

    private void WriteGame(string file, string name, string app, string extra = "") => Write(file, $$"""
        {
            "DisplayName": "{{name}}",
            "AppName": "{{app}}",
            "CatalogNamespace": "ns1",
            "CatalogItemId": "item1",
            "AppCategories": ["public", "games", "applications"]{{extra}}
        }
        """);

    [Fact]
    public void AnInstalledGame_OpensThroughTheLauncher()
    {
        WriteGame("A.item", "Fortnite", "Fortnite");

        Assert.Equal(
            new Resource("Fortnite", "com.epicgames.launcher://apps/ns1%3Aitem1%3AFortnite?action=launch&silent=true", ResourceKind.Game, "Epic Games"),
            Assert.Single(Load()));
    }

    [Fact]
    public void OnlyItemFiles_AreRead_InNameOrder()
    {
        WriteGame("B.item", "Second", "b");
        WriteGame("A.item", "First", "a");
        Write("notes.txt", "not a manifest");

        Assert.Equal(new[] { "First", "Second" }, Load().Select(game => game.Name));
    }

    [Fact]
    public void AnUnfinishedInstall_IsLeftOut()
    {
        WriteGame("A.item", "Fortnite", "Fortnite", ", \"bIsIncompleteInstall\": true");

        Assert.Empty(Load());
    }

    [Fact]
    public void AnAddOn_IsLeftOut_ButTheMainGameStays()
    {
        WriteGame("A.item", "Game", "game", ", \"MainGameAppName\": \"game\"");
        WriteGame("B.item", "Game: DLC", "dlc", ", \"MainGameAppName\": \"game\"");

        Assert.Equal("Game", Assert.Single(Load()).Name);
    }

    [Fact]
    public void AnEngine_IsLeftOut()
    {
        Write("A.item", """{ "DisplayName": "Unreal Engine", "AppName": "UE_5", "CatalogNamespace": "ue", "CatalogItemId": "x", "AppCategories": ["engines"] }""");

        Assert.Empty(Load());
    }

    [Fact]
    public void AManifestWithoutCategories_CountsAsAGame()
    {
        Write("A.item", """{ "DisplayName": "Old", "AppName": "old", "CatalogNamespace": "ns", "CatalogItemId": "x" }""");

        Assert.Single(Load());
    }

    [Theory]
    [InlineData("""{ "AppName": "a", "CatalogNamespace": "ns", "CatalogItemId": "x" }""")]
    [InlineData("""{ "DisplayName": " ", "AppName": "a", "CatalogNamespace": "ns", "CatalogItemId": "x" }""")]
    [InlineData("""{ "DisplayName": 3, "AppName": "a", "CatalogNamespace": "ns", "CatalogItemId": "x" }""")]
    public void AManifestWithoutAName_IsSkipped(string json)
    {
        Write("A.item", json);

        Assert.Empty(Load());
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2]")]
    public void AnUnreadableManifest_IsLoggedAndSkipped(string json)
    {
        WriteGame("A.item", "Fortnite", "Fortnite");
        Write("B.item", json);

        Assert.Equal("Fortnite", Assert.Single(Load()).Name);
        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("B.item"));
    }

    [Fact]
    public void AGameWithTwoManifests_IsListedOnce()
    {
        WriteGame("A.item", "Fortnite", "Fortnite");
        WriteGame("B.item", "Fortnite", "Fortnite");

        Assert.Single(Load());
    }

    [Fact]
    public void TheLauncherNotInstalled_GivesNoGames()
    {
        Assert.Empty(new EpicSource(null).GetResources());
        Assert.Empty(new EpicSource(Path.Combine(_manifests, "missing")).GetResources());
    }

    [Fact]
    public void TheManifests_AreInProgramData()
    {
        Assert.Equal(
            Path.Combine(@"C:\ProgramData", "Epic", "EpicGamesLauncher", "Data", "Manifests"),
            EpicSource.ManifestsFolderIn(@"C:\ProgramData"));
    }

    [Fact]
    public void TheSource_IsSwitchable_WithItsOwnIcon()
    {
        var source = new EpicSource(null);

        Assert.Equal("Epic Games", source.Name);
        Assert.Equal("Epic Games", source.DisplayName);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.Epic, source.Icon);
    }
}
