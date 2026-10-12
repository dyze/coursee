namespace Coursee.Core.Tests.Logging;

public class LogPipelineTests
{
    private sealed class ThrowingLog : ILog
    {
        public void Write(LogEntry entry) => throw new InvalidOperationException("broken sink");
    }

    [Fact]
    public void FilteredLog_DropsEntriesBelowTheMinimum()
    {
        var sink = new RingLog();
        var log = new FilteredLog(sink, LogLevel.Info);

        log.Debug("c", "hidden");
        log.Info("c", "shown");
        log.Error("c", "also shown");

        Assert.Equal(new[] { "shown", "also shown" }, sink.Entries.Select(e => e.Message).ToArray());
    }

    [Fact]
    public void FilteredLog_MinimumCanChangeAtRuntime()
    {
        var sink = new RingLog();
        var log = new FilteredLog(sink, LogLevel.Info);

        log.Minimum = LogLevel.Debug;
        log.Debug("c", "now shown");

        Assert.Single(sink.Entries);
    }

    [Fact]
    public void CompositeLog_SendsToEveryLog_EvenWhenOneThrows()
    {
        var first = new RingLog();
        var second = new RingLog();
        var log = new CompositeLog(first, new ThrowingLog(), second);

        log.Info("c", "hello");

        Assert.Single(first.Entries);
        Assert.Single(second.Entries);
    }

    [Fact]
    public void ErrorWithException_AppendsTypeAndMessage()
    {
        var sink = new RingLog();

        sink.Error("c", "failed", new IOException("disk gone"));

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("failed: IOException: disk gone", entry.Message);
    }

    [Fact]
    public void NullLog_DiscardsEverything()
    {
        NullLog.Instance.Info("c", "ignored");
    }
}
