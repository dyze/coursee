namespace Coursee.Core.Tests;

public class SourceLoaderTests
{
    private sealed class FixedSource(string name, params Resource[] resources) : IResourceSource
    {
        public string Name => name;

        public IReadOnlyList<Resource> GetResources() => resources;
    }

    private sealed class FailingSource(string name) : IResourceSource
    {
        public string Name => name;

        public IReadOnlyList<Resource> GetResources() => throw new InvalidOperationException("boom");
    }

    private static Resource R(string name, string source) => new(name, @"C:\" + name, ResourceKind.Folder, source);

    [Fact]
    public void Load_ReturnsEachSourceInOrder()
    {
        var results = SourceLoader.Load([new FixedSource("A", R("a", "A")), new FixedSource("B", R("b", "B"))]);

        Assert.Equal(new[] { "A", "B" }, results.Select(r => r.SourceName));
        Assert.Equal("a", Assert.Single(results[0].Resources).Name);
        Assert.All(results, r => Assert.Null(r.Error));
    }

    [Fact]
    public void FailingSource_IsReportedAndDoesNotStopTheOthers()
    {
        var results = SourceLoader.Load([new FailingSource("Bad"), new FixedSource("Good", R("g", "Good"))]);

        Assert.Equal(2, results.Count);
        Assert.Empty(results[0].Resources);
        Assert.IsType<InvalidOperationException>(results[0].Error);
        Assert.Equal("g", Assert.Single(results[1].Resources).Name);
    }

    [Fact]
    public void NoSources_MeansNoResults()
    {
        Assert.Empty(SourceLoader.Load([]));
    }
}
