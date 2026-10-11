namespace Coursee.Core;

/// <summary>
/// Keeps the most recent entries in memory (the oldest are dropped beyond the capacity).
/// Feeds the log window. Entries can arrive from any thread.
/// </summary>
public sealed class RingLog : ILog
{
    private readonly object _lock = new();
    private readonly Queue<LogEntry> _entries = new();
    private readonly int _capacity;

    public RingLog(int capacity = 1000)
    {
        _capacity = Math.Max(1, capacity);
    }

    /// <summary>Raised after an entry was added, possibly on a background thread.</summary>
    public event Action<LogEntry>? EntryAdded;

    /// <summary>Raised after <see cref="Clear"/>.</summary>
    public event Action? Cleared;

    /// <summary>A snapshot of the kept entries, oldest first.</summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _capacity)
            {
                _entries.Dequeue();
            }
        }

        EntryAdded?.Invoke(entry);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        Cleared?.Invoke();
    }
}
