namespace Coursee.Core.Tests;

public class AppVersionTests
{
    [Fact]
    public void Format_KeepsPlainVersion()
    {
        Assert.Equal("0.1", AppVersion.Format("0.1"));
    }

    [Fact]
    public void Format_DropsBuildMetadata()
    {
        Assert.Equal("0.2", AppVersion.Format("0.2+3f9a1c7d"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Format_MissingVersion_IsUnknown(string? raw)
    {
        Assert.Equal("unknown", AppVersion.Format(raw));
    }

    [Fact]
    public void Of_BuiltAssembly_HasAVersionWithoutMetadata()
    {
        var version = AppVersion.Of(typeof(AppVersion).Assembly);

        Assert.NotEqual("unknown", version);
        Assert.DoesNotContain("+", version);
    }
}
