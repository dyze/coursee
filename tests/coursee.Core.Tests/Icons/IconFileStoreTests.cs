namespace Coursee.Core.Tests.Icons;

public sealed class IconFileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string Folder => Path.Combine(_directory, "icons");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(@"C:\a.ICO", IconFileType.Image)]
    [InlineData("a.png", IconFileType.Image)]
    [InlineData("a.jpg", IconFileType.Image)]
    [InlineData("a.jpeg", IconFileType.Image)]
    [InlineData("a.bmp", IconFileType.Image)]
    [InlineData("a.gif", IconFileType.Image)]
    [InlineData("a.exe", IconFileType.Program)]
    [InlineData("a.dll", IconFileType.Program)]
    [InlineData("a.svg", IconFileType.Svg)]
    [InlineData("a.txt", null)]
    [InlineData("noextension", null)]
    public void TypeOf_IsKnownByTheExtension(string path, IconFileType? expected)
    {
        Assert.Equal(expected, IconFileStore.TypeOf(path));
    }

    [Fact]
    public void Add_WritesTheContentOnce_NamedByIt()
    {
        var store = new IconFileStore(Folder);

        var name = store.Add([1, 2, 3], ".PNG");
        var again = store.Add([1, 2, 3], ".png");
        var other = store.Add([4], ".png");

        Assert.Equal(name, again);
        Assert.NotEqual(name, other);
        Assert.EndsWith(".png", name, StringComparison.Ordinal);
        Assert.Equal(16 + ".png".Length, name.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(store.PathOf(name)));
        Assert.Equal(2, Directory.GetFiles(Folder).Length);
    }

    [Fact]
    public void RemoveUnused_DeletesTheOthers()
    {
        var store = new IconFileStore(Folder);
        var kept = store.Add([1], ".png");
        var dropped = store.Add([2], ".svg");

        Assert.Equal(1, store.RemoveUnused([kept.ToUpperInvariant()]));

        Assert.True(File.Exists(store.PathOf(kept)));
        Assert.False(File.Exists(store.PathOf(dropped)));
    }

    [Fact]
    public void RemoveUnused_WithoutFolder_DoesNothing()
    {
        Assert.Equal(0, new IconFileStore(Folder).RemoveUnused([]));
    }
}
