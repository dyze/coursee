using Coursee.Core.Logging;

namespace Coursee.Core.Updates;

/// <summary>
/// Asks for the latest release at most once a day, and says whether it is newer than this copy.
/// coursee never downloads nor installs it: the user is shown its page. A failed check (offline,
/// no release published) is logged and tried again a day later.
/// </summary>
public sealed class UpdateChecker
{
    public const string LogCategory = "Update";

    /// <summary>The time between two checks.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    private readonly IReleaseFeed _feed;
    private readonly UpdateStore _store;
    private readonly string _currentVersion;
    private readonly TimeProvider _time;
    private readonly ILog _log;
    private int _checking;

    /// <param name="currentVersion">This copy's version (<see cref="AppVersion"/>).</param>
    public UpdateChecker(IReleaseFeed feed, UpdateStore store, string currentVersion, TimeProvider? time = null, ILog? log = null)
    {
        _feed = feed;
        _store = store;
        _currentVersion = currentVersion;
        _time = time ?? TimeProvider.System;
        _log = log ?? NullLog.Instance;
    }

    /// <summary>The newer release found by the last check, or null.</summary>
    public Release? Available =>
        _store.Latest is { } latest && Release.IsNewer(latest.Version, _currentVersion) ? latest : null;

    /// <summary>
    /// True when the newer release has not been seen in Settings > About yet: the Settings button
    /// and the About tab show a mark. Seen by an older install, it is announced again (the state
    /// restarts with each installed version); a later release is announced too.
    /// </summary>
    public bool ShouldNotify =>
        Available is { } release
        && !(string.Equals(_store.AcknowledgedVersion, release.Version, StringComparison.OrdinalIgnoreCase)
             && string.Equals(_store.AcknowledgedBy, _currentVersion, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The user has seen the newer release announced (Settings > About): stop marking it. No-op
    /// when there is none or it is already acknowledged. May throw an I/O error (saving).
    /// </summary>
    public void Acknowledge()
    {
        if (ShouldNotify && Available is { } release)
        {
            _store.Acknowledge(release.Version, _currentVersion);
            _log.Info(LogCategory, $"Release {release.Version} seen");
        }
    }

    /// <summary>When the last check ran, or null.</summary>
    public DateTimeOffset? LastCheck => _store.LastCheck;

    /// <summary>True when a day has passed since the last check (or it never ran).</summary>
    public bool IsDue => _store.LastCheck is not { } last || _time.GetUtcNow() - last >= Interval;

    /// <summary>
    /// Checks when due; one check at a time. True when it ran and succeeded. Never throws for a
    /// network or file failure: it is logged.
    /// </summary>
    public async Task<bool> CheckIfDueAsync(CancellationToken cancellation = default)
    {
        if (!IsDue || Interlocked.Exchange(ref _checking, 1) == 1)
        {
            return false;
        }

        try
        {
            Release? latest;
            var succeeded = true;
            try
            {
                latest = await _feed.LatestAsync(cancellation).ConfigureAwait(false);
                _log.Info(LogCategory, latest is null
                    ? "No release published"
                    : $"Latest release {latest.Version}{(Release.IsNewer(latest.Version, _currentVersion) ? ", newer than " + _currentVersion : string.Empty)}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
            {
                latest = null;
                succeeded = false;
                _log.Warn(LogCategory, $"Check failed: {ex.GetType().Name}: {ex.Message}");
            }

            try
            {
                _store.Record(_time.GetUtcNow(), latest, succeeded);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.Warn(LogCategory, $"Cannot save the check: {ex.GetType().Name}: {ex.Message}");
            }

            return succeeded;
        }
        finally
        {
            Interlocked.Exchange(ref _checking, 0);
        }
    }
}
