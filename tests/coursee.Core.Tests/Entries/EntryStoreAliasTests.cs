namespace Coursee.Core.Tests.Entries;

public sealed class EntryStoreAliasTests : IDisposable
{
    private const string Firefox = @"C:\apps\firefox.exe";
    private const string Edge = @"C:\apps\msedge.exe";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "entries.json");

    [Fact]
    public void NoAlias_ByDefault()
    {
        var store = new EntryStore(FilePath);

        Assert.Null(store.AliasOf(Firefox));
        Assert.Null(store.TargetOfAlias("ff"));
    }

    [Fact]
    public void SetAlias_FindsTheEntry_ByItsAlias()
    {
        var store = new EntryStore(FilePath);

        Assert.Null(store.SetAlias(Firefox, "ff"));

        Assert.Equal("ff", store.AliasOf(Firefox));
        Assert.Equal(Firefox, store.TargetOfAlias("ff"));
    }

    [Theory]
    [InlineData("FF")]
    [InlineData(" ff ")]
    public void TargetOfAlias_IgnoresCaseAndSpaces(string typed)
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "ff");

        Assert.Equal(Firefox, store.TargetOfAlias(typed));
    }

    [Fact]
    public void TargetOfAlias_IgnoresAccents()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "été");

        Assert.Equal(Firefox, store.TargetOfAlias("ETE"));
    }

    [Fact]
    public void TargetOfAlias_NeedsTheWholeAlias()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "ff");

        Assert.Null(store.TargetOfAlias("f"));
        Assert.Null(store.TargetOfAlias("ffx"));
        Assert.Null(store.TargetOfAlias("ff docs"));
        Assert.Null(store.TargetOfAlias("  "));
    }

    [Fact]
    public void SetAlias_IsTrimmed()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "  ff ");

        Assert.Equal("ff", store.AliasOf(Firefox));
    }

    [Fact]
    public void SetAlias_ReplacesTheEntrysOldAlias()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "ff");
        store.SetAlias(Firefox, "fx");

        Assert.Equal("fx", store.AliasOf(Firefox));
        Assert.Null(store.TargetOfAlias("ff"));
    }

    [Fact]
    public void SetAlias_UsedByAnotherEntry_MovesIt()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "web");

        var movedFrom = store.SetAlias(Edge, "WEB");

        Assert.Equal(Firefox, movedFrom);
        Assert.Equal(Edge, store.TargetOfAlias("web"));
        Assert.Null(store.AliasOf(Firefox));
    }

    [Fact]
    public void SetAlias_SameEntryOtherCase_IsNoMove()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "ff");

        Assert.Null(store.SetAlias(@"c:\APPS\firefox.exe", "FF"));
        Assert.Equal("FF", store.AliasOf(Firefox));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyAlias_RemovesIt(string empty)
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "ff");

        store.SetAlias(Firefox, empty);

        Assert.Null(store.AliasOf(Firefox));
        Assert.Null(store.TargetOfAlias("ff"));
    }

    [Fact]
    public void Aliases_ArePersisted_WithThePins()
    {
        var store = new EntryStore(FilePath);
        store.SetAlias(Firefox, "ff");
        store.TogglePin(Edge);

        var reloaded = new EntryStore(FilePath);

        Assert.Equal(Firefox, reloaded.TargetOfAlias("ff"));
        Assert.True(reloaded.IsPinned(Edge));
    }

    [Fact]
    public void FileWithPinsOnly_IsRead()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{"Pinned":["C:\\a"]}""");

        var store = new EntryStore(FilePath);

        Assert.True(store.IsPinned(@"C:\a"));
        Assert.Null(store.TargetOfAlias("a"));
    }

    [Fact]
    public void RepeatedAliasInTheFile_FirstOneWins()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{"Aliases":{"ff":"C:\\a","FF":"C:\\b"," ":"C:\\c"}}""");

        var store = new EntryStore(FilePath);

        Assert.Equal(@"C:\a", store.TargetOfAlias("ff"));
        Assert.Null(store.AliasOf(@"C:\b"));
        Assert.Null(store.AliasOf(@"C:\c"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
