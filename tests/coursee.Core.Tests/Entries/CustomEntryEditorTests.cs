namespace Coursee.Core.Tests.Entries;

public sealed class CustomEntryEditorTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly CustomEntryStore _custom;
    private readonly EntryStore _entries;
    private readonly RecentStore _recents;
    private readonly CustomEntryEditor _editor;

    public CustomEntryEditorTests()
    {
        _custom = new CustomEntryStore(Path.Combine(_directory, "custom.json"));
        _entries = new EntryStore(Path.Combine(_directory, "entries.json"));
        _recents = new RecentStore(Path.Combine(_directory, "recents.json"));

        // No file system: a path is a folder when it ends with a slash.
        _editor = new CustomEntryEditor(_custom, _entries, _recents, entry => entry.ToResource(path => path.EndsWith('\\'), _ => false));
    }

    private static Resource Typed(string text) => FallbackClassifier.Classify(text, _ => false, _ => false)!;

    [Fact]
    public void Create_FromTypedText_UsesWhatEnterOpens()
    {
        var entry = _editor.Create(Typed("www.example.com"), "Example");

        Assert.Equal(new CustomEntry("Example", "https://www.example.com"), entry);
        Assert.Equal(entry, Assert.Single(_custom.All));
    }

    [Fact]
    public void Create_FromAnEntryOfASource_Throws()
    {
        var shortcut = new Resource("App", @"C:\app.exe", ResourceKind.File, "Desktop");

        Assert.Throws<ArgumentException>(() => _editor.Create(shortcut, "App"));
        Assert.Empty(_custom.All);
    }

    [Fact]
    public void Edit_ChangesTheNameAndTarget()
    {
        var entry = _custom.Add("Wiki", "https://old");

        var edited = _editor.Edit(entry, "Team wiki", "www.wiki.example");

        Assert.Equal(new CustomEntry("Team wiki", "https://www.wiki.example"), edited);
        Assert.Equal(edited, Assert.Single(_custom.All));
    }

    [Fact]
    public void Edit_KeepsThePinAliasHiddenStateAndHistory()
    {
        var entry = _custom.Add("Wiki", "https://old");
        var before = entry.ToResource();
        _entries.TogglePin(before.Target);
        _entries.SetAlias(before.Target, "w");
        _entries.Hide(before);
        _recents.Add(before);

        _editor.Edit(entry, "Tools", @"C:\tools\");

        var after = new Resource("Tools", @"C:\tools\", ResourceKind.Folder, CustomEntry.SourceName);
        Assert.True(_entries.IsPinned(after.Target));
        Assert.Equal("w", _entries.AliasOf(after.Target));
        Assert.True(_entries.IsHidden(after));
        Assert.Equal(after, Assert.Single(_recents.Items));
    }

    [Fact]
    public void Edit_ToTheTargetOfAnotherEntry_ChangesNothing()
    {
        var wiki = _custom.Add("Wiki", "https://wiki");
        _custom.Add("Other", "https://other");
        _entries.TogglePin("https://wiki");

        Assert.Null(_editor.Edit(wiki, "Wiki", "https://other"));

        Assert.True(_entries.IsPinned("https://wiki"));
        Assert.Equal(new[] { "https://wiki", "https://other" }, _custom.All.Select(entry => entry.Target));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Edit_WithoutATarget_Throws(string target)
    {
        var entry = _custom.Add("Wiki", "https://wiki");

        Assert.Throws<ArgumentException>(() => _editor.Edit(entry, "Wiki", target));
        Assert.Equal(entry, Assert.Single(_custom.All));
    }

    [Fact]
    public void DefaultResources_ComeFromTheEntry()
    {
        var editor = new CustomEntryEditor(_custom, _entries, _recents);
        var entry = _custom.Add("Wiki", "https://old");

        Assert.Equal(new CustomEntry("Wiki", "https://new"), editor.Edit(entry, "Wiki", "https://new"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
