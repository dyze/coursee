using Coursee.Core.Resources;

namespace Coursee.Core.Platform;

/// <summary>What Explorer's right-click item starts, seen from this copy of coursee.</summary>
public enum ExplorerMenuStatus
{
    /// <summary>No item in Explorer's menu.</summary>
    Off,

    /// <summary>The item starts this copy.</summary>
    ThisCopy,

    /// <summary>The item starts another copy (for example the installed one, while running a build).</summary>
    OtherCopy,
}

/// <summary>
/// The command line of Explorer's <i>Add to coursee</i> item, and how coursee reads it back.
/// Kept free of any Windows API so the rules can be unit-tested on any OS.
/// </summary>
public static class ExplorerMenuCommand
{
    /// <summary>Followed by the path of the file or folder to make a custom entry of.</summary>
    public const string AddArgument = "--add";

    /// <summary>The item's text in Explorer's menu.</summary>
    public const string Label = "Add to coursee";

    /// <summary>The quoted executable path, <see cref="AddArgument"/>, then Explorer's quoted placeholder for the item.</summary>
    public static string Build(string executablePath) => $"\"{executablePath}\" {AddArgument} \"%1\"";

    /// <summary>The path following <see cref="AddArgument"/>, trimmed; null when there is none.</summary>
    public static string? PathToAdd(IReadOnlyList<string> arguments)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index].Trim(), AddArgument, StringComparison.OrdinalIgnoreCase))
            {
                var path = arguments[index + 1].Trim();
                return path.Length == 0 ? null : path;
            }
        }

        return null;
    }

    /// <summary>Compares the stored command's program with this copy's executable, ignoring case.</summary>
    public static ExplorerMenuStatus Status(string? storedCommand, string executablePath) =>
        ProgramOf(storedCommand) switch
        {
            null => ExplorerMenuStatus.Off,
            var program when string.Equals(program, executablePath, StringComparison.OrdinalIgnoreCase) => ExplorerMenuStatus.ThisCopy,
            _ => ExplorerMenuStatus.OtherCopy,
        };

    /// <summary>The program the stored command starts (without its arguments), or null when there is none.</summary>
    public static string? ProgramOf(string? storedCommand) =>
        string.IsNullOrWhiteSpace(storedCommand) ? null : CommandLine.Split(storedCommand).FileName;
}
