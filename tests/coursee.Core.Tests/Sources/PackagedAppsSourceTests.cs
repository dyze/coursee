namespace Coursee.Core.Tests.Sources;

public class PackagedAppsSourceTests
{
    private sealed class FakeReader(params AppsFolderItem[] items) : IAppsFolderReader
    {
        public IReadOnlyList<AppsFolderItem> Items() => items;
    }

    private static IReadOnlyList<Resource> Load(params AppsFolderItem[] items) =>
        new PackagedAppsSource(new FakeReader(items)).GetResources();

    [Fact]
    public void APackagedApp_OpensByItsAppId()
    {
        var calculator = new AppsFolderItem("Calculator", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", "Microsoft.WindowsCalculator_8wekyb3d8bbwe");

        Assert.Equal(
            new Resource("Calculator", @"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", ResourceKind.App, "Packaged apps"),
            Assert.Single(Load(calculator)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void DesktopPrograms_AreLeftOut(string? family)
    {
        Assert.Empty(Load(new AppsFolderItem("Notepad++", @"{6D809377-6AF0-444B-8957-A3773F02200E}\Notepad++\notepad++.exe", family)));
    }

    [Fact]
    public void AnAppListedTwice_IsListedOnce()
    {
        var terminal = new AppsFolderItem("Terminal", "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", "Microsoft.WindowsTerminal_8wekyb3d8bbwe");

        Assert.Single(Load(terminal, terminal with { AppUserModelId = "microsoft.windowsterminal_8wekyb3d8bbwe!app" }));
    }

    [Fact]
    public void TheSource_IsSwitchable_WithItsOwnIcon()
    {
        var source = new PackagedAppsSource(new FakeReader());

        Assert.Equal("Packaged apps", source.Name);
        Assert.Equal("Packaged apps (Store, MSIX)", source.DisplayName);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.PackagedApps, source.Icon);
    }
}
