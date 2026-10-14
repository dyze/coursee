namespace Coursee.Core.Tests.Sources;

public sealed class SourceCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly RingLog _log = new();
    private readonly FakeQuickAccess _quickAccess = new();
    private readonly SettingsStore _settings;
    private readonly string _desktop;
    private readonly string _code;
    private readonly string _notes;

    public SourceCatalogTests()
    {
        _settings = new SettingsStore(Path.Combine(_root, "settings.json"));
        _desktop = Folder("desktop");
        _code = Folder("code");
        _notes = Folder("notes");
    }

    private sealed class FakeQuickAccess : IQuickAccessReader
    {
        public List<string> Pinned { get; set; } = [];

        /// <summary>The name Explorer shows, by path; else the folder name.</summary>
        public Dictionary<string, string> Shown { get; } = [];

        public Exception? Error { get; set; }

        public IReadOnlyList<PinnedFolder> PinnedFolders() => Error is null
            ? Pinned.Select(path => new PinnedFolder(path, Shown.GetValueOrDefault(path, Path.GetFileName(path)))).ToList()
            : throw Error;
    }

    private sealed class NoShortcuts : IShortcutResolver
    {
        public string? ResolveTarget(string shortcutPath) => null;
    }

    private sealed class Fixed(string name) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => name;

        public bool CanBeDisabled => true;

        public string Icon => string.Empty;

        public IReadOnlyList<Resource> GetResources() => [];
    }

    private string Folder(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private SourceCatalog Catalog() => new(
        _settings,
        new NoShortcuts(),
        [_desktop],
        _quickAccess,
        first: [new Fixed("Custom")],
        last: [new Fixed("Start Menu")],
        _log);

    private static string[] Names(SourceCatalog catalog) => catalog.All.Select(source => source.Name).ToArray();

    [Fact]
    public void ByDefault_TheDesktopIsBetweenTheFirstAndLastSources()
    {
        Assert.Equal(new[] { "Custom", "Desktop", "Start Menu" }, Names(Catalog()));
    }

    [Fact]
    public void AddedFolders_ComeAfterTheDesktop_InTheirOrder()
    {
        var catalog = Catalog();

        Assert.Equal(AddFolderResult.Added, catalog.AddFolder(_code));
        Assert.Equal(AddFolderResult.Added, catalog.AddFolder(_notes + Path.DirectorySeparatorChar));

        Assert.Equal(
            new[] { "Custom", "Desktop", FolderSources.NameOf(_code), FolderSources.NameOf(_notes), "Start Menu" },
            Names(catalog));
        Assert.Equal(new[] { SettingsStore.DesktopFolder, _code, _notes }, _settings.Folders);
    }

    [Fact]
    public void AddingAFolderTwice_SaysSo()
    {
        var catalog = Catalog();
        catalog.AddFolder(_code);

        Assert.Equal(AddFolderResult.AlreadyAdded, catalog.AddFolder(_code.ToUpperInvariant()));
    }

    [Fact]
    public void ANetworkFolder_IsRefused()
    {
        var catalog = Catalog();

        Assert.Equal(AddFolderResult.NetworkFolder, catalog.AddFolder(@"\\server\share"));
        Assert.Equal(new[] { SettingsStore.DesktopFolder }, _settings.Folders);
    }

    [Fact]
    public void TheDesktop_CanBeRemoved_AndAddedBackByChoosingItsFolder()
    {
        var catalog = Catalog();

        Assert.True(catalog.RemoveFolder("Desktop"));
        Assert.Equal(new[] { "Custom", "Start Menu" }, Names(catalog));

        Assert.Equal(AddFolderResult.Added, catalog.AddFolder(_desktop));
        Assert.Equal(new[] { "Custom", "Desktop", "Start Menu" }, Names(catalog));
    }

    [Fact]
    public void RemovingAFolder_ForgetsItWasSwitchedOff()
    {
        var catalog = Catalog();
        catalog.AddFolder(_code);
        var name = FolderSources.NameOf(_code);
        _settings.SetSourceEnabled(name, false);

        Assert.True(catalog.RemoveFolder(name));

        Assert.True(_settings.IsSourceEnabled(name));
        Assert.DoesNotContain(name, Names(catalog));
    }

    [Fact]
    public void OnlyTheUsersFolders_CanBeRemoved()
    {
        _quickAccess.Pinned = [_notes];
        var catalog = Catalog();
        catalog.AddFolder(_code);
        catalog.SyncQuickAccess();

        Assert.True(catalog.CanRemove("Desktop"));
        Assert.True(catalog.CanRemove(FolderSources.NameOf(_code)));
        Assert.False(catalog.CanRemove(FolderSources.NameOf(_notes)));
        Assert.False(catalog.CanRemove("Start Menu"));
        Assert.False(catalog.RemoveFolder(FolderSources.NameOf(_notes)));
        Assert.False(catalog.RemoveFolder("Start Menu"));
    }

    [Fact]
    public void QuickAccessFolders_ComeAfterTheUsersFolders()
    {
        _quickAccess.Pinned = [_notes];
        var catalog = Catalog();
        catalog.AddFolder(_code);

        Assert.True(catalog.SyncQuickAccess());

        Assert.Equal(
            new[] { "Custom", "Desktop", FolderSources.NameOf(_code), FolderSources.NameOf(_notes), "Start Menu" },
            Names(catalog));
        Assert.Equal(SourceIcons.QuickAccess, catalog.All[3].Icon);
    }

    [Fact]
    public void AFolderAlreadyListed_IsNotAddedAgainFromQuickAccess()
    {
        _quickAccess.Pinned = [_desktop, _code + Path.DirectorySeparatorChar, _code];
        var catalog = Catalog();
        catalog.AddFolder(_code);

        catalog.SyncQuickAccess();

        Assert.Equal(new[] { "Custom", "Desktop", FolderSources.NameOf(_code), "Start Menu" }, Names(catalog));
        Assert.Equal(SourceIcons.Folder, catalog.All[2].Icon);
    }

    [Fact]
    public void APinnedDesktop_IsTheDesktopSource_LikeOtherQuickAccessFolders()
    {
        _quickAccess.Pinned = [_code, _desktop];
        var catalog = Catalog();
        catalog.RemoveFolder("Desktop");

        catalog.SyncQuickAccess();

        Assert.Equal(new[] { "Custom", FolderSources.NameOf(_code), "Desktop", "Start Menu" }, Names(catalog));
        Assert.Equal(SourceIcons.Desktop, catalog.All[2].Icon);
        Assert.False(catalog.CanRemove("Desktop"));
        Assert.True(catalog.IsQuickAccess("Desktop"));
    }

    [Fact]
    public void IsQuickAccess_OnlyForPinnedFoldersNotListedOtherwise()
    {
        _quickAccess.Pinned = [_code, _notes, _desktop];
        var catalog = Catalog();
        catalog.AddFolder(_notes);
        catalog.SyncQuickAccess();

        Assert.True(catalog.IsQuickAccess(FolderSources.NameOf(_code)));
        Assert.False(catalog.IsQuickAccess(FolderSources.NameOf(_notes)));
        Assert.False(catalog.IsQuickAccess("Desktop"));
        Assert.False(catalog.IsQuickAccess("Start Menu"));
    }

    [Fact]
    public void TheDesktopMovingToQuickAccess_IsAChange_EvenInTheSamePlace()
    {
        _quickAccess.Pinned = [_desktop];
        var catalog = Catalog();
        catalog.SyncQuickAccess();
        var raised = 0;
        catalog.Changed += () => raised++;

        catalog.RemoveFolder("Desktop");

        Assert.Equal(new[] { "Custom", "Desktop", "Start Menu" }, Names(catalog));
        Assert.Equal(1, raised);
        Assert.True(catalog.IsQuickAccess("Desktop"));
    }

    [Fact]
    public void NetworkFoldersInQuickAccess_AreSkipped()
    {
        _quickAccess.Pinned = [@"\\server\share", " "];
        var catalog = Catalog();

        Assert.False(catalog.SyncQuickAccess());
        Assert.Equal(new[] { "Custom", "Desktop", "Start Menu" }, Names(catalog));
    }

    [Fact]
    public void UnpinnedFolders_Disappear_AndTheChangeIsLogged()
    {
        _quickAccess.Pinned = [_code];
        var catalog = Catalog();
        catalog.SyncQuickAccess();

        _quickAccess.Pinned = [_notes];
        Assert.True(catalog.SyncQuickAccess());

        Assert.Equal(new[] { "Custom", "Desktop", FolderSources.NameOf(_notes), "Start Menu" }, Names(catalog));
        var entry = _log.Entries.Last(e => e.Category == SourceCatalog.QuickAccessCategory);
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Contains("added: " + _notes, entry.Message);
        Assert.Contains("removed: " + _code, entry.Message);
    }

    [Fact]
    public void UnchangedQuickAccess_KeepsTheSameList()
    {
        _quickAccess.Pinned = [_code];
        var catalog = Catalog();
        catalog.SyncQuickAccess();
        var before = catalog.All;

        Assert.False(catalog.SyncQuickAccess());
        Assert.Same(before, catalog.All);
    }

    [Fact]
    public void AFailingQuickAccess_KeepsItsFolders_AndIsLogged()
    {
        _quickAccess.Pinned = [_code];
        var catalog = Catalog();
        catalog.SyncQuickAccess();

        _quickAccess.Error = new InvalidOperationException("shell busy");

        Assert.False(catalog.SyncQuickAccess());
        Assert.Contains(FolderSources.NameOf(_code), Names(catalog));
        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("shell busy"));
    }

    [Fact]
    public void AFolderSource_IsKeptWhileListed()
    {
        _quickAccess.Pinned = [_code];
        var catalog = Catalog();
        catalog.SyncQuickAccess();
        var source = catalog.All[2];

        catalog.AddFolder(_notes);

        Assert.Same(source, catalog.All.Single(s => s.Name == source.Name));
    }

    [Fact]
    public void AQuickAccessFolderAddedByTheUser_BecomesTheirs()
    {
        _quickAccess.Pinned = [_code];
        var catalog = Catalog();
        catalog.SyncQuickAccess();

        catalog.AddFolder(_code);

        var source = catalog.All.Single(s => s.Name == FolderSources.NameOf(_code));
        Assert.Equal(SourceIcons.Folder, source.Icon);
        Assert.True(catalog.CanRemove(source.Name));
    }

    [Fact]
    public void EveryFolderSource_ListsTheFolderItself()
    {
        _quickAccess.Pinned = [_notes];
        var catalog = Catalog();
        catalog.AddFolder(_code);
        catalog.SyncQuickAccess();

        Assert.Contains(catalog.All[1].GetResources(), r => r.Name == "desktop" && r.Target == _desktop && r.Kind == ResourceKind.Folder);
        Assert.Contains(catalog.All[2].GetResources(), r => r.Name == "code" && r.Target == _code);
        Assert.Contains(catalog.All[3].GetResources(), r => r.Name == "notes" && r.Target == _notes);
    }

    [Fact]
    public void AFolderIsNamedAsExplorerShowsIt_EvenWhenTheUserAddedIt()
    {
        _quickAccess.Pinned = [_desktop, _code];
        _quickAccess.Shown[_desktop] = "Bureau";
        _quickAccess.Shown[_code] = "Code";
        var catalog = Catalog();

        catalog.SyncQuickAccess();

        Assert.Equal("Bureau (desktop)", ((ShortcutsSource)catalog.All[1]).FolderEntryName);
        Assert.Equal("code", ((ShortcutsSource)catalog.All[2]).FolderEntryName);
    }

    [Fact]
    public void ARenamedPin_IsAChange_AndRenamesTheRow()
    {
        _quickAccess.Pinned = [_code];
        var catalog = Catalog();
        catalog.SyncQuickAccess();

        _quickAccess.Shown[_code] = "Projets";

        Assert.True(catalog.SyncQuickAccess());
        Assert.Equal("Projets (code)", ((ShortcutsSource)catalog.All[2]).FolderEntryName);
    }

    [Fact]
    public void Changed_IsRaisedOnlyWhenTheListChanges()
    {
        var catalog = Catalog();
        var raised = 0;
        catalog.Changed += () => raised++;

        catalog.AddFolder(_code);
        catalog.AddFolder(_code);
        catalog.SyncQuickAccess();
        catalog.RemoveFolder(FolderSources.NameOf(_code));

        Assert.Equal(2, raised);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
