using System.Text;
using Coursee.Core;
using Serilog;
using Serilog.Events;

namespace Coursee.UI;

/// <summary>
/// Writes the log to a text file with Serilog. The file is capped at 1 MB, then a new one is
/// started; only the 3 most recent files are kept, so the log never exceeds about 3 MB.
/// </summary>
public sealed class SerilogFileLog : ILog, IDisposable
{
    public const long MaxFileBytes = 1_048_576;
    public const int FilesToKeep = 3;

    private readonly Serilog.Core.Logger _logger;

    public SerilogFileLog(string directory)
    {
        Directory = directory;
        _logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(
                Path.Combine(directory, "coursee.log"),
                outputTemplate: "{Message:l}{NewLine}",
                fileSizeLimitBytes: MaxFileBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: FilesToKeep,
                encoding: Encoding.UTF8)
            .CreateLogger();
    }

    /// <summary>The folder holding the log files.</summary>
    public string Directory { get; }

    public void Write(LogEntry entry)
    {
        var level = entry.Level switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Information,
        };

        // The whole line is passed as a value, so braces in messages are never read as a template.
        _logger.Write(level, "{Line:l}", entry.Format());
    }

    public void Dispose() => _logger.Dispose();
}
