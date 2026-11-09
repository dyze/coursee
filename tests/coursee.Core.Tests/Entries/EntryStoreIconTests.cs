namespace Coursee.Core.Tests.Entries;

public sealed class EntryStoreIconTests : IDisposable
{
    private const string Doom = @"C:\games\doom.exe";
    private const string Quake = @"C:\games\quake.exe";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "entries.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void NoIcon_ByDefault()
    {
        Assert.Null(new EntryStore(FilePath).IconOf(Doom));
        Assert.Empty(new EntryStore(FilePath).Icons);
    }

    [Fact]
    public void SetIcon_IsKeptByTarget_IgnoringCase_AndSaved()
    {
        var store = new EntryStore(FilePath);
        store.SetIcon(Quake, EntryIcon.File("ab.png"));
        store.SetIcon(Doom, EntryIcon.Library("game"));

        var reloaded = new EntryStore(FilePath);

        Assert.Equal(EntryIcon.Library("game"), reloaded.IconOf(Doom.ToUpperInvariant()));
        Assert.Equal(
            new[] { new EntryIconChoice(Doom, EntryIcon.Library("game")), new EntryIconChoice(Quake, EntryIcon.File("ab.png")) },
            reloaded.Icons);
    }

    [Fact]
    public void SetIcon_Null_RemovesIt()
    {
        var store = new EntryStore(FilePath);
        store.SetIcon(Doom, EntryIcon.Library("game"));

        store.SetIcon(Doom, null);

        Assert.Null(new EntryStore(FilePath).IconOf(Doom));
    }

    [Fact]
    public void Move_TakesTheIconToTheNewTarget()
    {
        var store = new EntryStore(FilePath);
        store.SetIcon(Doom, EntryIcon.Library("game"));

        store.Move(
            new Resource("Doom", Doom, ResourceKind.File, CustomEntry.SourceName),
            new Resource("Doom", Quake, ResourceKind.File, CustomEntry.SourceName));

        Assert.Null(store.IconOf(Doom));
        Assert.Equal(EntryIcon.Library("game"), store.IconOf(Quake));
    }

    [Fact]
    public void AFileEditedByHand_KeepsOnlyRealIcons()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, $$"""
            { "Pinned": [], "Icons": { "{{Doom.Replace("\\", "\\\\")}}": "library:game", "a": "nonsense", " ": "library:star" } }
            """);

        var store = new EntryStore(FilePath);

        Assert.Equal(new EntryIconChoice(Doom, EntryIcon.Library("game")), Assert.Single(store.Icons));
    }
}
