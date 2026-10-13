namespace Coursee.Core.Tests.Sources;

public sealed class CustomSourceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "custom.json");

    [Fact]
    public void DescribesItself()
    {
        var source = new CustomSource(new CustomEntryStore(FilePath));

        Assert.Equal(CustomEntry.SourceName, source.Name);
        Assert.Equal("Custom entries", source.DisplayName);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.Custom, source.Icon);
    }

    [Fact]
    public void GetResources_ListsTheEntries_InOrder()
    {
        var store = new CustomEntryStore(FilePath);
        store.Add("Wiki", "https://wiki");
        store.Add("Ping", "ping localhost");

        var resources = new CustomSource(store).GetResources();

        Assert.Equal(
            new[]
            {
                new Resource("Wiki", "https://wiki", ResourceKind.Url, CustomEntry.SourceName),
                new Resource("Ping", "ping localhost", ResourceKind.Command, CustomEntry.SourceName),
            },
            resources);
    }

    [Fact]
    public void GetResources_SeesEntriesAddedLater()
    {
        var store = new CustomEntryStore(FilePath);
        var source = new CustomSource(store);
        Assert.Empty(source.GetResources());

        store.Add("Wiki", "https://wiki");

        Assert.Single(source.GetResources());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
