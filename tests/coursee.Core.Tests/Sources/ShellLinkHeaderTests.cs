namespace Coursee.Core.Tests.Sources;

public sealed class ShellLinkHeaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    public ShellLinkHeaderTests() => Directory.CreateDirectory(_directory);

    /// <summary>The 76 bytes of a ShellLinkHeader with these target attributes.</summary>
    public static byte[] Header(int attributes, Guid? clsid = null)
    {
        var header = new byte[0x4C];
        BitConverter.GetBytes(0x4C).CopyTo(header, 0);
        (clsid ?? new Guid("00021401-0000-0000-c000-000000000046")).ToByteArray().CopyTo(header, 0x04);
        BitConverter.GetBytes(attributes).CopyTo(header, 0x18);
        return header;
    }

    private string Write(byte[] content)
    {
        var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".lnk");
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public void ADirectoryTarget_IsAFolder() =>
        Assert.True(ShellLinkHeader.TargetIsFolder(Write(Header(0x10 | 0x01))));

    [Fact]
    public void AFileTarget_IsNoFolder() =>
        Assert.False(ShellLinkHeader.TargetIsFolder(Write([.. Header(0x20), 1, 2, 3])));

    [Fact]
    public void NoShortcut_GivesNull()
    {
        Assert.Null(ShellLinkHeader.TargetIsFolder(Write([])));
        Assert.Null(ShellLinkHeader.TargetIsFolder(Write(Header(0x10)[..0x20])));
        Assert.Null(ShellLinkHeader.TargetIsFolder(Write(Header(0x10, Guid.Empty))));
        Assert.Null(ShellLinkHeader.TargetIsFolder(Write([.. new byte[0x4C]])));
        Assert.Null(ShellLinkHeader.TargetIsFolder(Path.Combine(_directory, "missing.lnk")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
