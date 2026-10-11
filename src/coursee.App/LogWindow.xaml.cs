using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Coursee.Core;

namespace Coursee.UI;

/// <summary>Shows the recent log entries live, with a level filter and a text filter.</summary>
public partial class LogWindow : Window
{
    private const string AllLevels = "All levels";

    private readonly RingLog _ring;
    private readonly string _logDirectory;
    private readonly List<LogEntry> _visible = [];

    public LogWindow(RingLog ring, string logDirectory)
    {
        _ring = ring;
        _logDirectory = logDirectory;
        InitializeComponent();

        LevelComboBox.ItemsSource = new[] { AllLevels, "Info+", "Warning+", "Error" };
        LevelComboBox.SelectedIndex = 0;

        _ring.EntryAdded += OnEntryAdded;
        _ring.Cleared += OnCleared;
        Closed += (_, _) =>
        {
            _ring.EntryAdded -= OnEntryAdded;
            _ring.Cleared -= OnCleared;
        };

        Reload();
    }

    private LogLevel MinimumLevel => LevelComboBox.SelectedIndex switch
    {
        1 => LogLevel.Info,
        2 => LogLevel.Warning,
        3 => LogLevel.Error,
        _ => LogLevel.Debug,
    };

    private bool Matches(LogEntry entry)
    {
        if (entry.Level < MinimumLevel)
        {
            return false;
        }

        var text = FilterBox.Text.Trim();
        return text.Length == 0
            || entry.Message.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.Category.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private void Reload()
    {
        _visible.Clear();
        _visible.AddRange(_ring.Entries.Where(Matches));
        EntriesList.ItemsSource = null;
        EntriesList.ItemsSource = _visible.ToList();
        ScrollToEnd();
        UpdateStatus();
    }

    private void OnEntryAdded(LogEntry entry)
    {
        // Entries can come from any thread.
        Dispatcher.BeginInvoke(() =>
        {
            if (!Matches(entry))
            {
                return;
            }

            _visible.Add(entry);
            EntriesList.ItemsSource = null;
            EntriesList.ItemsSource = _visible.ToList();
            ScrollToEnd();
            UpdateStatus();
        });
    }

    private void OnCleared() => Dispatcher.BeginInvoke(Reload);

    private void ScrollToEnd()
    {
        if (EntriesList.Items.Count > 0)
        {
            EntriesList.ScrollIntoView(EntriesList.Items[^1]);
        }
    }

    private void UpdateStatus() =>
        StatusText.Text = $"{_visible.Count} lines shown. Files in {_logDirectory} (max {SerilogFileLog.FilesToKeep} x 1 MB).";

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            Reload();
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntriesList.SelectedItems.Count > 0
            ? EntriesList.SelectedItems.Cast<LogEntry>()
            : _visible;

        var text = string.Join(Environment.NewLine, entries.Select(entry => entry.Format()));
        if (text.Length > 0)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // The clipboard is busy; the user can try again.
            }
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_logDirectory);
        Process.Start(new ProcessStartInfo(_logDirectory) { UseShellExecute = true });
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => _ring.Clear();
}
