namespace Coursee.Core.Tests.Sources;

public sealed class ChromiumBookmarksTests : IDisposable
{
    private readonly string _userData = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private const string Sample = """
        {
          "checksum": "abc",
          "roots": {
            "bookmark_bar": {
              "type": "folder",
              "name": "Favorites bar",
              "children": [
                { "type": "url", "name": "Example", "url": "https://example.com/" },
                {
                  "type": "folder",
                  "name": "Work",
                  "children": [
                    { "type": "url", "name": "Wiki", "url": "https://wiki.example.com/" }
                  ]
                }
              ]
            },
            "other": {
              "type": "folder",
              "name": "Other favorites",
              "children": [
                { "type": "url", "name": "", "url": "ftp://files.example.com/" },
                { "type": "url", "name": "Bookmarklet", "url": "javascript:alert(1)" },
                { "type": "url", "name": "Settings", "url": "edge://settings" },
                { "type": "url", "name": "Example again", "url": "https://EXAMPLE.com/" }
              ]
            },
            "synced": { "type": "folder", "name": "Mobile favorites", "children": [] }
          },
          "version": 1
        }
        """;

    [Fact]
    public void Parse_ReturnsTheLinksOfEveryRootAndFolder_InFileOrder()
    {
        var resources = ChromiumBookmarks.Parse(Sample, "Edge");

        Assert.Equal(
            new[] { "https://example.com/", "https://wiki.example.com/", "ftp://files.example.com/" },
            resources.Select(r => r.Target));
        Assert.All(resources, r =>
        {
            Assert.Equal(ResourceKind.Url, r.Kind);
            Assert.Equal("Edge", r.Source);
        });
    }

    [Fact]
    public void Parse_UsesTheUrlWhenTheNameIsEmpty()
    {
        var resources = ChromiumBookmarks.Parse(Sample, "Edge");

        Assert.Equal("Example", resources[0].Name);
        Assert.Equal("ftp://files.example.com/", resources[2].Name);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"roots\": 3}")]
    [InlineData("{\"roots\": {\"bookmark_bar\": {\"children\": \"nope\"}}}")]
    public void Parse_UnexpectedShapes_GiveNothing(string json)
    {
        Assert.Empty(ChromiumBookmarks.Parse(json, "Edge"));
    }

    [Fact]
    public void Parse_NotJson_Throws()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => ChromiumBookmarks.Parse("{ nope", "Edge"));
    }

    [Theory]
    [InlineData("{\"profile\":{\"last_used\":\"Profile 2\"}}", "Profile 2")]
    [InlineData("{\"profile\":{}}", null)]
    [InlineData("{\"profile\":{\"last_used\":\"..\\\\..\\\\Windows\"}}", null)]
    [InlineData("{\"profile\":{\"last_used\":\"C:/x\"}}", null)]
    [InlineData("{ nope", null)]
    [InlineData("[]", null)]
    public void ParseLastUsedProfile_ReadsAPlainFolderName(string json, string? expected)
    {
        Assert.Equal(expected, ChromiumBookmarks.ParseLastUsedProfile(json));
    }

    [Fact]
    public void FindProfile_PrefersTheLastUsedProfile()
    {
        Directory.CreateDirectory(Path.Combine(_userData, "Default"));
        Directory.CreateDirectory(Path.Combine(_userData, "Profile 1"));
        File.WriteAllText(Path.Combine(_userData, "Local State"), "{\"profile\":{\"last_used\":\"Profile 1\"}}");

        Assert.Equal(Path.Combine(_userData, "Profile 1"), ChromiumBookmarks.FindProfile(_userData));
    }

    [Fact]
    public void FindProfile_FallsBackToDefault()
    {
        Directory.CreateDirectory(Path.Combine(_userData, "Default"));
        File.WriteAllText(Path.Combine(_userData, "Local State"), "{\"profile\":{\"last_used\":\"Gone\"}}");

        Assert.Equal(Path.Combine(_userData, "Default"), ChromiumBookmarks.FindProfile(_userData));
    }

    [Fact]
    public void FindProfile_NoBrowser_IsNull()
    {
        Assert.Null(ChromiumBookmarks.FindProfile(_userData));
    }

    public void Dispose()
    {
        if (Directory.Exists(_userData))
        {
            Directory.Delete(_userData, recursive: true);
        }
    }
}
