namespace Coursee.Core.Tests.Icons;

public class OwnIconTests
{
    private static string? PathOf(Resource resource) => OwnIcon.PathOf(resource, path => path.StartsWith('N'));

    [Theory]
    [InlineData(ResourceKind.File, @"C:\tools\a.exe")]
    [InlineData(ResourceKind.Folder, @"C:\tools")]
    [InlineData(ResourceKind.App, @"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")]
    public void AFileFolderOrApp_ShowsTheIconOfItsTarget(ResourceKind kind, string target)
    {
        Assert.Equal(target, PathOf(new Resource("a", target, kind, "Desktop")));
    }

    [Fact]
    public void AShortcut_ShowsTheIconOfItsLinkFile()
    {
        var shortcut = new Resource("Tool", @"C:\tools\a.exe", ResourceKind.File, "Desktop", LaunchPath: @"C:\Desktop\Tool.lnk");

        Assert.Equal(@"C:\Desktop\Tool.lnk", PathOf(shortcut));
    }

    [Fact]
    public void AGame_ShowsItsIconPath()
    {
        Assert.Equal(@"C:\g\game.exe", PathOf(new Resource("Game", "steam://rungameid/1", ResourceKind.Game, "Steam", IconPath: @"C:\g\game.exe")));
        Assert.Null(PathOf(new Resource("Game", "steam://rungameid/1", ResourceKind.Game, "Steam")));
    }

    [Theory]
    [InlineData(ResourceKind.Url, "https://example.com")]
    [InlineData(ResourceKind.Command, "notepad")]
    [InlineData(ResourceKind.Setting, "ms-settings:display")]
    [InlineData(ResourceKind.NetworkPath, @"\\server\share")]
    public void OtherKinds_HaveNoIconOfTheirOwn(ResourceKind kind, string target)
    {
        Assert.Null(PathOf(new Resource("a", target, kind, "Custom")));
    }

    [Fact]
    public void ANetworkPath_IsNeverRead()
    {
        Assert.Null(PathOf(new Resource("a", @"N:\share\a.exe", ResourceKind.File, "Desktop")));
        Assert.Null(OwnIcon.PathOf(new Resource("a", @"\\server\a.exe", ResourceKind.File, "Desktop")));
    }
}
