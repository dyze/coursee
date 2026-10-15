namespace Coursee.Core.Tests.Games;

public class GogSourceTests
{
    private sealed class FakeReader(params Dictionary<string, string>[] games) : IGogGamesReader
    {
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Games() => games;
    }

    private sealed class ThrowingReader : IGogGamesReader
    {
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Games() => throw new System.Security.SecurityException("denied");
    }

    private static IReadOnlyList<Resource> Load(params Dictionary<string, string>[] games) =>
        new GogSource(new FakeReader(games)).GetResources();

    [Fact]
    public void AGame_StartsFromItsProgram()
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
        var source = new GogSource(new FakeReader());

        Assert.Equal("GOG", source.Name);
        Assert.Equal("GOG games", source.DisplayName);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.Gog, source.Icon);
    }
}
