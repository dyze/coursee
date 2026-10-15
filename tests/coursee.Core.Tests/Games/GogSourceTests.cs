namespace Coursee.Core.Tests.Games;

public class GogSourceTests
{
    private const string Galaxy = @"C:\GOG Galaxy\GalaxyClient.exe";

    private sealed class FakeReader(string? galaxy, params Dictionary<string, string>[] games) : IGogGamesReader
    {
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Games() => games;

        public string? GalaxyClient() => galaxy;
    }

    private sealed class ThrowingReader : IGogGamesReader
    {
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Games() => throw new System.Security.SecurityException("denied");

        public string? GalaxyClient() => null;
    }

    private static IReadOnlyList<Resource> Load(params Dictionary<string, string>[] games) =>
        new GogSource(new FakeReader(null, games)).GetResources();

    private static IReadOnlyList<Resource> LoadWithGalaxy(params Dictionary<string, string>[] games) =>
        new GogSource(new FakeReader(Galaxy, games)).GetResources();

    [Fact]
    public void WithGalaxy_AGame_StartsThroughIt_AsGalaxysShortcutsDo()
    {
        var game = new Dictionary<string, string>
        {
            ["gameName"] = "Foundation",
            ["gameID"] = "1446930743",
            ["exe"] = @"D:\gog\Foundation\foundation.exe",
            ["path"] = @"D:\gog\Foundation",
        };

        Assert.Equal(
            new Resource("Foundation", @"""C:\GOG Galaxy\GalaxyClient.exe"" /command=runGame /gameId=1446930743 /path=""D:\gog\Foundation""", ResourceKind.Game, "GOG"),
            Assert.Single(LoadWithGalaxy(game)));
    }

    [Fact]
    public void WithGalaxy_AGameWithoutFolder_StartsThroughItByItsId()
    {
        var game = new Dictionary<string, string> { ["gameName"] = "Game", ["gameID"] = "42", ["exe"] = @"C:\g.exe" };

        Assert.Equal(@"""C:\GOG Galaxy\GalaxyClient.exe"" /command=runGame /gameId=42", Assert.Single(LoadWithGalaxy(game)).Target);
    }

    [Fact]
    public void WithGalaxy_AGameWithoutId_StartsFromItsProgram()
    {
        var game = new Dictionary<string, string> { ["gameName"] = "Game", ["exe"] = @"C:\g.exe", ["gameID"] = " " };

        Assert.Equal(@"""C:\g.exe""", Assert.Single(LoadWithGalaxy(game)).Target);
    }

    [Fact]
    public void WithoutGalaxy_AGame_StartsFromItsProgram()
    {
        var game = new Dictionary<string, string> { ["gameName"] = "The Witcher 3", ["exe"] = @"C:\GOG Games\Witcher 3\bin\witcher3.exe" };

        Assert.Equal(
            new Resource("The Witcher 3", @"""C:\GOG Games\Witcher 3\bin\witcher3.exe""", ResourceKind.Game, "GOG"),
            Assert.Single(Load(game)));
    }

    [Fact]
    public void TheRegisteredArguments_AreKept()
    {
        var game = new Dictionary<string, string>
        {
            ["gameName"] = "Old Game",
            ["exe"] = @"C:\GOG Games\Old\DOSBOX\DOSBox.exe",
            ["launchParam"] = @"-conf ""..\dosbox.conf"" -noconsole",
        };

        Assert.Equal(@"""C:\GOG Games\Old\DOSBOX\DOSBox.exe"" -conf ""..\dosbox.conf"" -noconsole", Assert.Single(Load(game)).Target);
    }

    [Fact]
    public void AnAddOn_IsLeftOut()
    {
        var dlc = new Dictionary<string, string> { ["gameName"] = "DLC", ["exe"] = @"C:\g.exe", ["dependsOn"] = "1207664663" };

        Assert.Empty(Load(dlc));
    }

    [Theory]
    [InlineData("gameName")]
    [InlineData("exe")]
    public void AGameWithoutNameOrProgram_IsSkipped(string missing)
    {
        var game = new Dictionary<string, string> { ["gameName"] = "Game", ["exe"] = @"C:\g.exe", ["dependsOn"] = " " };
        game[missing] = "";

        Assert.Empty(Load(game));
    }

    [Fact]
    public void AGameRegisteredTwice_IsListedOnce()
    {
        var game = new Dictionary<string, string> { ["gameName"] = "Game", ["exe"] = @"C:\g.exe" };

        Assert.Single(Load(game, new Dictionary<string, string>(game)));
    }

    [Fact]
    public void AnUnreadableRegistry_IsLogged_AndGivesNoGames()
    {
        var log = new RingLog(10);

        Assert.Empty(new GogSource(new ThrowingReader(), log).GetResources());
        Assert.Equal(LogLevel.Warning, Assert.Single(log.Entries).Level);
    }

    [Fact]
    public void TheSource_IsSwitchable_WithItsOwnIcon()
    {
        var source = new GogSource(new FakeReader(null));

        Assert.Equal("GOG", source.Name);
        Assert.Equal("GOG games", source.DisplayName);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.Gog, source.Icon);
    }
}
