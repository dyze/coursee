namespace Coursee.Core.Tests.Entries;

public sealed class EntryStorePinTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "entries.json");

    [Fact]
    public void NothingIsPinned_ByDefault()
    {
        Assert.False(new EntryStore(FilePath).IsPinned(@"C:\a"));
    }

    [Fact]
    public void TogglePin_PinsThenUnpins()
    {
        var store = new EntryStore(FilePath);

        Assert.True(store.TogglePin(@"C:\a"));
        Assert.True(store.IsPinned(@"C:\a"));

        Assert.False(store.TogglePin(@"C:\a"));
        Assert.False(store.IsPinned(@"C:\a"));
    }

    [Fact]
    public void Target_IsComparedIgnoringCase()
    {
        var store = new EntryStore(FilePath);
        store.TogglePin(@"C:\Tools");

        Assert.True(store.IsPinned(@"c:\tools"));
        Assert.False(store.TogglePin(@"C:\TOOLS"));
    }

    [Fact]
    public void Pins_ArePersisted()
    {
        new EntryStore(FilePath).TogglePin(@"C:\a");

        var reloaded = new EntryStore(FilePath);

        Assert.True(reloaded.IsPinned(@"C:\a"));
        Assert.False(reloaded.IsPinned(@"C:\b"));
    }

    [Fact]
    public void CorruptFile_MeansNothingPinned()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        Assert.False(new EntryStore(FilePath).IsPinned(@"C:\a"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
