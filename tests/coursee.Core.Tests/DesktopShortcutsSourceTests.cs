namespace Coursee.Core.Tests;

public sealed class DesktopShortcutsSourceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _desktop;

    public DesktopShortcutsSourceTests()
    {
        _desktop = Path.Combine(_root, "desktop");
        Directory.CreateDirectory(_desktop);
    }

    private sealed class FakeResolver(Dictionary<string, string> map) : IShortcutResolver
    {
        public string? ResolveTarget(string shortcutPath) => map.GetValueOrDefault(shortcutPath);
    }

    private string Shortcut(string name)
    {
        var path = Path.Combine(_desktop, name + ".lnk");
        File.WriteAllText(path, string.Empty);
        return path;
    }

    [Fact]
    public void ResolvesFolderAndFileTargets()
    {
        var folder = Path.Combine(_root, "work");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(_root, "notes.txt");
        File.WriteAllText(file, "x");

        var map = new Dictionary<string, string>
        {
            [Shortcut("Work")] = folder,
            [Shortcut("Notes")] = file,
        };
        var source = new DesktopShortcutsSource([_desktop], new FakeResolver(map));

        var result = source.GetResources().OrderBy(r => r.Name).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new Resource("Notes", file, ResourceKind.File, "Desktop"), result[0]);
        Assert.Equal(new Resource("Work", folder, ResourceKind.Folder, "Desktop"), result[1]);
    }

    [Fact]
    public void SkipsShortcutsWithMissingOrUnresolvableTargets()
    {
        var map = new Dictionary<string, string>
        {
            [Shortcut("Gone")] = Path.Combine(_root, "does-not-exist"),
        };
        Shortcut("Unresolvable");
        var source = new DesktopShortcutsSource([_desktop], new FakeResolver(map));

        Assert.Empty(source.GetResources());
    }

    [Fact]
    public void IgnoresNonShortcutFiles()
    {
        File.WriteAllText(Path.Combine(_desktop, "readme.txt"), "x");
        var source = new DesktopShortcutsSource([_desktop], new FakeResolver([]));

        Assert.Empty(source.GetResources());
    }

    [Fact]
    public void MissingFolder_IsIgnored()
    {
        var source = new DesktopShortcutsSource([Path.Combine(_root, "nope")], new FakeResolver([]));

        Assert.Empty(source.GetResources());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
