namespace Coursee.Core.Tests.Sources;

public sealed class ShortcutsSourceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _desktop;

    public ShortcutsSourceTests()
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
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver(map));

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
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver(new() { [shortcut] = file }));

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
        var source = ShortcutsSource.Desktop([_desktop], resolver);

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
        var source = ShortcutsSource.Desktop([_desktop], resolver);
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
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver(new() { [shortcut] = file }));
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
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver(map));

        Assert.Empty(source.GetResources());
    }

    [Fact]
    public void StartMenu_IgnoresNonShortcutFiles()
    {
        File.WriteAllText(Path.Combine(_desktop, "readme.txt"), "x");
        Directory.CreateDirectory(Path.Combine(_desktop, "Programs"));
        var source = ShortcutsSource.StartMenu([_desktop], new FakeResolver([]));

        Assert.Empty(source.GetResources());
    }

    [Fact]
    public void Desktop_ListsItsFilesAndFolders_OpenedAsTheyAre()
    {
        var script = Path.Combine(_desktop, "kill rds.cmd");
        File.WriteAllText(script, "x");
        var folder = Path.Combine(_desktop, "rds data files");
        Directory.CreateDirectory(folder);
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver([]));

        var result = source.GetResources().OrderBy(r => r.Name).ToList();

        Assert.Equal(
            new[]
            {
                new Resource("kill rds.cmd", script, ResourceKind.File, "Desktop"),
                new Resource("rds data files", folder, ResourceKind.Folder, "Desktop"),
            },
            result);
    }

    [Fact]
    public void Desktop_SkipsHiddenFiles_AndListsShortcutsOnce()
    {
        var hidden = Path.Combine(_desktop, "desktop.ini");
        File.WriteAllText(hidden, "x");
        File.SetAttributes(hidden, FileAttributes.Hidden);
        var file = Path.Combine(_root, "notes.txt");
        File.WriteAllText(file, "x");
        var shortcut = Shortcut("Notes");
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver(new() { [shortcut] = file }));

        var resource = Assert.Single(source.GetResources());

        Assert.Equal(new Resource("Notes", file, ResourceKind.File, "Desktop", shortcut), resource);
    }

    [Fact]
    public void MissingFolder_IsIgnored()
    {
        var source = ShortcutsSource.Desktop([Path.Combine(_root, "nope")], new FakeResolver([]));

        Assert.Empty(source.GetResources());
    }

    [Fact]
    public void Desktop_IgnoresSubfolders()
    {
        var file = Path.Combine(_root, "notes.txt");
        File.WriteAllText(file, "x");
        var nested = Path.Combine(_desktop, "Tools", "Notes.lnk");
        Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        File.WriteAllText(nested, string.Empty);
        var source = ShortcutsSource.Desktop([_desktop], new FakeResolver(new() { [nested] = file }));

        // The subfolder itself is listed, not the shortcut inside it.
        var resource = Assert.Single(source.GetResources());
        Assert.Equal(new Resource("Tools", Path.GetDirectoryName(nested)!, ResourceKind.Folder, "Desktop"), resource);
    }

    [Fact]
    public void StartMenu_IncludesSubfolders_AndHasItsOwnName()
    {
        var file = Path.Combine(_root, "app.exe");
        File.WriteAllText(file, "x");
        var nested = Path.Combine(_desktop, "Programs", "Vendor", "App.lnk");
        Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        File.WriteAllText(nested, string.Empty);
        var source = ShortcutsSource.StartMenu([_desktop], new FakeResolver(new() { [nested] = file }));

        var resource = Assert.Single(source.GetResources());

        Assert.Equal("Start Menu", source.Name);
        Assert.Equal(new Resource("App", file, ResourceKind.File, "Start Menu", nested), resource);
    }

    [Fact]
    public void UninstallShortcuts_AreSkipped()
    {
        var uninstaller = Path.Combine(_root, "unins000.exe");
        var app = Path.Combine(_root, "app.exe");
        File.WriteAllText(uninstaller, "x");
        File.WriteAllText(app, "x");
        var map = new Dictionary<string, string>
        {
            [Shortcut("Remove App")] = uninstaller,
            [Shortcut("Uninstall Tool")] = app,
            [Shortcut("App")] = app,
        };
        var source = ShortcutsSource.StartMenu([_desktop], new FakeResolver(map));

        Assert.Equal("App", Assert.Single(source.GetResources()).Name);
    }

    [Theory]
    [InlineData("Uninstall App", "app.exe", true)]
    [InlineData("App", "unins000.exe", true)]
    [InlineData("App", "Uninstall.exe", true)]
    [InlineData("App", "app.exe", false)]
    [InlineData("Install guide", "setup.exe", false)]
    public void IsUninstaller_LooksAtTheNameAndTheTargetFile(string name, string targetFile, bool expected)
    {
        Assert.Equal(expected, ShortcutsSource.IsUninstaller(name, Path.Combine(_root, targetFile)));
    }

    [Fact]
    public void DesktopAndStartMenu_CanBeSwitchedOff()
    {
        var desktop = ShortcutsSource.Desktop([_desktop], new FakeResolver([]));
        var startMenu = ShortcutsSource.StartMenu([_desktop], new FakeResolver([]));

        Assert.True(desktop.CanBeDisabled);
        Assert.True(startMenu.CanBeDisabled);
        Assert.Equal("Desktop", desktop.DisplayName);
        Assert.Equal("Start Menu shortcuts", startMenu.DisplayName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
