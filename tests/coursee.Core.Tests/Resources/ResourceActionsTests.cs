namespace Coursee.Core.Tests.Resources;

public class ResourceActionsTests
{
    private static Resource R(ResourceKind kind, string target, string source = "Desktop") =>
        new("item", target, kind, source);

    [Theory]
    [InlineData(ResourceKind.File, true)]
    [InlineData(ResourceKind.Folder, true)]
    [InlineData(ResourceKind.NetworkPath, true)]
    [InlineData(ResourceKind.Url, false)]
    [InlineData(ResourceKind.Command, false)]
    public void CanShowInFolder_ForFileSystemItemsOnly(ResourceKind kind, bool expected)
    {
        Assert.Equal(expected, ResourceActions.CanShowInFolder(R(kind, @"C:\x")));
    }

    [Fact]
    public void ShowInFolderArguments_SelectTheRealTarget_NotTheShortcut()
    {
        var resource = new Resource("Notes", @"C:\docs\notes.txt", ResourceKind.File, "Desktop", @"C:\Desktop\Notes.lnk");

        Assert.Equal("/select,\"C:\\docs\\notes.txt\"", ResourceActions.ShowInFolderArguments(resource));
    }

    [Theory]
    [InlineData(@"C:\work\", "/select,\"C:\\work\"")]
    [InlineData(@"\\server\share\", "/select,\"\\\\server\\share\"")]
    [InlineData(@"C:\", "/select,\"C:\\\"")]
    public void ShowInFolderArguments_DropATrailingSlash_ButKeepADriveRoot(string target, string expected)
    {
        Assert.Equal(expected, ResourceActions.ShowInFolderArguments(R(ResourceKind.Folder, target)));
    }

    [Theory]
    [InlineData(@"C:\tools\app.exe", true)]
    [InlineData(@"C:\tools\Build.CMD", true)]
    [InlineData(@"C:\tools\run.bat", true)]
    [InlineData(@"C:\Windows\System32\compmgmt.msc", true)]
    [InlineData(@"C:\docs\notes.txt", false)]
    [InlineData(@"C:\tools\script.ps1", false)]
    [InlineData(@"C:\tools.d\noextension", false)]
    public void CanRunAsAdministrator_ExecutableFiles(string target, bool expected)
    {
        Assert.Equal(expected, ResourceActions.CanRunAsAdministrator(R(ResourceKind.File, target)));
    }

    [Fact]
    public void CanRunAsAdministrator_CommandsYes_FoldersAndUrlsNo()
    {
        Assert.True(ResourceActions.CanRunAsAdministrator(R(ResourceKind.Command, "cmd /k dir")));
        Assert.False(ResourceActions.CanRunAsAdministrator(R(ResourceKind.Folder, @"C:\tools.exe")));
        Assert.False(ResourceActions.CanRunAsAdministrator(R(ResourceKind.Url, "https://example.com/a.exe")));
    }

    [Fact]
    public void Hint_ListsOnlyTheActionsThatApply()
    {
        Assert.Equal(
            "Enter: Open file  ·  Ctrl+Enter: Show in folder  ·  Ctrl+Shift+Enter: Run as admin",
            ResourceActions.Hint(R(ResourceKind.File, @"C:\tools\app.exe"), inRecents: false));

        Assert.Equal(
            "Enter: Open URL",
            ResourceActions.Hint(R(ResourceKind.Url, "https://example.com"), inRecents: false));
    }

    [Fact]
    public void Hint_OffersRemovalForRecentItems()
    {
        Assert.Equal(
            "Enter: Open folder  ·  Ctrl+Enter: Show in folder  ·  Del: Remove from recents",
            ResourceActions.Hint(R(ResourceKind.Folder, @"C:\work"), inRecents: true));
    }

    [Fact]
    public void Hint_SaysWhenTheItemIsWhatWasTyped()
    {
        Assert.Equal(
            "Enter: Run command (what you typed)  ·  Ctrl+Shift+Enter: Run as admin",
            ResourceActions.Hint(R(ResourceKind.Command, "notepad", FallbackClassifier.SourceName), inRecents: false));
    }
}
