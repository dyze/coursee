namespace Coursee.Core.Tests;

public sealed class RecentStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "recents.json");

    private static Resource R(string name) => new(name, @"C:\" + name, ResourceKind.Folder, "test");

    [Fact]
    public void Add_PutsMostRecentFirst()
    {
        var store = new RecentStore(FilePath);

        store.Add(R("a"));
        store.Add(R("b"));

        Assert.Equal(new[] { "b", "a" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void Add_MovesDuplicateToFront_WithoutDuplicating()
    {
        var store = new RecentStore(FilePath);

        store.Add(R("a"));
        store.Add(R("b"));
        store.Add(R("a"));

        Assert.Equal(new[] { "a", "b" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void Add_TrimsToCapacity()
    {
        var store = new RecentStore(FilePath, capacity: 2);

        store.Add(R("a"));
        store.Add(R("b"));
        store.Add(R("c"));

        Assert.Equal(new[] { "c", "b" }, store.Items.Select(r => r.Name));
    }

    [Fact]
    public void Items_ArePersistedAcrossInstances()
    {
        new RecentStore(FilePath).Add(R("a"));

        var reloaded = new RecentStore(FilePath);

        Assert.Equal(new[] { "a" }, reloaded.Items.Select(r => r.Name));
    }

    [Fact]
    public void Remove_DropsTheItem_AndIsPersisted()
    {
        var store = new RecentStore(FilePath);
        store.Add(R("a"));
        store.Add(R("b"));

        Assert.True(store.Remove(@"c:\A"));

        Assert.Equal(new[] { "b" }, store.Items.Select(r => r.Name));
        Assert.Equal(new[] { "b" }, new RecentStore(FilePath).Items.Select(r => r.Name));
    }

    [Fact]
    public void Remove_UnknownTarget_ReturnsFalse()
    {
        var store = new RecentStore(FilePath);
        store.Add(R("a"));

        Assert.False(store.Remove(@"C:\nope"));
        Assert.Single(store.Items);
    }

    [Fact]
    public void CorruptFile_MeansEmptyList()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        var store = new RecentStore(FilePath);

        Assert.Empty(store.Items);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
