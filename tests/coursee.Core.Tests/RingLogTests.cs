namespace Coursee.Core.Tests;

public class RingLogTests
{
    [Fact]
    public void KeepsEntriesOldestFirst()
    {
        var log = new RingLog();
        log.Info("c", "one");
        log.Info("c", "two");

        Assert.Equal(new[] { "one", "two" }, log.Entries.Select(e => e.Message).ToArray());
    }

    [Fact]
    public void DropsTheOldestBeyondCapacity()
    {
        var log = new RingLog(2);
        log.Info("c", "one");
        log.Info("c", "two");
        log.Info("c", "three");

        Assert.Equal(new[] { "two", "three" }, log.Entries.Select(e => e.Message).ToArray());
    }

    [Fact]
    public void RaisesEntryAdded()
    {
        var log = new RingLog();
        LogEntry? seen = null;
        log.EntryAdded += entry => seen = entry;

        log.Warn("c", "careful");

        Assert.Equal("careful", seen?.Message);
    }

    [Fact]
    public void Clear_EmptiesAndRaisesCleared()
    {
        var log = new RingLog();
        var cleared = false;
        log.Cleared += () => cleared = true;
        log.Info("c", "one");

        log.Clear();

        Assert.Empty(log.Entries);
        Assert.True(cleared);
    }

    [Fact]
    public void Entries_IsASnapshot()
    {
        var log = new RingLog();
        log.Info("c", "one");
        var snapshot = log.Entries;

        log.Info("c", "two");

        Assert.Single(snapshot);
    }
}
