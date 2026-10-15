namespace Coursee.Core;

/// <summary>One of coursee's files or folders, as listed in Settings > About.</summary>
public sealed record AppFile(string Label, string Path);

/// <summary>
/// Where coursee keeps its files: the user data (settings, entries, recents, window positions,
/// logs) in one folder, and the program. The single place their names are written.
/// </summary>
public sealed class AppFiles
{
    /// <summary>Next to coursee.exe, it makes a portable copy: the data is kept beside the program.</summary>
    public const string PortableMarker = "portable.txt";

    /// <param name="dataDirectory">The user data folder (<c>%LOCALAPPDATA%\coursee</c>).</param>
    /// <param name="programDirectory">The folder of coursee.exe.</param>
    /// <param name="isPortable">True when the data is kept beside the program.</param>
    public AppFiles(string dataDirectory, string programDirectory, bool isPortable = false)
    {
        DataDirectory = dataDirectory;
        ProgramDirectory = programDirectory;
        IsPortable = isPortable;
    }

    /// <summary>
    /// The files of the copy in <paramref name="programDirectory"/>: with the
    /// <see cref="PortableMarker"/> beside it, the data goes to its <c>data</c> folder (a USB stick,
    /// say), else to <c>coursee</c> in <paramref name="localAppData"/>.
    /// </summary>
    public static AppFiles For(string programDirectory, string localAppData, Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        return fileExists(Path.Combine(programDirectory, PortableMarker))
            ? new AppFiles(Path.Combine(programDirectory, "data"), programDirectory, isPortable: true)
            : new AppFiles(Path.Combine(localAppData, "coursee"), programDirectory);
    }

    public string DataDirectory { get; }

    public bool IsPortable { get; }

    public string ProgramDirectory { get; }

    public string Settings => Path.Combine(DataDirectory, "settings.json");

    public string Entries => Path.Combine(DataDirectory, "entries.json");

    public string CustomEntries => Path.Combine(DataDirectory, "custom.json");

    public string Recents => Path.Combine(DataDirectory, "recents.json");

    public string WindowPositions => Path.Combine(DataDirectory, "windows.json");

    public string UpdateCheck => Path.Combine(DataDirectory, "updates.json");

    public string Logs => Path.Combine(DataDirectory, "logs");

    /// <summary>The changelog, shipped next to coursee.exe.</summary>
    public string Changelog => Path.Combine(ProgramDirectory, "CHANGELOG.md");

    /// <summary>What Settings > About lists, in this order.</summary>
    public IReadOnlyList<AppFile> All =>
    [
        new("Settings", Settings),
        new("Pins, aliases, hidden", Entries),
        new("Custom entries", CustomEntries),
        new("Recents", Recents),
        new("Window positions", WindowPositions),
        new("Update check", UpdateCheck),
        new("Logs", Logs),
        new("Program", ProgramDirectory),
    ];

    /// <summary>
    /// The arguments for explorer.exe that show a file or folder in its folder, selected. One not
    /// made yet (no custom entry so far, say) opens the folder it will be in.
    /// </summary>
    public static string ShowInFolderArguments(string path, Func<string, bool>? exists = null)
    {
        exists ??= target => File.Exists(target) || Directory.Exists(target);
        var target = Path.TrimEndingDirectorySeparator(path);
        return exists(target)
            ? $"/select,\"{target}\""
            : $"\"{Path.GetDirectoryName(target)}\"";
    }
}
