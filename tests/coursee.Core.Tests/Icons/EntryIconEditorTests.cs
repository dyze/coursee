namespace Coursee.Core.Tests.Icons;

public sealed class EntryIconEditorTests : IDisposable
{
    private const string Doom = @"C:\games\doom.exe";
    private const string Quake = @"C:\games\quake.exe";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly EntryStore _entries;
    private readonly EntryIconEditor _editor;

    public EntryIconEditorTests()
    {
        _entries = new EntryStore(Path.Combine(_directory, "entries.json"));
        _editor = new EntryIconEditor(_entries, new IconFileStore(Path.Combine(_directory, "icons")));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void SetFile_StoresTheContent_AndGivesItToTheEntry()
    {
        var icon = _editor.SetFile(Doom, [1, 2], ".png");

        Assert.Equal(EntryIconKind.File, icon.Kind);
        Assert.Equal(icon, _entries.IconOf(Doom));
        Assert.True(File.Exists(_editor.Files.PathOf(icon.Name)));
    }

    [Fact]
    public void AFileNoEntryUses_IsDeleted()
    {
        var shared = _editor.SetFile(Doom, [1], ".png");
        _editor.SetFile(Quake, [1], ".png");

        _editor.Set(Doom, EntryIcon.Library("game"));
        Assert.True(File.Exists(_editor.Files.PathOf(shared.Name)));

        _editor.Set(Quake, null);
        Assert.False(File.Exists(_editor.Files.PathOf(shared.Name)));
        Assert.Equal(EntryIcon.Library("game"), _entries.IconOf(Doom));
        Assert.Null(_entries.IconOf(Quake));
    }
}
