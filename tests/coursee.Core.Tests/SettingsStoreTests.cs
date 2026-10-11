namespace Coursee.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void EverySourceIsEnabledByDefault()
    {
        var store = new SettingsStore(FilePath);

        Assert.True(store.IsSourceEnabled("Firefox"));
    }

    [Fact]
    public void DisablingASource_IsPersisted()
    {
        new SettingsStore(FilePath).SetSourceEnabled("Firefox", false);

        var reloaded = new SettingsStore(FilePath);

        Assert.False(reloaded.IsSourceEnabled("Firefox"));
        Assert.True(reloaded.IsSourceEnabled("Desktop"));
    }

    [Fact]
    public void ReEnablingASource_IsPersisted()
    {
        var store = new SettingsStore(FilePath);
        store.SetSourceEnabled("Firefox", false);
        store.SetSourceEnabled("Firefox", true);

        Assert.True(new SettingsStore(FilePath).IsSourceEnabled("Firefox"));
    }

    [Fact]
    public void SourceNamesAreCaseInsensitive()
    {
        var store = new SettingsStore(FilePath);
        store.SetSourceEnabled("Firefox", false);

        Assert.False(store.IsSourceEnabled("firefox"));
    }

    [Fact]
    public void CorruptFile_MeansDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        Assert.True(new SettingsStore(FilePath).IsSourceEnabled("Firefox"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
