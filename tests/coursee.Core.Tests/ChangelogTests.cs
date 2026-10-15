namespace Coursee.Core.Tests;

public sealed class ChangelogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "coursee-tests", Guid.NewGuid().ToString("N"));

    public ChangelogTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void TheFile_IsReadAsItIs()
    {
        var path = Path.Combine(_folder, "CHANGELOG.md");
        File.WriteAllText(path, "# Changelog\n\n## [Unreleased]\n");

        Assert.Equal("# Changelog\n\n## [Unreleased]\n", Changelog.Read(path));
    }

    [Fact]
    public void AMissingFile_SaysSo()
    {
        var path = Path.Combine(_folder, "CHANGELOG.md");

        Assert.Equal($"Changelog not found: {path}", Changelog.Read(path));
    }
}
