namespace Coursee.Core.Logging;

/// <summary>
/// Tells when the UI thread stops answering. A watcher on another thread asks it, now and then,
/// to answer; an ask left unanswered past the threshold is a freeze, reported once, and so is its end.
/// Thread-safe: the watcher and the UI thread both call it.
/// </summary>
public sealed class FreezeWatch(TimeSpan threshold, TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly Lock _gate = new();
    private DateTimeOffset? _askedAt;
    private bool _reported;

    /// <summary>Called by the watcher before it asks. False while the last ask is unanswered: no new one is needed.</summary>
    public bool Ask()
    {
        lock (_gate)
        {
            if (_askedAt is not null)
            {
                return false;
            }

            _askedAt = _clock.GetUtcNow();
            return true;
        }
    }

    /// <summary>Called on the UI thread when it answers. How long it was frozen, when that was reported; else null.</summary>
    public TimeSpan? Answered()
    {
        lock (_gate)
        {
            var frozenFor = _reported && _askedAt is { } askedAt ? _clock.GetUtcNow() - askedAt : (TimeSpan?)null;
            _askedAt = null;
            _reported = false;
            return frozenFor;
        }
    }

    /// <summary>Called by the watcher: how long the UI thread has not answered, once it passes the threshold; else null.</summary>
    public TimeSpan? Frozen()
    {
        lock (_gate)
        {
            if (_askedAt is not { } askedAt || _reported)
            {
                return null;
            }

            var waited = _clock.GetUtcNow() - askedAt;
            if (waited < threshold)
            {
                return null;
            }

            _reported = true;
            return waited;
        }
    }
}
