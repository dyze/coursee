namespace Coursee.Core.Tests.Recents;

public sealed class RecentStoreUsageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ManualClock _clock = new();

    private string FilePath => Path.Combine(_directory, "recents.json");

    private static Resource R(string name) => new(name, @"C:\" + name, ResourceKind.Folder, "test");

    private RecentStore NewStore(int capacity = 20) => new(FilePath, capacity, _clock);

    [Fact]
    public void OftenOpened_IsListedBeforeOpenedOnce()
    {
        var store = NewStore();
        store.Add(R("often"));
        store.Add(R("often"));
        store.Add(R("once"));

        Assert.Equal(new[] { "often", "once" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void RecentOpen_CountsMoreThanOldOnes()
    {
        var store = NewStore();
        store.Add(R("old"));
        store.Add(R("old"));
        _clock.Advance(TimeSpan.FromDays(40));
        store.Add(R("new"));

        // old: 1 + 1, new: 10.
        Assert.Equal(new[] { "new", "old" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void SameScore_LastOpenedFirst()
    {
        var store = NewStore();
        store.Add(R("a"));
        store.Add(R("b"));

        Assert.Equal(new[] { "b", "a" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void Full_DropsTheLeastRecentlyOpened_EvenIfOftenUsed()
    {
        var store = NewStore(capacity: 2);
        store.Add(R("often"));
        store.Add(R("often"));
        store.Add(R("b"));
        store.Add(R("c"));

        Assert.Equal(new[] { "c", "b" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void UsageBonus_GrowsWithOpens_AndIsCapped()
    {
        var store = NewStore();
        store.Add(R("a"));
        Assert.Equal(10, store.UsageBonus(@"C:\a"));

        store.Add(R("a"));
        store.Add(R("a"));
        Assert.Equal(Frecency.MaxBonus, store.UsageBonus(@"c:\A"));
    }

    [Fact]
    public void UsageBonus_OfUnknownTarget_IsZero()
    {
        Assert.Equal(0, NewStore().UsageBonus(@"C:\nope"));
    }

    [Fact]
    public void Remove_ForgetsTheHistory()
    {
        var store = NewStore();
        store.Add(R("a"));
        store.Add(R("a"));
        store.Remove(@"C:\a");
        store.Add(R("a"));

        Assert.Equal(10, store.UsageBonus(@"C:\a"));
    }

    [Fact]
    public void OnlyTheLastOpensAreKept()
    {
        var store = NewStore();
        for (var i = 0; i < Frecency.MaxOpens; i++)
        {
            store.Add(R("a"));
        }

        _clock.Advance(TimeSpan.FromDays(60));
        store.Add(R("a"));

        // The oldest open is dropped: 9 old ones (1 point each) and the new one (10).
        Assert.Equal(19, store.UsageBonus(@"C:\a"));
    }

    [Fact]
    public void History_IsPersisted()
    {
        var store = NewStore();
        store.Add(R("a"));
        store.Add(R("b"));
        store.Add(R("b"));

        var reloaded = NewStore();

        Assert.Equal(new[] { "b", "a" }, reloaded.Items.Select(r => r.Name));
        Assert.Equal(20, reloaded.UsageBonus(@"C:\b"));
    }

    [Fact]
    public void FileOfVersion04_IsRead_InItsOrder_AsOpenedOnce()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            FilePath,
            """[{"Name":"b","Target":"C:\\b","Kind":1,"Source":"test"},{"Name":"a","Target":"C:\\a","Kind":1,"Source":"test"}]""");

        var store = NewStore();

        Assert.Equal(new[] { "b", "a" }, store.Items.Select(r => r.Name));
        Assert.Equal(10, store.UsageBonus(@"C:\a"));
    }

    [Fact]
    public void FileOfVersion04_IsSavedInTheNewFormat()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """[{"Name":"a","Target":"C:\\a","Kind":1,"Source":"test"}]""");

        NewStore().Add(R("b"));

        Assert.Equal(new[] { "b", "a" }, NewStore().Items.Select(r => r.Name));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
