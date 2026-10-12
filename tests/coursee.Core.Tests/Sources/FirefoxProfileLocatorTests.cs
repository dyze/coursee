namespace Coursee.Core.Tests.Sources;

public sealed class FirefoxProfileLocatorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ParseDefault_PrefersTheInstallEntry()
    {
        const string ini = """
            [Install308046B0AF4A39CB]
            Default=Profiles/install.default
            Locked=1

            [Profile1]
            Name=old
            IsRelative=1
            Path=Profiles/old.default
            Default=1

            [Profile0]
            Name=default-release
            IsRelative=1
            Path=Profiles/install.default
            """;

        Assert.Equal(new ProfileLocation("Profiles/install.default", true), FirefoxProfileLocator.ParseDefault(ini));
    }

    [Fact]
    public void ParseDefault_FallsBackToTheProfileMarkedDefault()
    {
        const string ini = """
            [Profile0]
            Name=a
            IsRelative=1
            Path=Profiles/a

            [Profile1]
            Name=b
            IsRelative=1
            Path=Profiles/b
            Default=1
            """;

        Assert.Equal(new ProfileLocation("Profiles/b", true), FirefoxProfileLocator.ParseDefault(ini));
    }

    [Fact]
    public void ParseDefault_FallsBackToTheFirstProfile()
    {
        const string ini = """
            [General]
            Version=2

            [Profile0]
            Name=a
            IsRelative=1
            Path=Profiles/a
            """;

        Assert.Equal(new ProfileLocation("Profiles/a", true), FirefoxProfileLocator.ParseDefault(ini));
    }

    [Fact]
    public void ParseDefault_KnowsAboutAbsolutePaths()
    {
        const string ini = """
            [Profile0]
            Name=a
            IsRelative=0
            Path=D:\Firefox\a
            Default=1
            """;

        Assert.Equal(new ProfileLocation(@"D:\Firefox\a", false), FirefoxProfileLocator.ParseDefault(ini));
    }

    [Fact]
    public void ParseDefault_InstallEntryUsesItsProfilesRelativeFlag()
    {
        const string ini = """
            [Install1]
            Default=D:\Firefox\a

            [Profile0]
            IsRelative=0
            Path=D:\Firefox\a
            """;

        Assert.Equal(new ProfileLocation(@"D:\Firefox\a", false), FirefoxProfileLocator.ParseDefault(ini));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[General]\nVersion=2")]
    public void ParseDefault_NoProfile_ReturnsNull(string ini)
    {
        Assert.Null(FirefoxProfileLocator.ParseDefault(ini));
    }

    [Fact]
    public void FindDefaultProfile_ResolvesRelativeToTheRoot()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Profiles", "abc.default"));
        File.WriteAllText(Path.Combine(_root, "profiles.ini"), "[Profile0]\nIsRelative=1\nPath=Profiles/abc.default\nDefault=1\n");

        var result = FirefoxProfileLocator.FindDefaultProfile(_root);

        Assert.Equal(Path.Combine(_root, "Profiles/abc.default"), result);
    }

    [Fact]
    public void FindDefaultProfile_NoIniOrNoFolder_ReturnsNull()
    {
        Directory.CreateDirectory(_root);
        Assert.Null(FirefoxProfileLocator.FindDefaultProfile(_root));

        File.WriteAllText(Path.Combine(_root, "profiles.ini"), "[Profile0]\nIsRelative=1\nPath=Profiles/gone\nDefault=1\n");
        Assert.Null(FirefoxProfileLocator.FindDefaultProfile(_root));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
