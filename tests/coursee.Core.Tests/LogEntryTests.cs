namespace Coursee.Core.Tests;

public class LogEntryTests
{
    [Fact]
    public void Create_KeepsShortMessages()
    {
        var entry = LogEntry.Create(LogLevel.Info, "Cat", "hello");

        Assert.Equal("hello", entry.Message);
        Assert.Equal("Cat", entry.Category);
        Assert.Equal(LogLevel.Info, entry.Level);
    }

    [Fact]
    public void Create_CutsLongMessages()
    {
        var entry = LogEntry.Create(LogLevel.Info, "Cat", new string('x', 5000));

        Assert.Equal(LogEntry.MaxMessageLength, entry.Message.Length);
        Assert.EndsWith("…", entry.Message);
    }

    [Theory]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Info, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    public void LevelText_IsThreeLetters(LogLevel level, string expected)
    {
        Assert.Equal(expected, LogEntry.Create(level, "c", "m").LevelText);
    }

    [Fact]
    public void Format_IsOneLineWithTimeLevelCategoryAndMessage()
    {
        var time = new DateTimeOffset(2026, 3, 4, 5, 6, 7, 89, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 3, 4)));
        var entry = LogEntry.Create(LogLevel.Warning, "Firefox", "oops", time);

        Assert.Equal("2026-03-04 05:06:07.089 [WRN] Firefox: oops", entry.Format());
    }
}
