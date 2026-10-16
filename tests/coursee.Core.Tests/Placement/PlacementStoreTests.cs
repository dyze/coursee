namespace Coursee.Core.Tests.Placement;

public sealed class PlacementStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "windows.json");

    private static WindowPlacement Sample(int left = 10) => new(@"\\.\DISPLAY1", new ScreenRect(left, 20, 640, 520));

    [Fact]
    public void NothingSaved_GivesNull()
    {
        Assert.Null(new PlacementStore(FilePath).Get("Main"));
    }

    [Fact]
    public void Placements_ArePersistedByWindowName()
    {
        var store = new PlacementStore(FilePath);
        store.Set("Main", Sample(10));
        store.Set("Log", Sample(30) with { Maximized = true });

        var reloaded = new PlacementStore(FilePath);

        Assert.Equal(Sample(10), reloaded.Get("Main"));
        Assert.Equal(Sample(30) with { Maximized = true }, reloaded.Get("Log"));
    }

    [Fact]
    public void WindowNamesIgnoreCase()
    {
        var store = new PlacementStore(FilePath);
        store.Set("Main", Sample());

        Assert.Equal(Sample(), store.Get("main"));
    }

    [Fact]
    public void SettingTheSamePlacement_DoesNotWriteTheFile()
    {
        var store = new PlacementStore(FilePath);
        store.Set("Main", Sample());
        File.Delete(FilePath);

        store.Set("Main", Sample());

        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void ColumnWidths_ArePersisted()
    {
        new PlacementStore(FilePath).Set("Log", Sample() with { ColumnWidths = [100, 64, 220, 600] });

        Assert.Equal(new[] { 100.0, 64, 220, 600 }, new PlacementStore(FilePath).Get("Log")?.ColumnWidths);
    }

    [Fact]
    public void SettingTheSameColumnWidths_DoesNotWriteTheFile()
    {
        var store = new PlacementStore(FilePath);
        store.Set("Log", Sample() with { ColumnWidths = [100, 64] });
        File.Delete(FilePath);

        store.Set("Log", Sample() with { ColumnWidths = [100, 64] });

        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void PlacementsDiffer_ByTheirColumnWidths()
    {
        Assert.NotEqual(Sample() with { ColumnWidths = [100, 64] }, Sample() with { ColumnWidths = [100, 70] });
        Assert.NotEqual(Sample(10), Sample(30));
        Assert.False(Sample().Equals(null));
        Assert.Equal(Sample().GetHashCode(), (Sample() with { ColumnWidths = [] }).GetHashCode());
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2]")]
    [InlineData("null")]
    public void CorruptFile_MeansNothingSaved(string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, content);

        Assert.Null(new PlacementStore(FilePath).Get("Main"));
    }

    [Fact]
    public void IncompleteEntry_IsDropped()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{\"Main\":{\"Monitor\":\"x\"},\"Log\":{\"Monitor\":\"y\",\"Bounds\":{\"Left\":1,\"Top\":2,\"Width\":3,\"Height\":4}}}");

        var store = new PlacementStore(FilePath);

        Assert.Null(store.Get("Main"));
        Assert.Equal(new WindowPlacement("y", new ScreenRect(1, 2, 3, 4)), store.Get("Log"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
