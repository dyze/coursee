namespace Coursee.Core.Tests.Platform;

public class InstanceIdentityTests
{
    private static readonly InstanceIdentity Installed =
        new("0.8.1+97bddb2c5e1f0a4b", @"C:\Users\me\AppData\Local\Programs\coursee\coursee.exe");

    [Fact]
    public void Encode_ThenDecode_GivesTheSameIdentity()
    {
        Assert.Equal(Installed, InstanceIdentity.Decode(Installed.Encode()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0.8.1")]
    [InlineData("0.8.1\n")]
    [InlineData("\nC:\\coursee.exe")]
    [InlineData("0.8.1\nC:\\coursee.exe\nextra")]
    public void Decode_NotAnIdentity_IsNull(string? text)
    {
        Assert.Null(InstanceIdentity.Decode(text));
    }

    [Fact]
    public void IsSameBuildAs_IgnoresThePath()
    {
        var portable = Installed with { ExecutablePath = @"E:\coursee\coursee.exe" };

        Assert.True(Installed.IsSameBuildAs(portable));
    }

    [Theory]
    [InlineData("0.8.2+97bddb2c5e1f0a4b")]
    [InlineData("0.8.1+abc1234def567890")]
    [InlineData("0.8.1+97bddb2c5e1f0a4b.dirty")]
    public void IsSameBuildAs_OtherVersionCommitOrDirtyMark_IsFalse(string build)
    {
        Assert.False(Installed.IsSameBuildAs(Installed with { Build = build }));
    }

    [Fact]
    public void Describe_ShowsTheShortBuildAndThePath()
    {
        Assert.Equal(
            @"coursee 0.8.1 (97bddb2), from C:\Users\me\AppData\Local\Programs\coursee\coursee.exe",
            Installed.Describe());
    }
}
