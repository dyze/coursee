namespace Coursee.Core.Tests.Resources;

public class ResourceActionsTests
{
    private static Resource R(ResourceKind kind, string target, string source = "Desktop") =>
        new("item", target, kind, source);

    private sealed class FakeLocator(params string[] installed) : IBrowserLocator
    {
        public List<string> Asked { get; } = [];

        public string? FindExecutable(string executableName)
        {
            Asked.Add(executableName);
            return installed.Contains(executableName) ? @"C:\Apps\" + executableName : null;
        }
    }

    [Theory]
    [InlineData("Firefox", @"C:\Apps\firefox.exe")]
    [InlineData("Edge", @"C:\Apps\msedge.exe")]
    [InlineData("edge", @"C:\Apps\msedge.exe")]
    public void BrowserExecutable_IsTheBrowserTheBookmarkCameFrom(string browser, string expected)
    {
        var bookmark = new Resource("Docs", "https://example.com", ResourceKind.Url, browser, Browser: browser);

        Assert.Equal(expected, ResourceActions.BrowserExecutable(bookmark, new FakeLocator("firefox.exe", "msedge.exe")));
    }

    [Fact]
    public void BrowserExecutable_BrowserNotInstalled_IsNull()
    {
        var bookmark = new Resource("Docs", "https://example.com", ResourceKind.Url, "Edge", Browser: "Edge");

        Assert.Null(ResourceActions.BrowserExecutable(bookmark, new FakeLocator()));
    }

    [Fact]
    public void BrowserExecutable_NoBrowserOrUnknownOne_DoesNotLookForAny()
    {
        var locator = new FakeLocator("firefox.exe");

        Assert.Null(ResourceActions.BrowserExecutable(R(ResourceKind.Url, "https://example.com", "Typed"), locator));
        Assert.Null(ResourceActions.BrowserExecutable(new Resource("x", "https://x", ResourceKind.Url, "s", Browser: "Opera"), locator));
        Assert.Null(ResourceActions.BrowserExecutable(new Resource("x", @"C:\x", ResourceKind.File, "s", Browser: "Firefox"), locator));
        Assert.Empty(locator.Asked);
    }

    [Fact]
    public void Hint_NamesTheBrowserOfABookmark()
    {
        var bookmark = new Resource("Docs", "https://example.com", ResourceKind.Url, "Firefox", Browser: "Firefox");

        Assert.Equal("Enter: Open in Firefox", ResourceActions.Hint(bookmark, inRecents: false));
    }

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

    [Theory]
    [InlineData("services.msc", true)]
    [InlineData("SystemPropertiesAdvanced.exe", true)]
    [InlineData("control.exe appwiz.cpl", true)]
    [InlineData("rundll32.exe sysdm.cpl,EditEnvironmentVariables", true)]
    [InlineData("ms-settings:appsfeatures", false)]
    public void CanRunAsAdministrator_Settings_ConsolesAndProgramsOnly(string target, bool expected) =>
        Assert.Equal(expected, ResourceActions.CanRunAsAdministrator(R(ResourceKind.Setting, target)));

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

    [Fact]
    public void CanPin_EntriesFromASource_NotTypedText()
    {
        Assert.True(ResourceActions.CanPin(R(ResourceKind.Folder, @"C:\work")));
        Assert.False(ResourceActions.CanPin(R(ResourceKind.Command, "notepad", FallbackClassifier.SourceName)));
    }
}
