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
        public List<string> Calls { get; } = [];

        public string? ResolveTarget(string shortcutPath)
        {
            Calls.Add(shortcutPath);
            return map.GetValueOrDefault(shortcutPath);
        }
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

        var work = Shortcut("Work");
        var notes = Shortcut("Notes");
        var map = new Dictionary<string, string> { [work] = folder, [notes] = file };
        var source = new DesktopShortcutsSource([_desktop], new FakeResolver(map));

        var result = source.GetResources().OrderBy(r => r.Name).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new Resource("Notes", file, ResourceKind.File, "Desktop", notes), result[0]);
        Assert.Equal(new Resource("Work", folder, ResourceKind.Folder, "Desktop", work), result[1]);
    }

    [Fact]
    public void Shortcuts_AreOpenedThroughTheLnkFile_ToKeepTheirArguments()
    {
        var file = Path.Combine(_root, "app.exe");
        File.WriteAllText(file, "x");
        var shortcut = Shortcut("App");
        var source = new DesktopShortcutsSource([_desktop], new FakeResolver(new() { [shortcut] = file }));

        var resource = Assert.Single(source.GetResources());

        Assert.Equal(file, resource.Target);
        Assert.Equal(shortcut, resource.OpenTarget);
    }

    [Fact]
    public void UnchangedShortcuts_AreResolvedOnlyOnce()
    {
        var file = Path.Combine(_root, "notes.txt");
        File.WriteAllText(file, "x");
        var shortcut = Shortcut("Notes");
        var resolver = new FakeResolver(new() { [shortcut] = file });
        var source = new DesktopShortcutsSource([_desktop], resolver);

        source.GetResources();
        source.GetResources();

        Assert.Single(resolver.Calls);
    }

    [Fact]
    public void ChangedShortcut_IsResolvedAgain()
    {
        var first = Path.Combine(_root, "first.txt");
        var second = Path.Combine(_root, "second.txt");
        File.WriteAllText(first, "x");
        File.WriteAllText(second, "x");
        var shortcut = Shortcut("Notes");
        var map = new Dictionary<string, string> { [shortcut] = first };
        var resolver = new FakeResolver(map);
        var source = new DesktopShortcutsSource([_desktop], resolver);
        source.GetResources();

        map[shortcut] = second;
        File.SetLastWriteTimeUtc(shortcut, File.GetLastWriteTimeUtc(shortcut).AddMinutes(1));

        Assert.Equal(second, Assert.Single(source.GetResources()).Target);
        Assert.Equal(2, resolver.Calls.Count);
    }

    [Fact]
    public void DeletedShortcut_Disappears()
    {
        var file = Path.Combine(_root, "notes.txt");
        File.WriteAllText(file, "x");
        var shortcut = Shortcut("Notes");
        var source = new DesktopShortcutsSource([_desktop], new FakeResolver(new() { [shortcut] = file }));
        source.GetResources();

        File.Delete(shortcut);

        Assert.Empty(source.GetResources());
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
