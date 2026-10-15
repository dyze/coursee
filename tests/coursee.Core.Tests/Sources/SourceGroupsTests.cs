namespace Coursee.Core.Tests.Sources;

public class SourceGroupsTests
{
    [Theory]
    [InlineData("Start Menu", "Windows")]
    [InlineData("Packaged apps", "Windows")]
    [InlineData("Windows Run", "Windows")]
    [InlineData("windows settings", "Windows")]
    [InlineData("Firefox", "Browsers")]
    [InlineData("Edge", "Browsers")]
    [InlineData("Steam", "Games")]
    [InlineData("Epic Games", "Games")]
    [InlineData("GOG", "Games")]
    public void KnownSources_BelongToTheirGroup(string sourceName, string group)
    {
        Assert.Equal(group, SourceGroups.Of(sourceName, isQuickAccess: false)?.Name);
    }

    [Fact]
    public void AQuickAccessFolder_IsInTheQuickAccessGroup()
    {
        Assert.Same(SourceGroups.QuickAccess, SourceGroups.Of(FolderSources.NameOf(@"D:\code"), isQuickAccess: true));
    }

    [Theory]
    [InlineData("Desktop")]
    [InlineData("Folder:D:\\code")]
    public void TheUsersFolders_AreInTheFoldersGroup(string sourceName)
    {
        Assert.Same(SourceGroups.Folders, SourceGroups.Of(sourceName, isQuickAccess: false, isUserFolder: true));
    }

    [Fact]
    public void APinnedFolder_IsInQuickAccess_EvenIfAdded()
    {
        Assert.Same(SourceGroups.QuickAccess, SourceGroups.Of("Desktop", isQuickAccess: true, isUserFolder: true));
    }

    [Theory]
    [InlineData("Custom")]
    [InlineData("Desktop")]
    [InlineData("Folder:D:\\code")]
    public void OtherSources_HaveNoGroup(string sourceName)
    {
        Assert.Null(SourceGroups.Of(sourceName, isQuickAccess: false));
    }

    [Fact]
    public void EachGroup_HasADistinctNameAndIcon()
    {
        Assert.Equal(SourceGroups.All.Count, SourceGroups.All.Select(group => group.Name).Distinct().Count());
        Assert.Equal(SourceGroups.All.Count, SourceGroups.All.Select(group => group.Icon).Distinct().Count());
        Assert.All(SourceGroups.All, group => Assert.False(string.IsNullOrWhiteSpace(group.ToolTip)));
    }
}
