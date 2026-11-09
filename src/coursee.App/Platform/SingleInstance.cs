using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text;
using Coursee.Core.Platform;

namespace Coursee.UI.Platform;

/// <summary>
/// Keeps a single coursee running per Windows session. The first copy owns a named mutex, publishes
/// its <see cref="InstanceIdentity"/> in named shared memory, and waits on two named events: show
/// (it then shows its launcher) and quit (a copy of another build replaces it). It also listens on a
/// named pipe for a path to add (Explorer's <i>Add to coursee</i>), which an event cannot carry.
/// Without this, a second copy could not register the hotkey, and would open Settings to ask for another one.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\coursee-single-instance";
    private const string ShowEventName = @"Local\coursee-show-launcher";
    private const string QuitEventName = @"Local\coursee-quit";
    private const string IdentityName = @"Local\coursee-identity";

    // A length, then the identity's UTF-8 text. An identity too long for it is published empty (unknown).
    private const int IdentityCapacity = 4096;

    // How long a copy waits for the running one to take a path.
    private const int AddConnectTimeoutMs = 2000;

    // Pipe names are not per session like Local\ objects: the session id keeps two signed-in users apart.
    private static readonly string AddPipeName = $"coursee-add-{Process.GetCurrentProcess().SessionId}";

    private readonly Mutex _mutex;
    private readonly MemoryMappedFile _identity;
    private readonly EventWaitHandle _showEvent;
    private readonly EventWaitHandle _quitEvent;
    private readonly RegisteredWaitHandle _showWait;
    private readonly RegisteredWaitHandle _quitWait;
    private readonly CancellationTokenSource _stopListening = new();

    private SingleInstance(Mutex mutex, InstanceIdentity self, Callbacks callbacks)
    {
        _mutex = mutex;
        _identity = Publish(self);
        _showEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, ShowEventName);
        _quitEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, QuitEventName);
        _showWait = WaitFor(_showEvent, callbacks.ShowRequested);
        _quitWait = WaitFor(_quitEvent, callbacks.QuitRequested);
        _ = ListenForPaths(callbacks.AddRequested, _stopListening.Token);
    }

    /// <summary>What the running copy is asked by another one. Called on a background thread.</summary>
    /// <param name="AddRequested">A file or folder to make a custom entry of.</param>
    public sealed record Callbacks(Action ShowRequested, Action QuitRequested, Action<string> AddRequested);

    /// <summary>
    /// Returns the instance guard when this is the first copy; null when another copy is already running.
    /// </summary>
    public static SingleInstance? TryClaim(InstanceIdentity self, Callbacks callbacks)
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstance(mutex, self, callbacks);
    }

    /// <summary>
    /// Asks the running copy to quit, and returns the instance guard once it has. Null when it did not
    /// close within <paramref name="timeout"/>.
    /// </summary>
    public static SingleInstance? TryReplace(InstanceIdentity self, Callbacks callbacks, TimeSpan timeout)
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

        return new SingleInstance(mutex, self, callbacks);
    }

    /// <summary>
    /// Asks the running copy to add <paramref name="pathToAdd"/> (it then shows its launcher with it),
    /// or, without a path or when it does not take it (0.8.1 and older have no pipe), to show its launcher.
    /// </summary>
    public static void RequestShow(string? pathToAdd = null)
    {
        NativeWindows.AllowAnyToTakeForeground();
        if (pathToAdd is null || !TrySendPath(pathToAdd))
        {
            TrySet(ShowEventName);
        }
    }

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
        // Not disposed: the listening task may still read its token while it ends.
        _stopListening.Cancel();
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

    /// <summary>One path per connection, in UTF-8, until the other copy closes the pipe. Only the same user can connect.</summary>
    private static async Task ListenForPaths(Action<string> onAddRequested, CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    AddPipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(stop);
                using var reader = new StreamReader(pipe, Encoding.UTF8);
                var path = (await reader.ReadToEndAsync(stop)).Trim();
                if (path.Length > 0)
                {
                    onAddRequested(path);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A copy that left mid-message, or the pipe still held by a copy being replaced: try again.
                if (!await Pause(stop))
                {
                    return;
                }
            }
        }
    }

    /// <summary>Waits a second; false when stopped meanwhile.</summary>
    private static async Task<bool> Pause(CancellationToken stop)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stop);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static bool TrySendPath(string path)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", AddPipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            pipe.Connect(AddConnectTimeoutMs);
            pipe.Write(Encoding.UTF8.GetBytes(path));
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

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
