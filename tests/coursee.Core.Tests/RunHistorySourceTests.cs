namespace Coursee.Core.Tests;

public class RunHistorySourceTests
{
    private sealed class FakeReader(Dictionary<string, string> values) : IRunHistoryReader
    {
        public IReadOnlyDictionary<string, string> Read() => values;
    }

    private sealed class ThrowingReader : IRunHistoryReader
    {
        public IReadOnlyDictionary<string, string> Read() => throw new UnauthorizedAccessException();
    }

    private static IReadOnlyList<Resource> Load(Dictionary<string, string> values) =>
        new RunHistorySource(new FakeReader(values)).GetResources();

    [Fact]
    public void Entries_FollowTheMruListOrder_AndDropTheSuffix()
    {
        var result = Load(new()
        {
            ["MRUList"] = "bca",
            ["a"] = "notepad\\1",
            ["b"] = "cmd /k dir\\1",
            ["c"] = "calc\\1",
        });

        Assert.Equal(new[] { "cmd /k dir", "calc", "notepad" }, result.Select(r => r.Name));
    }

    [Fact]
    public void Entries_AreCommandsFromTheWindowsRunSource()
    {
        var result = Load(new() { ["MRUList"] = "a", ["a"] = "notepad\\1" });

        Assert.Equal(new Resource("notepad", "notepad", ResourceKind.Command, "Windows Run"), Assert.Single(result));
    }

    [Fact]
    public void UrlEntries_AreOpenedAsUrls()
    {
        var result = Load(new() { ["MRUList"] = "ab", ["a"] = "https://example.com\\1", ["b"] = "www.example.org\\1" });

        Assert.Equal(
            new[]
            {
                new Resource("https://example.com", "https://example.com", ResourceKind.Url, "Windows Run"),
                new Resource("www.example.org", "https://www.example.org", ResourceKind.Url, "Windows Run"),
            },
            result);
    }

    [Fact]
    public void Duplicates_KeepTheMostRecentOne()
    {
        var result = Load(new() { ["MRUList"] = "ba", ["a"] = "Notepad\\1", ["b"] = "notepad\\1" });

        Assert.Equal(new[] { "notepad" }, result.Select(r => r.Name));
    }

    [Fact]
    public void MissingLettersAndEmptyEntries_AreSkipped()
    {
        var result = Load(new() { ["MRUList"] = "xab", ["a"] = "\\1", ["b"] = "calc\\1" });

        Assert.Equal(new[] { "calc" }, result.Select(r => r.Name));
    }

    [Fact]
    public void EntryWithoutSuffix_IsStillRead()
    {
        var result = Load(new() { ["MRUList"] = "a", ["a"] = "calc" });

        Assert.Equal(new[] { "calc" }, result.Select(r => r.Name));
    }

    [Fact]
    public void NoMruList_MeansNoResources()
    {
        Assert.Empty(Load(new() { ["a"] = "notepad\\1" }));
        Assert.Empty(Load(new()));
    }

    [Fact]
    public void UnreadableRegistry_MeansNoResources()
    {
        Assert.Empty(new RunHistorySource(new ThrowingReader()).GetResources());
    }

    [Fact]
    public void SourceName_IsWindowsRun()
    {
        Assert.Equal("Windows Run", new RunHistorySource(new FakeReader([])).Name);
    }
}
