namespace Coursee.Core;

/// <summary>Sends every entry to several logs. A failing log never affects the others or the caller.</summary>
public sealed class CompositeLog : ILog
{
    private readonly ILog[] _logs;

    public CompositeLog(params ILog[] logs)
    {
        _logs = logs;
    }

    public void Write(LogEntry entry)
    {
        foreach (var log in _logs)
        {
            try
            {
                log.Write(entry);
            }
            catch (Exception)
            {
                // Logging must never break the application; a broken log is simply skipped.
            }
        }
    }
}
