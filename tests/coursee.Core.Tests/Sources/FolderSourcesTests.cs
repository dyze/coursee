namespace Coursee.Core.Tests.Sources;

public sealed class FolderSourcesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private sealed class NoShortcuts : IShortcutResolver
    {
        public string? ResolveTarget(string shortcutPath) => null;
    }

    [Fact]
    public void Name_IsThePath_WithoutTrailingSeparator()
    {
        var folder = Path.Combine(_root, "work");

        Assert.Equal("Folder:" + folder, FolderSources.NameOf(folder + Path.DirectorySeparatorChar));
        Assert.Equal(folder, FolderSources.FolderOf(FolderSources.NameOf(folder)));
    }

    [Fact]
    public void OtherSourceNames_AreNoFolders()
    {
        Assert.False(FolderSources.IsFolderName("Desktop"));
        Assert.Null(FolderSources.FolderOf("Firefox"));
        Assert.True(FolderSources.IsFolderName("folder:x"));
    }

    [Fact]
    public void AreSame_IgnoresCaseSpacesAndTrailingSeparator()
    {
        var folder = Path.Combine(_root, "Work");

        Assert.True(FolderSources.AreSame(folder, " " + folder.ToLowerInvariant() + Path.DirectorySeparatorChar));
        Assert.False(FolderSources.AreSame(folder, folder + "2"));
    }

    [Theory]
    [InlineData(@"\\server\share")]
    [InlineData("//server/share")]
    public void SharesAreNetworkFolders(string folder) => Assert.True(FolderSources.IsNetwork(folder));

    [Theory]
    [InlineData("relative")]
    [InlineData(@"\rooted")]
    public void FoldersWithoutADrive_AreNotNetworkFolders(string folder) => Assert.False(FolderSources.IsNetwork(folder));

    [Fact]
    public void ALocalFolder_IsNotANetworkFolder() => Assert.False(FolderSources.IsNetwork(Path.GetTempPath()));

    [Fact]
    public void FolderSource_ListsItsTopLevel_AndIsNamedAfterItsPath()
    {
        var folder = Path.Combine(_root, "work");
        Directory.CreateDirectory(Path.Combine(folder, "sub"));
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "x");
        File.WriteAllText(Path.Combine(folder, "sub", "deep.txt"), "x");

        var source = ShortcutsSource.Folder(folder, quickAccess: false, new NoShortcuts());

        Assert.Equal(FolderSources.NameOf(folder), source.Name);
        Assert.Equal(folder, source.DisplayName);
        Assert.Equal(SourceIcons.Folder, source.Icon);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(new[] { "notes.txt", "sub" }, source.GetResources().Select(r => r.Name).Order());
        Assert.All(source.GetResources(), r => Assert.Equal(source.Name, r.Source));
    }

    [Theory]
    [InlineData(@"C:\Users\me\Desktop", null, "Desktop")]
    [InlineData(@"C:\Users\me\Desktop\", " ", "Desktop")]
    [InlineData(@"C:\Users\me\Desktop", "desktop", "Desktop")]
    [InlineData(@"C:\Users\me\Desktop", "Bureau", "Bureau (Desktop)")]
    public void EntryName_IsTheFolderName_WithTheShownNameWhenItDiffers(string folder, string? shown, string expected)
    {
        // Backslashes are separators on Windows only.
        var path = folder.Replace('\\', Path.DirectorySeparatorChar);

        Assert.Equal(expected, FolderSources.EntryName(path, shown));
    }

    [Fact]
    public void EntryName_OfADrive_IsTheDrive()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Equal(FolderSources.Normalize(root), FolderSources.EntryName(root));
    }

    [Fact]
    public void FolderSource_WithAnEntryName_ListsTheFolderItself()
    {
        Directory.CreateDirectory(_root);

        var source = ShortcutsSource.Folder(_root, quickAccess: false, new NoShortcuts(), folderEntryName: "Work");

        var entry = Assert.Single(source.GetResources());
        Assert.Equal(new Resource("Work", _root, ResourceKind.Folder, source.Name), entry);
        Assert.Equal("Work", source.FolderEntryName);
    }

    [Fact]
    public void FolderSource_OfAMissingFolder_ListsNothing()
    {
        var source = ShortcutsSource.Folder(Path.Combine(_root, "gone"), quickAccess: false, new NoShortcuts(), folderEntryName: "Gone");

        Assert.Empty(source.GetResources());
    }

    [Fact]
    public void QuickAccessFolder_HasItsOwnIcon_AndItsPathAsLabel()
    {
        var source = ShortcutsSource.Folder(_root, quickAccess: true, new NoShortcuts());

        Assert.Equal(SourceIcons.QuickAccess, source.Icon);
        Assert.Equal(_root, source.DisplayName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
