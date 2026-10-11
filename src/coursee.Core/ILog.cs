namespace Coursee.Core;

/// <summary>Destination for log entries. Implementations must never throw into the caller.</summary>
public interface ILog
{
    void Write(LogEntry entry);
}

/// <summary>A log that discards everything. Used when no log is supplied.</summary>
public sealed class NullLog : ILog
{
    public static NullLog Instance { get; } = new();

    public void Write(LogEntry entry)
    {
    }
}

/// <summary>Shorthand to write an entry of a given level.</summary>
public static class LogExtensions
{
    public static void Debug(this ILog log, string category, string message) =>
        log.Write(LogEntry.Create(LogLevel.Debug, category, message));

    public static void Info(this ILog log, string category, string message) =>
        log.Write(LogEntry.Create(LogLevel.Info, category, message));

    public static void Warn(this ILog log, string category, string message) =>
        log.Write(LogEntry.Create(LogLevel.Warning, category, message));

    public static void Error(this ILog log, string category, string message) =>
        log.Write(LogEntry.Create(LogLevel.Error, category, message));

    public static void Error(this ILog log, string category, string message, Exception exception) =>
        log.Error(category, $"{message}: {exception.GetType().Name}: {exception.Message}");
}
