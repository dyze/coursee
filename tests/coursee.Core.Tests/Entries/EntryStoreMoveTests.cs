namespace Coursee.Core.Tests.Entries;

public sealed class EntryStoreMoveTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "entries.json");

    private static Resource R(string name, string target) => new(name, target, ResourceKind.Url, CustomEntry.SourceName);

    [Fact]
    public void Move_MovesThePinAndTheAlias_ToTheNewTarget()
    {
        var store = new EntryStore(FilePath);
        store.TogglePin("https://old");
        store.SetAlias("https://old", "w");

        store.Move(R("Wiki", "https://old"), R("Wiki", "https://new"));

        var reloaded = new EntryStore(FilePath);
        Assert.True(reloaded.IsPinned("https://new"));
        Assert.False(reloaded.IsPinned("https://old"));
        Assert.Equal("w", reloaded.AliasOf("https://new"));
        Assert.Null(reloaded.AliasOf("https://old"));
    }

    [Fact]
    public void Move_OfAnEntryWithNothingSet_SetsNothing()
    {
        var store = new EntryStore(FilePath);

        store.Move(R("Wiki", "https://old"), R("Wiki", "https://new"));

        Assert.False(store.IsPinned("https://new"));
        Assert.Null(store.AliasOf("https://new"));
        Assert.Empty(store.Hidden);
    }

    [Fact]
    public void Move_KeepsAHiddenEntryHidden_UnderItsNewNameAndTarget()
    {
        var store = new EntryStore(FilePath);
        store.Hide(R("Wiki", "https://old"));

        store.Move(R("Wiki", "https://old"), R("Team wiki", "https://new"));

        Assert.Equal(new[] { new HiddenEntry(CustomEntry.SourceName, "Team wiki", "https://new") }, store.Hidden);
    }

    [Fact]
    public void Move_ToAHiddenEntry_HidesItOnce()
    {
        var store = new EntryStore(FilePath);
        store.Hide(R("Wiki", "https://old"));
        store.Hide(R("Wiki", "https://new"));

        store.Move(R("Wiki", "https://old"), R("Wiki", "https://new"));

        Assert.Equal(new[] { new HiddenEntry(CustomEntry.SourceName, "Wiki", "https://new") }, store.Hidden);
    }

    [Fact]
    public void Move_WithTheSameTarget_KeepsThePinAndAlias()
    {
        var store = new EntryStore(FilePath);
        store.TogglePin("https://wiki");
        store.SetAlias("https://wiki", "w");

        store.Move(R("Wiki", "https://wiki"), R("Team wiki", "HTTPS://WIKI"));

        Assert.True(store.IsPinned("https://wiki"));
        Assert.Equal("w", store.AliasOf("https://wiki"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
