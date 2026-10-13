namespace Coursee.Core.Tests.Entries;

public sealed class CustomEntryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "custom.json");

    [Fact]
    public void NoFile_MeansNoEntries()
    {
        Assert.Empty(new CustomEntryStore(FilePath).All);
    }

    [Fact]
    public void Add_KeepsTheOrder_AndIsPersisted()
    {
        var store = new CustomEntryStore(FilePath);
        store.Add("Wiki", "https://wiki");
        store.Add("Tools", @"C:\tools");

        var expected = new[] { new CustomEntry("Wiki", "https://wiki"), new CustomEntry("Tools", @"C:\tools") };
        Assert.Equal(expected, store.All);
        Assert.Equal(expected, new CustomEntryStore(FilePath).All);
    }

    [Fact]
    public void Add_TrimsSpaces_AndAnEmptyNameBecomesTheTarget()
    {
        var store = new CustomEntryStore(FilePath);

        Assert.Equal(new CustomEntry("Wiki", "https://wiki"), store.Add("  Wiki ", " https://wiki "));
        Assert.Equal(new CustomEntry(@"C:\tools", @"C:\tools"), store.Add("  ", @"C:\tools"));
    }

    [Fact]
    public void Add_WithAKnownTarget_RenamesThatEntry()
    {
        var store = new CustomEntryStore(FilePath);
        store.Add("Wiki", "https://wiki");
        store.Add("Other", "https://other");

        store.Add("Team wiki", "HTTPS://WIKI");

        Assert.Equal(new[] { "Team wiki", "Other" }, store.All.Select(entry => entry.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_WithoutATarget_Throws(string target)
    {
        Assert.Throws<ArgumentException>(() => new CustomEntryStore(FilePath).Add("Name", target));
    }

    [Fact]
    public void Update_ReplacesTheEntryInPlace()
    {
        var store = new CustomEntryStore(FilePath);
        var wiki = store.Add("Wiki", "https://wiki");
        store.Add("Other", "https://other");

        var updated = store.Update(wiki, "New wiki", "https://new-wiki");

        Assert.Equal(new CustomEntry("New wiki", "https://new-wiki"), updated);
        Assert.Equal(new[] { "https://new-wiki", "https://other" }, new CustomEntryStore(FilePath).All.Select(entry => entry.Target));
    }

    [Fact]
    public void Update_CanChangeTheCaseOfItsOwnTarget()
    {
        var store = new CustomEntryStore(FilePath);
        var wiki = store.Add("Wiki", "https://wiki");

        Assert.NotNull(store.Update(wiki, "Wiki", "https://WIKI"));
        Assert.Equal("https://WIKI", Assert.Single(store.All).Target);
    }

    [Fact]
    public void Update_ToTheTargetOfAnotherEntry_ChangesNothing()
    {
        var store = new CustomEntryStore(FilePath);
        var wiki = store.Add("Wiki", "https://wiki");
        store.Add("Other", "https://other");

        Assert.Null(store.Update(wiki, "Wiki", "https://other"));
        Assert.Equal(new[] { "Wiki", "Other" }, store.All.Select(entry => entry.Name));
    }

    [Fact]
    public void Update_OfAnEntryThatIsGone_ReturnsNull()
    {
        var store = new CustomEntryStore(FilePath);

        Assert.Null(store.Update(new CustomEntry("Wiki", "https://wiki"), "Wiki", "https://new"));
        Assert.Empty(store.All);
    }

    [Fact]
    public void Delete_RemovesTheEntry_AndIsPersisted()
    {
        var store = new CustomEntryStore(FilePath);
        var wiki = store.Add("Wiki", "https://wiki");
        store.Add("Other", "https://other");

        Assert.True(store.Delete(wiki));

        Assert.Equal("Other", Assert.Single(new CustomEntryStore(FilePath).All).Name);
    }

    [Fact]
    public void Delete_OfAnEntryThatIsGone_ReturnsFalse()
    {
        Assert.False(new CustomEntryStore(FilePath).Delete(new CustomEntry("Wiki", "https://wiki")));
    }

    [Fact]
    public void All_IsACopy()
    {
        var store = new CustomEntryStore(FilePath);
        var before = store.All;

        store.Add("Wiki", "https://wiki");

        Assert.Empty(before);
    }

    [Fact]
    public void CorruptFile_MeansNoEntries()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        Assert.Empty(new CustomEntryStore(FilePath).All);
    }

    [Fact]
    public void FileEditedByHand_BadOrRepeatedEntriesAreSkipped()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            FilePath,
            """
            [
              null,
              {"Name":"No target"},
              {"Name":"Blank","Target":"  "},
              {"Target":"https://no-name"},
              {"Name":" Wiki ","Target":" https://wiki "},
              {"Name":"Again","Target":"HTTPS://WIKI"}
            ]
            """);

        var store = new CustomEntryStore(FilePath);

        Assert.Equal(new[] { new CustomEntry("Wiki", "https://wiki") }, store.All);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
