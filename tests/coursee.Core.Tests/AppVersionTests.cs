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

    [Fact]
    public void FormatWithCommit_ShortensTheCommit()
    {
        Assert.Equal("0.8.1 (97bddb2)", AppVersion.FormatWithCommit("0.8.1+97bddb2c5e1f0a4b"));
    }

    [Fact]
    public void FormatWithCommit_MarksADirtyBuild()
    {
        Assert.Equal("0.8.1 (97bddb2-dirty)", AppVersion.FormatWithCommit("0.8.1+97bddb2c5e1f0a4b.dirty"));
    }

    [Fact]
    public void FormatWithCommit_KeepsAShortCommitWhole()
    {
        Assert.Equal("0.8.1 (abc)", AppVersion.FormatWithCommit("0.8.1+abc"));
    }

    [Theory]
    [InlineData("0.8.1")]
    [InlineData("0.8.1+")]
    [InlineData("0.8.1+.dirty")]
    public void FormatWithCommit_WithoutCommit_IsTheVersion(string raw)
    {
        Assert.Equal("0.8.1", AppVersion.FormatWithCommit(raw));
    }

    [Fact]
    public void FormatWithCommit_MissingVersion_IsUnknown()
    {
        Assert.Equal("unknown", AppVersion.FormatWithCommit(null));
    }

    [Fact]
    public void WithCommitOf_BuiltAssembly_StartsWithItsVersion()
    {
        var assembly = typeof(AppVersion).Assembly;

        Assert.StartsWith(AppVersion.Of(assembly), AppVersion.WithCommitOf(assembly));
    }
}
