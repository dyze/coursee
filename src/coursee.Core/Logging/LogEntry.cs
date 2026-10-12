namespace Coursee.Core.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

/// <summary>One line of the activity log.</summary>
public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Category, string Message)
{
    /// <summary>Longer messages are cut, to keep the log small.</summary>
    public const int MaxMessageLength = 1000;

    /// <summary>Time of day, for display in the log window.</summary>
    public string TimeText => Time.LocalDateTime.ToString("HH:mm:ss.fff");

    /// <summary>Three-letter level, for display.</summary>
    public string LevelText => Level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "???",
    };

    public static LogEntry Create(LogLevel level, string category, string message, DateTimeOffset? time = null) =>
        new(time ?? DateTimeOffset.Now, level, category, Truncate(message));

    /// <summary>The whole entry as one line of text, as written to the file and copied from the log window.</summary>
    public string Format() =>
        $"{Time.LocalDateTime:yyyy-MM-dd HH:mm:ss.fff} [{LevelText}] {Category}: {Message}";

    private static string Truncate(string message) =>
        message.Length <= MaxMessageLength ? message : message[..(MaxMessageLength - 1)] + "…";
}
