using System.Text.Json;

namespace Coursee.Core.Tests.Resources;

public class ResourceTests
{
    [Theory]
    [InlineData(ResourceKind.File, "Open file")]
    [InlineData(ResourceKind.Folder, "Open folder")]
    [InlineData(ResourceKind.Url, "Open URL")]
    [InlineData(ResourceKind.Command, "Run command")]
    [InlineData(ResourceKind.NetworkPath, "Open")]
    public void ActionLabel_DescribesWhatEnterDoes(ResourceKind kind, string expected)
    {
        var resource = new Resource("x", "y", kind, "test");

        Assert.Equal(expected, resource.ActionLabel);
        Assert.Equal(expected, ActionLabels.For(kind));
    }

    [Theory]
    [InlineData("Firefox", "Open in Firefox")]
    [InlineData("Edge", "Open in Edge")]
    [InlineData(null, "Open URL")]
    [InlineData("", "Open URL")]
    public void ActionLabel_NamesTheBrowserOfABookmark(string? browser, string expected)
    {
        Assert.Equal(expected, new Resource("x", "https://example.com", ResourceKind.Url, "test", Browser: browser).ActionLabel);
    }

    [Fact]
    public void ActionLabel_IgnoresTheBrowserForOtherKinds()
    {
        Assert.Equal("Open file", new Resource("x", @"C:\x", ResourceKind.File, "test", Browser: "Firefox").ActionLabel);
    }

    [Theory]
    [InlineData(ResourceKind.File, null, ResourceAction.OpenFile)]
    [InlineData(ResourceKind.Folder, null, ResourceAction.OpenFolder)]
    [InlineData(ResourceKind.Url, null, ResourceAction.OpenUrl)]
    [InlineData(ResourceKind.Url, "", ResourceAction.OpenUrl)]
    [InlineData(ResourceKind.Url, "Firefox", ResourceAction.OpenInBrowser)]
    [InlineData(ResourceKind.Command, null, ResourceAction.RunCommand)]
    [InlineData(ResourceKind.NetworkPath, null, ResourceAction.Open)]
    [InlineData(ResourceKind.File, "Firefox", ResourceAction.OpenFile)]
    public void Action_MatchesTheLabel(ResourceKind kind, string? browser, ResourceAction expected)
    {
        Assert.Equal(expected, new Resource("x", "y", kind, "test", Browser: browser).Action);
        Assert.Equal(expected, ActionLabels.ActionFor(kind, browser));
    }

    [Fact]
    public void Json_LeavesOutTheAction()
    {
        var json = JsonSerializer.Serialize(new Resource("x", @"C:\x", ResourceKind.File, "test"));

        Assert.DoesNotContain("\"Action\"", json);
    }

    [Fact]
    public void Json_RoundTripsTheBrowser()
    {
        var original = new Resource("Docs", "https://example.com", ResourceKind.Url, "Firefox", Browser: "Firefox");

        Assert.Equal(original, JsonSerializer.Deserialize<Resource>(JsonSerializer.Serialize(original)));
    }

    [Fact]
    public void Json_FromBeforeTheBrowser_LoadsWithoutOne()
    {
        const string json = """{"Name":"Docs","Target":"https://example.com","Kind":2,"Source":"Firefox","LaunchPath":null}""";

        Assert.Null(JsonSerializer.Deserialize<Resource>(json)!.Browser);
    }

    [Fact]
    public void Json_RoundTripsWithoutTheDisplayLabel()
    {
        var original = new Resource("notepad", "notepad", ResourceKind.Command, "Typed");

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Resource>(json);

        Assert.DoesNotContain("ActionLabel", json);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void ExistingKinds_KeepTheirStoredNumbers()
    {
        Assert.Equal(0, (int)ResourceKind.File);
        Assert.Equal(1, (int)ResourceKind.Folder);
        Assert.Equal(2, (int)ResourceKind.Url);
        Assert.Equal(3, (int)ResourceKind.Command);
        Assert.Equal(4, (int)ResourceKind.NetworkPath);
    }

    [Fact]
    public void OpenTarget_IsTheTargetByDefault()
    {
        var resource = new Resource("Work", @"C:\work", ResourceKind.Folder, "Desktop");

        Assert.Null(resource.LaunchPath);
        Assert.Equal(@"C:\work", resource.OpenTarget);
    }

    [Fact]
    public void OpenTarget_IsTheLaunchPathWhenSet()
    {
        var resource = new Resource("Work", @"C:\work", ResourceKind.Folder, "Desktop", @"C:\Desktop\Work.lnk");

        Assert.Equal(@"C:\Desktop\Work.lnk", resource.OpenTarget);
    }

    [Fact]
    public void Json_RoundTripsTheLaunchPath()
    {
        var original = new Resource("Work", @"C:\work", ResourceKind.Folder, "Desktop", @"C:\Desktop\Work.lnk");

        var restored = JsonSerializer.Deserialize<Resource>(JsonSerializer.Serialize(original));

        Assert.Equal(original, restored);
        Assert.DoesNotContain("OpenTarget", JsonSerializer.Serialize(original));
    }

    [Fact]
    public void Json_FromBeforeTheLaunchPath_StillLoads()
    {
        const string json = """{"Name":"Work","Target":"C:\\work","Kind":1,"Source":"Desktop"}""";

        var restored = JsonSerializer.Deserialize<Resource>(json);

        Assert.Equal(new Resource("Work", @"C:\work", ResourceKind.Folder, "Desktop"), restored);
        Assert.Equal(@"C:\work", restored!.OpenTarget);
    }
}
