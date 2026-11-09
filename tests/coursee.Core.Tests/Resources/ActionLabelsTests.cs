namespace Coursee.Core.Tests.Resources;

public class ActionLabelsTests
{
    [Fact]
    public void EveryAction_HasItsOwnIcon_FromTheLibrary()
    {
        var icons = Enum.GetValues<ResourceAction>().Select(ActionLabels.IconOf).ToList();

        Assert.All(icons, icon => Assert.NotNull(IconLibrary.Find(icon)));
        Assert.Equal(icons.Count, icons.Distinct().Count());
    }

    [Fact]
    public void ANetworkPath_ShowsTheNetworkDrive()
    {
        Assert.Equal("network-drive", ActionLabels.IconOf(ActionLabels.ActionFor(ResourceKind.NetworkPath)));
    }
}
