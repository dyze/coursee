using Coursee.Core;

namespace Coursee.UI;

/// <summary>
/// The application's logging setup: every entry goes through a level filter, then to the
/// in-memory ring (log window) and the rolling file.
/// </summary>
public sealed class LogHub : IDisposable
{
    private readonly SerilogFileLog? _file;

    public LogHub(string directory, bool debug)
    {
        Ring = new RingLog();
        Directory = directory;

        try
        {
            _file = new SerilogFileLog(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // No log file (folder not writable): the log window still works.
            _file = null;
        }

        ILog sink = _file is null ? Ring : new CompositeLog(Ring, _file);
        Filter = new FilteredLog(sink, LevelFor(debug));
        if (_file is null)
        {
            Filter.Warn("App", $"Cannot write log files in {directory}; the log is kept in memory only");
        }
    }

    public RingLog Ring { get; }

    /// <summary>The log to hand to everything that writes entries.</summary>
    public FilteredLog Filter { get; }

    public string Directory { get; }

    public void SetDebug(bool debug) => Filter.Minimum = LevelFor(debug);

    public void Dispose() => _file?.Dispose();

    private static LogLevel LevelFor(bool debug) => debug ? LogLevel.Debug : LogLevel.Info;
}
