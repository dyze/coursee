namespace Coursee.Core.Tests.Entries;

public class CustomEntryTests
{
    [Fact]
    public void ToResource_KeepsTheNameAndUsesTheCustomSource()
    {
        var resource = new CustomEntry("Team wiki", "https://wiki").ToResource();

        Assert.Equal(new Resource("Team wiki", "https://wiki", ResourceKind.Url, CustomEntry.SourceName), resource);
    }

    [Fact]
    public void ToResource_DecidesTheKindLikeTypedText()
    {
        Assert.Equal(ResourceKind.Folder, new CustomEntry("Tools", @"C:\tools").ToResource(_ => true, _ => false).Kind);
        Assert.Equal(ResourceKind.File, new CustomEntry("Notes", @"C:\notes.txt").ToResource(_ => false, _ => true).Kind);
        Assert.Equal(ResourceKind.NetworkPath, new CustomEntry("Share", @"\\server\share").ToResource().Kind);
        Assert.Equal(ResourceKind.Command, new CustomEntry("Ping", "ping localhost").ToResource().Kind);
    }

    [Fact]
    public void ToResource_TargetIsWhatIsOpened()
    {
        Assert.Equal("https://www.example.com", new CustomEntry("Example", "www.example.com").ToResource().Target);
    }
}
