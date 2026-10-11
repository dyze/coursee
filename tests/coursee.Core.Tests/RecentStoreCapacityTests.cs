namespace Coursee.Core.Tests;

public sealed class RecentStoreCapacityTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "recents.json");

    private static Resource R(string name) => new(name, @"C:\" + name, ResourceKind.Folder, "test");

    [Fact]
    public void SetCapacity_DropsTheOldestItems()
    {
        var store = new RecentStore(FilePath, capacity: 5);
        foreach (var name in new[] { "a", "b", "c", "d" })
        {
            store.Add(R(name));
        }

        store.SetCapacity(2);

        Assert.Equal(new[] { "d", "c" }, store.Items.Select(r => r.Name));
        Assert.Equal(new[] { "d", "c" }, new RecentStore(FilePath).Items.Select(r => r.Name));
    }

    [Fact]
    public void SetCapacity_AppliesToLaterAdds()
    {
        var store = new RecentStore(FilePath, capacity: 2);
        store.SetCapacity(3);

        foreach (var name in new[] { "a", "b", "c", "d" })
        {
            store.Add(R(name));
        }

        Assert.Equal(new[] { "d", "c", "b" }, store.Items.Select(r => r.Name));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
