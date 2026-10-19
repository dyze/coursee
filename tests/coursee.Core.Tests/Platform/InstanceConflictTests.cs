namespace Coursee.Core.Tests.Platform;

public class InstanceConflictTests
{
    private static readonly InstanceIdentity Running = new("0.8.1+97bddb2c5e1f0a4b", @"C:\installed\coursee.exe");
    private static readonly InstanceIdentity Starting = new("0.8.2+abc1234def567890.dirty", @"C:\repo\artifacts\coursee.exe");

    [Fact]
    public void Compare_NoIdentity_IsUnknown()
    {
        Assert.Equal(InstanceConflictKind.Unknown, InstanceConflict.Compare(null, Starting));
    }

    [Fact]
    public void Compare_SameBuild_IsSameBuild()
    {
        var samePortable = Running with { ExecutablePath = @"E:\coursee\coursee.exe" };

        Assert.Equal(InstanceConflictKind.SameBuild, InstanceConflict.Compare(Running, samePortable));
    }

    [Fact]
    public void Compare_OtherBuild_IsOtherBuild()
    {
        Assert.Equal(InstanceConflictKind.OtherBuild, InstanceConflict.Compare(Running, Starting));
    }

    [Fact]
    public void ReplaceQuestion_NamesBothBuildsAndPaths()
    {
        Assert.Equal(
            "coursee 0.8.1 (97bddb2), from C:\\installed\\coursee.exe, is already running.\n\n"
            + "This copy is coursee 0.8.2 (abc1234-dirty), from C:\\repo\\artifacts\\coursee.exe.\n\n"
            + "Close the running one and start this one?",
            InstanceConflict.ReplaceQuestion(Running, Starting));
    }
}
