using System.Windows.Threading;
using Coursee.Core.Logging;

namespace Coursee.UI.Platform;

/// <summary>
/// Logs a Warning when the UI thread stops answering for <see cref="Threshold"/>, and when it
/// answers again, so that a freeze leaves a trace even if coursee is then ended by force.
/// </summary>
public sealed class UiWatchdog : IDisposable
{
    private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Every = TimeSpan.FromMilliseconds(500);

    private readonly FreezeWatch _watch = new(Threshold);
    private readonly Dispatcher _ui;
    private readonly ILog _log;
    private readonly Timer _timer;

    public UiWatchdog(Dispatcher ui, ILog log)
    {
        _ui = ui;
        _log = log;
        _timer = new Timer(_ => Check(), null, Every, Every);
    }

    /// <summary>On a pool thread.</summary>
    private void Check()
    {
        if (_watch.Frozen() is { } waited)
        {
            _log.Warn("App", $"The window has not answered for {waited.TotalSeconds:0.0} s");
        }

        // Input priority: what the user waits for when typing or clicking.
        if (_watch.Ask())
        {
            _ui.BeginInvoke(Answer, DispatcherPriority.Input);
        }
    }

    private void Answer()
    {
        if (_watch.Answered() is { } frozenFor)
        {
            _log.Warn("App", $"The window answers again, after {frozenFor.TotalSeconds:0.0} s");
        }
    }

    public void Dispose() => _timer.Dispose();
}
