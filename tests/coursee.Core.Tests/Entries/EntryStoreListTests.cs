namespace Coursee.Core.Tests.Entries;

public sealed class EntryStoreListTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "entries.json");

    [Fact]
    public void NothingSet_ListsNothing()
    {
        var store = new EntryStore(FilePath);

        Assert.Empty(store.Pinned);
        Assert.Empty(store.Aliases);
    }

    [Fact]
    public void Pinned_ListsTheTargets_Alphabetically()
    {
        var store = new EntryStore(FilePath);
        store.TogglePin(@"C:\b");
        store.TogglePin(@"C:\A");

        Assert.Equal(new[] { @"C:\A", @"C:\b" }, store.Pinned);
    }

    [Fact]
    public void Aliases_ListEachAliasWithItsTarget_Alphabetically()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(@"C:\word.exe", "w");
        store.SetAlias(@"C:\firefox.exe", "FF");

        Assert.Equal(
            new[] { new EntryAlias("FF", @"C:\firefox.exe"), new EntryAlias("w", @"C:\word.exe") },
            store.Aliases);
    }

    [Fact]
    public void Lists_AreCopies()
    {
        var store = new EntryStore(FilePath);
        var pinned = store.Pinned;
        var aliases = store.Aliases;

        store.TogglePin(@"C:\a");
        store.SetAlias(@"C:\a", "a");

        Assert.Empty(pinned);
        Assert.Empty(aliases);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
