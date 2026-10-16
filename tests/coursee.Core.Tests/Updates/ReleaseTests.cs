namespace Coursee.Core.Tests.Updates;

public class ReleaseTests
{
    [Fact]
    public void GitHubJson_GivesTheTagAndThePage()
    {
        const string json = """{ "tag_name": "v0.8", "html_url": "https://github.com/dyze/coursee/releases/tag/v0.8", "draft": false }""";

        Assert.Equal(new Release("v0.8", "https://github.com/dyze/coursee/releases/tag/v0.8"), Release.FromGitHubJson(json));
    }

    [Theory]
    [InlineData("""{ "html_url": "https://x" }""")]
    [InlineData("""{ "tag_name": 8, "html_url": "https://x" }""")]
    [InlineData("""["v0.8"]""")]
    [InlineData("not json")]
    public void IncompleteOrBadJson_GivesNothing(string json)
    {
        Assert.Null(Release.FromGitHubJson(json));
    }

    [Theory]
    [InlineData("0.8", "0.7", true)]
    [InlineData("v0.10", "0.9", true)]
    [InlineData("V1.0", "0.9", true)]
    [InlineData("0.7", "0.7", false)]
    [InlineData("0.6", "0.7", false)]
    [InlineData("latest", "0.7", false)]
    [InlineData("0.8", "unknown", false)]
    [InlineData("0.8.1", "0.8", true)]
    [InlineData("0.9", "0.8.1", true)]
    [InlineData("0.8", "0.8.1", false)]
    public void IsNewer_ComparesMajorMinorThenPatch(string candidate, string current, bool expected)
    {
        Assert.Equal(expected, Release.IsNewer(candidate, current));
    }
}
