namespace Coursee.Core.Tests.Recents;

public class CurrentRecentsTests
{
    [Fact]
    public void ARecentItem_ShowsAsItsSourceListsItNow()
    {
        var saved = new Resource("Doom", "steam://rungameid/1", ResourceKind.Game, "Steam");
        var now = saved with { Name = "DOOM", IconPath = @"C:\steam\1.jpg" };

        Assert.Equal(new[] { now }, CurrentRecents.Of([saved], [now]));
    }

    [Fact]
    public void SourceAndTarget_IgnoreCase_AndTheOrderIsKept()
    {
        var first = new Resource("A", @"C:\a.exe", ResourceKind.File, "Desktop");
        var second = new Resource("B", @"C:\b.exe", ResourceKind.File, "Desktop");
        var current = second with { Target = @"C:\B.EXE", Source = "desktop", Name = "B now" };

        Assert.Equal(new[] { first, current }, CurrentRecents.Of([first, second], [current]));
    }

    [Fact]
    public void AnItemNoLongerListed_OrFromAnotherSource_IsKeptAsSaved()
    {
        var saved = new Resource("Tool", @"C:\tool.exe", ResourceKind.File, "Desktop");
        var elsewhere = saved with { Source = "Start Menu", Name = "Other" };

        Assert.Equal(new[] { saved }, CurrentRecents.Of([saved], [elsewhere]));
    }

    [Fact]
    public void TheFirstOfTwoLoadedItems_Wins()
    {
        var saved = new Resource("Tool", @"C:\tool.exe", ResourceKind.File, "Desktop");
        var first = saved with { Name = "First" };

        Assert.Equal(new[] { first }, CurrentRecents.Of([saved], [first, saved with { Name = "Second" }]));
    }
}
