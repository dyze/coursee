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
