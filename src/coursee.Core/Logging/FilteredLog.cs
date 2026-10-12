namespace Coursee.Core.Logging;

/// <summary>Passes on only the entries at or above a minimum level. The level can change at runtime.</summary>
public sealed class FilteredLog : ILog
{
    private readonly ILog _inner;

    public FilteredLog(ILog inner, LogLevel minimum)
    {
        _inner = inner;
        Minimum = minimum;
    }

    public LogLevel Minimum { get; set; }

    public void Write(LogEntry entry)
    {
        if (entry.Level >= Minimum)
        {
            _inner.Write(entry);
        }
    }
}
