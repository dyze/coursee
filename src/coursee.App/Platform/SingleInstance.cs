using System.IO.MemoryMappedFiles;
using System.Text;
using Coursee.Core.Platform;

namespace Coursee.UI.Platform;

/// <summary>
/// Keeps a single coursee running per Windows session. The first copy owns a named mutex, publishes
/// its <see cref="InstanceIdentity"/> in named shared memory, and waits on two named events: show
/// (it then shows its launcher) and quit (a copy of another build replaces it). Without this, a
/// second copy could not register the hotkey, and would open Settings to ask for another one.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\coursee-single-instance";
    private const string ShowEventName = @"Local\coursee-show-launcher";
    private const string QuitEventName = @"Local\coursee-quit";
    private const string IdentityName = @"Local\coursee-identity";

    // A length, then the identity's UTF-8 text. An identity too long for it is published empty (unknown).
    private const int IdentityCapacity = 4096;

    private readonly Mutex _mutex;
    private readonly MemoryMappedFile _identity;
    private readonly EventWaitHandle _showEvent;
    private readonly EventWaitHandle _quitEvent;
    private readonly RegisteredWaitHandle _showWait;
    private readonly RegisteredWaitHandle _quitWait;

    private SingleInstance(Mutex mutex, InstanceIdentity self, Action onShowRequested, Action onQuitRequested)
    {
        _mutex = mutex;
        _identity = Publish(self);
        _showEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, ShowEventName);
        _quitEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, QuitEventName);
        _showWait = WaitFor(_showEvent, onShowRequested);
        _quitWait = WaitFor(_quitEvent, onQuitRequested);
    }

    /// <summary>
    /// Returns the instance guard when this is the first copy; null when another copy is already running.
    /// The callbacks are called on a background thread.
    /// </summary>
    public static SingleInstance? TryClaim(InstanceIdentity self, Action onShowRequested, Action onQuitRequested)
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstance(mutex, self, onShowRequested, onQuitRequested);
    }

    /// <summary>
    /// Asks the running copy to quit, and returns the instance guard once it has. Null when it did not
    /// close within <paramref name="timeout"/>.
    /// </summary>
    public static SingleInstance? TryReplace(
        InstanceIdentity self,
        Action onShowRequested,
        Action onQuitRequested,
        TimeSpan timeout)
    {
        if (!TrySet(QuitEventName))
        {
            return null;
        }

        var mutex = new Mutex(initiallyOwned: false, MutexName);
        bool owned;
        try
        {
            owned = mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            // The running copy ended without releasing it (killed): the mutex is ours.
            owned = true;
        }

        if (!owned)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstance(mutex, self, onShowRequested, onQuitRequested);
    }

    /// <summary>Asks the running copy to show its launcher.</summary>
    public static void RequestShow() => TrySet(ShowEventName);

    /// <summary>The running copy's identity; null when it publishes none (0.8.1 and older) or it cannot be read.</summary>
    public static InstanceIdentity? ReadRunning()
    {
        try
        {
            using var file = MemoryMappedFile.OpenExisting(IdentityName, MemoryMappedFileRights.Read);
            using var view = file.CreateViewAccessor(0, IdentityCapacity, MemoryMappedFileAccess.Read);
            var length = view.ReadInt32(0);
            if (length <= 0 || length > IdentityCapacity - sizeof(int))
            {
                return null;
            }

            var bytes = new byte[length];
            view.ReadArray(sizeof(int), bytes, 0, length);
            return InstanceIdentity.Decode(Encoding.UTF8.GetString(bytes));
        }
        catch (Exception exception) when (exception is FileNotFoundException or UnauthorizedAccessException)
        {
            // None published, or by a copy run as administrator.
            return null;
        }
    }

    public void Dispose()
    {
        _showWait.Unregister(null);
        _quitWait.Unregister(null);
        _showEvent.Dispose();
        _quitEvent.Dispose();
        _identity.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    private static MemoryMappedFile Publish(InstanceIdentity self)
    {
        var bytes = Encoding.UTF8.GetBytes(self.Encode());
        var fits = bytes.Length <= IdentityCapacity - sizeof(int);
        var file = MemoryMappedFile.CreateOrOpen(IdentityName, IdentityCapacity, MemoryMappedFileAccess.ReadWrite);
        using var view = file.CreateViewAccessor(0, IdentityCapacity);
        view.Write(0, fits ? bytes.Length : 0);
        if (fits)
        {
            view.WriteArray(sizeof(int), bytes, 0, bytes.Length);
        }

        return file;
    }

    private static RegisteredWaitHandle WaitFor(EventWaitHandle signal, Action onSignal) =>
        ThreadPool.RegisterWaitForSingleObject(
            signal,
            (_, _) => onSignal(),
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);

    private static bool TrySet(string eventName)
    {
        if (!EventWaitHandle.TryOpenExisting(eventName, out var signal))
        {
            return false;
        }

        using (signal)
        {
            signal.Set();
        }

        return true;
    }
}
