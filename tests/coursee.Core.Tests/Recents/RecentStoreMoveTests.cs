namespace Coursee.Core.Tests.Recents;

public sealed class RecentStoreMoveTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ManualClock _clock = new();

    private string FilePath => Path.Combine(_directory, "recents.json");

    private static Resource R(string name, string target) => new(name, target, ResourceKind.Url, "Custom");

    private RecentStore NewStore() => new(FilePath, clock: _clock);

    [Fact]
    public void Move_ReplacesTheItem_AndKeepsItsHistory()
    {
        var store = NewStore();
        store.Add(R("Wiki", "https://old"));
        store.Add(R("Wiki", "https://old"));
        var bonus = store.UsageBonus("https://old");

        Assert.True(store.Move("HTTPS://OLD", R("Team wiki", "https://new")));

        var reloaded = NewStore();
        Assert.Equal(R("Team wiki", "https://new"), Assert.Single(reloaded.Items));
        Assert.Equal(bonus, reloaded.UsageBonus("https://new"));
        Assert.Equal(0, reloaded.UsageBonus("https://old"));
    }

    [Fact]
    public void Move_KeepsThePlaceInTheList()
    {
        var store = NewStore();
        store.Add(R("A", "https://a"));
        _clock.Advance(TimeSpan.FromSeconds(1));
        store.Add(R("B", "https://b"));

        store.Move("https://a", R("A2", "https://a2"));

        Assert.Equal(new[] { "B", "A2" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void Move_OntoAnItemWithTheNewTarget_MergesThem()
    {
        var store = NewStore();
        store.Add(R("Old", "https://old"));
        store.Add(R("New", "https://new"));
        store.Add(R("Other", "https://other"));

        store.Move("https://old", R("Wiki", "https://new"));

        Assert.Equal(new[] { "Other", "Wiki" }, store.Items.Select(r => r.Name).Order());
        Assert.Equal(20, store.UsageBonus("https://new"));
    }

    [Fact]
    public void Move_MergedHistory_KeepsTheLastOpens()
    {
        var store = NewStore();
        for (var i = 0; i < Frecency.MaxOpens; i++)
        {
            store.Add(R("Old", "https://old"));
            store.Add(R("New", "https://new"));
        }

        store.Move("https://old", R("Wiki", "https://new"));

        Assert.Equal(Frecency.MaxBonus, store.UsageBonus("https://new"));
        Assert.Equal("Wiki", Assert.Single(store.Items).Name);
    }

    [Fact]
    public void Move_OfAnItemNotInTheList_ReturnsFalse()
    {
        var store = NewStore();
        store.Add(R("A", "https://a"));

        Assert.False(store.Move("https://missing", R("B", "https://b")));
        Assert.Equal("A", Assert.Single(store.Items).Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
