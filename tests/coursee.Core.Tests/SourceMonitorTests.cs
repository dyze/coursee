namespace Coursee.Core.Tests;

public class SourceMonitorTests
{
    private readonly RingLog _log = new();

    private static Resource R(string name) => new(name, "target-" + name, ResourceKind.File, "S");

    private static IReadOnlyList<Resource> Items(params string[] names) => names.Select(R).ToList();

    [Fact]
    public void FirstObservation_LogsTheCountAtInfo()
    {
        new SourceMonitor(_log).Observe("S", Items("a", "b"));

        var entry = Assert.Single(_log.Entries);
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal("S", entry.Category);
        Assert.Contains("2 items", entry.Message);
    }

    [Fact]
    public void NoChange_LogsAtDebug()
    {
        var monitor = new SourceMonitor(_log);
        monitor.Observe("S", Items("a"));
        monitor.Observe("S", Items("a"));

        Assert.Equal(LogLevel.Debug, _log.Entries[1].Level);
    }

    [Fact]
    public void AddedAndRemoved_AreListedByName()
    {
        var monitor = new SourceMonitor(_log);
        monitor.Observe("S", Items("a", "b"));
        monitor.Observe("S", Items("b", "c"));

        var entry = _log.Entries[1];
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Contains("+1 -1", entry.Message);
        Assert.Contains("added: 'c'", entry.Message);
        Assert.Contains("removed: 'a'", entry.Message);
    }

    [Fact]
    public void ManyChanges_AreSummarised()
    {
        var monitor = new SourceMonitor(_log);
        monitor.Observe("S", Items());
        monitor.Observe("S", Items("1", "2", "3", "4", "5", "6", "7"));

        Assert.Contains("and 2 more", _log.Entries[1].Message);
    }

    [Fact]
    public void Reset_MakesTheNextObservationAFreshLoad()
    {
        var monitor = new SourceMonitor(_log);
        monitor.Observe("S", Items("a"));
        monitor.Reset("S");
        monitor.Observe("S", Items("a"));

        Assert.Contains("loaded", _log.Entries[1].Message);
    }

    [Fact]
    public void SourcesAreTrackedIndependently()
    {
        var monitor = new SourceMonitor(_log);
        monitor.Observe("S", Items("a"));
        monitor.Observe("T", Items("b"));

        Assert.All(_log.Entries, e => Assert.Contains("loaded", e.Message));
    }
}
