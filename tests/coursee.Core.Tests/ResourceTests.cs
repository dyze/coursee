using System.Text.Json;

namespace Coursee.Core.Tests;

public class ResourceTests
{
    [Theory]
    [InlineData(ResourceKind.File, "Open file")]
    [InlineData(ResourceKind.Folder, "Open folder")]
    [InlineData(ResourceKind.Url, "Open URL")]
    [InlineData(ResourceKind.Command, "Run command")]
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
    }
}
