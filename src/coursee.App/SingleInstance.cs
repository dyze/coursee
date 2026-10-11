namespace Coursee.UI;

/// <summary>
/// Keeps a single coursee running per Windows session. The first copy owns a named mutex and
/// waits on a named event; a later copy sets the event (the first copy then shows its
/// launcher) and exits. Without this, a second copy could not register the hotkey and would
/// fall back to the default one, overwriting the saved setting.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\coursee-single-instance";
    private const string ShowEventName = @"Local\coursee-show-launcher";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _showEvent;
    private readonly RegisteredWaitHandle _wait;

    private SingleInstance(Mutex mutex, EventWaitHandle showEvent, Action onShowRequested)
    {
        _mutex = mutex;
        _showEvent = showEvent;
        _wait = ThreadPool.RegisterWaitForSingleObject(
            showEvent,
            (_, _) => onShowRequested(),
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    /// <summary>
    /// Returns the instance guard when this is the first copy. Returns null when another copy
    /// is already running; that copy has then been asked to show its launcher.
    /// <paramref name="onShowRequested"/> is called on a background thread.
    /// </summary>
    public static SingleInstance? TryClaim(Action onShowRequested)
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            if (EventWaitHandle.TryOpenExisting(ShowEventName, out var existing))
            {
                using (existing)
                {
                    existing.Set();
                }
            }

            return null;
        }

        var showEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, ShowEventName);
        return new SingleInstance(mutex, showEvent, onShowRequested);
    }

    public void Dispose()
    {
        _wait.Unregister(null);
        _showEvent.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
