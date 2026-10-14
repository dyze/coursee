namespace Coursee.Core.Resources;

/// <summary>
/// Tells whether a program name, as typed in a command line, is an existing file, like the
/// Windows Run dialog: in the working folder, then in the folders of PATH, trying the extensions
/// of PATHEXT when the name has none.
/// </summary>
public sealed class ProgramFinder
{
    private const string DefaultExtensions = ".COM;.EXE;.BAT;.CMD";

    private readonly string _workingDirectory;
    private readonly string[] _searchFolders;
    private readonly string[] _extensions;
    private readonly Func<string, bool> _fileExists;

    /// <param name="workingDirectory">Where a command starts (Win+R: the user's folder); searched first.</param>
    /// <param name="pathVariable">The PATH variable: folders separated by ';'.</param>
    /// <param name="pathExtVariable">The PATHEXT variable: extensions separated by ';'. Null means the usual ones.</param>
    /// <param name="fileExists">Replaced in tests; by default the real file system is used.</param>
    public ProgramFinder(
        string workingDirectory,
        string? pathVariable,
        string? pathExtVariable,
        Func<string, bool>? fileExists = null)
    {
        _workingDirectory = workingDirectory;
        _searchFolders = SplitList(pathVariable);
        _extensions = SplitList(pathExtVariable ?? DefaultExtensions);
        _fileExists = fileExists ?? File.Exists;
    }

    public bool Exists(string program) => Find(program) is not null;

    /// <summary>The path of the file the program name stands for, or null when there is none.</summary>
    public string? Find(string program)
    {
        string[] names = Path.HasExtension(program)
            ? [program]
            : _extensions.Select(extension => program + extension).ToArray();

        return Folders(program)
            .SelectMany(folder => names.Select(name => Path.Combine(folder, name)))
            .FirstOrDefault(_fileExists);
    }

    /// <summary>
    /// A full path is only itself; a relative one (with a folder) is looked for from the working
    /// folder only; a bare name also in the folders of PATH.
    /// </summary>
    private IEnumerable<string> Folders(string program)
    {
        if (Path.IsPathRooted(program))
        {
            return [string.Empty];
        }

        var hasFolder = program.Contains('\\') || program.Contains('/');
        return hasFolder ? [_workingDirectory] : [_workingDirectory, .. _searchFolders];
    }

    private static string[] SplitList(string? list) =>
        (list ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
