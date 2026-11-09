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

    [Theory]
    [InlineData(@"C:\games\Doom\doom.exe", ResourceKind.File, "doom")]
    [InlineData(@"C:\notes\plan.v2.txt", ResourceKind.File, "plan.v2")]
    [InlineData(@"C:\code\.gitignore", ResourceKind.File, ".gitignore")]
    [InlineData(@"C:\tools\README", ResourceKind.File, "README")]
    [InlineData(@"C:\code\coursee", ResourceKind.Folder, "coursee")]
    [InlineData(@"C:\code\my.project\", ResourceKind.Folder, "my.project")]
    [InlineData("/home/me/docs", ResourceKind.Folder, "docs")]
    [InlineData(@"\\server\share\report.pdf", ResourceKind.NetworkPath, "report.pdf")]
    public void NameFor_IsTheLastPartOfThePath(string target, ResourceKind kind, string expected)
    {
        Assert.Equal(expected, CustomEntry.NameFor(new Resource(target, target, kind, FallbackClassifier.SourceName)));
    }

    [Theory]
    [InlineData(@"D:\", ResourceKind.Folder)]
    [InlineData("D:", ResourceKind.Folder)]
    [InlineData("https://wiki", ResourceKind.Url)]
    [InlineData("ping localhost", ResourceKind.Command)]
    public void NameFor_KeepsTheNameOfADriveOrAnythingElse(string target, ResourceKind kind)
    {
        Assert.Equal(target, CustomEntry.NameFor(new Resource(target, target, kind, FallbackClassifier.SourceName)));
    }
}
