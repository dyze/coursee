namespace Coursee.Core.Tests.Entries;

public sealed class EntryStoreHiddenTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "entries.json");

    private static Resource R(string name, string target, string source = "Desktop") =>
        new(name, target, ResourceKind.File, source);

    [Fact]
    public void NothingIsHidden_ByDefault()
    {
        var store = new EntryStore(FilePath);

        Assert.False(store.IsHidden(R("App", @"C:\app.exe")));
        Assert.Empty(store.Hidden);
    }

    [Fact]
    public void Hide_HidesThatEntryOnly()
    {
        var store = new EntryStore(FilePath);

        Assert.True(store.Hide(R("App", @"C:\app.exe")));

        Assert.True(store.IsHidden(R("app", @"c:\APP.exe", "desktop")));
        Assert.False(store.IsHidden(R("App (old)", @"C:\app.exe")));
        Assert.False(store.IsHidden(R("App", @"C:\app.exe", "Start Menu")));
        Assert.False(store.IsHidden(R("App", @"C:\other.exe")));
    }

    [Fact]
    public void Hide_Twice_ReturnsFalse_AndKeepsOne()
    {
        var store = new EntryStore(FilePath);
        store.Hide(R("App", @"C:\app.exe"));

        Assert.False(store.Hide(R("APP", @"C:\app.exe")));
        Assert.Single(store.Hidden);
    }

    [Fact]
    public void Hidden_ListsWhatWasHidden_InOrder()
    {
        var store = new EntryStore(FilePath);
        store.Hide(R("B", @"C:\b"));
        store.Hide(R("A", @"C:\a", "Edge"));

        Assert.Equal(
            new[] { new HiddenEntry("Desktop", "B", @"C:\b"), new HiddenEntry("Edge", "A", @"C:\a") },
            store.Hidden);
    }

    [Fact]
    public void Visible_LeavesHiddenEntriesOut_InOrder()
    {
        var store = new EntryStore(FilePath);
        store.Hide(R("B", @"C:\b"));

        var visible = store.Visible([R("A", @"C:\a"), R("B", @"C:\b"), R("C", @"C:\c")]);

        Assert.Equal(new[] { "A", "C" }, visible.Select(r => r.Name));
    }

    [Fact]
    public void Restore_ShowsTheEntryAgain()
    {
        var store = new EntryStore(FilePath);
        store.Hide(R("App", @"C:\app.exe"));

        Assert.True(store.Restore(new HiddenEntry("DESKTOP", "app", @"c:\app.exe")));

        Assert.False(store.IsHidden(R("App", @"C:\app.exe")));
        Assert.False(new EntryStore(FilePath).IsHidden(R("App", @"C:\app.exe")));
    }

    [Fact]
    public void Restore_OfAnEntryNotHidden_ReturnsFalse()
    {
        Assert.False(new EntryStore(FilePath).Restore(new HiddenEntry("Desktop", "App", @"C:\app.exe")));
    }

    [Fact]
    public void Hidden_ArePersisted_WithPinsAndAliases()
    {
        var store = new EntryStore(FilePath);
        store.TogglePin(@"C:\a");
        store.SetAlias(@"C:\a", "aa");
        store.Hide(R("App", @"C:\app.exe"));

        var reloaded = new EntryStore(FilePath);

        Assert.True(reloaded.IsHidden(R("App", @"C:\app.exe")));
        Assert.True(reloaded.IsPinned(@"C:\a"));
        Assert.Equal("aa", reloaded.AliasOf(@"C:\a"));
    }

    [Fact]
    public void FileEditedByHand_IncompleteOrRepeatedHiddenEntriesAreSkipped()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            FilePath,
            """
            {"Pinned":[],"Hidden":[
              null,
              {"Source":"Desktop","Name":"App"},
              {"Source":"Desktop","Name":"App","Target":"C:\app.exe"},
              {"Source":"desktop","Name":"APP","Target":"c:\app.exe"}
            ]}
            """);

        var store = new EntryStore(FilePath);

        Assert.Equal(new[] { new HiddenEntry("Desktop", "App", @"C:\app.exe") }, store.Hidden);
    }

    [Fact]
    public void FileWithoutHiddenEntries_MeansNothingHidden()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{"Pinned":["C:\a"]}""");

        var store = new EntryStore(FilePath);

        Assert.Empty(store.Hidden);
        Assert.True(store.IsPinned(@"C:\a"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
