using Coursee.Core.Resources;

namespace Coursee.Core.Platform;

/// <summary>What the Windows auto start entry does, seen from this copy of coursee.</summary>
public enum AutoStartStatus
{
    /// <summary>No entry: nothing starts at sign-in.</summary>
    Off,

    /// <summary>The entry starts this copy, hidden.</summary>
    ThisCopy,

    /// <summary>The entry starts this copy, but in an older form (without <see cref="AutoStartCommand.HiddenArgument"/>).</summary>
    ThisCopyOutdated,

    /// <summary>The entry starts another copy (for example the installed one, while running a build).</summary>
    OtherCopy,
}

/// <summary>
/// The command line registered to start coursee at sign-in, and the argument that starts it
/// hidden. Kept free of any Windows API so the rules can be unit-tested on any OS.
/// </summary>
public static class AutoStartCommand
{
    /// <summary>
    /// Starts coursee in the notification area only. Without it (started by the user or the
    /// installer), the launcher is shown at once.
    /// </summary>
    public const string HiddenArgument = "--hidden";

    /// <summary>The quoted executable path (paths may contain spaces), then <see cref="HiddenArgument"/>.</summary>
    public static string Build(string executablePath) => $"\"{executablePath}\" {HiddenArgument}";

    /// <summary>True when the command-line arguments ask to start hidden.</summary>
    public static bool StartsHidden(IEnumerable<string> arguments) =>
        arguments.Any(argument => string.Equals(argument.Trim(), HiddenArgument, StringComparison.OrdinalIgnoreCase));

    /// <summary>Compares the stored entry with this copy's executable, ignoring case and surrounding spaces.</summary>
    public static AutoStartStatus Status(string? storedCommand, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(storedCommand))
        {
            return AutoStartStatus.Off;
        }

        if (!string.Equals(ProgramOf(storedCommand), executablePath, StringComparison.OrdinalIgnoreCase))
        {
            return AutoStartStatus.OtherCopy;
        }

        return string.Equals(storedCommand.Trim(), Build(executablePath), StringComparison.OrdinalIgnoreCase)
            ? AutoStartStatus.ThisCopy
            : AutoStartStatus.ThisCopyOutdated;
    }

    /// <summary>The program the stored entry starts (without its arguments), or null when there is none.</summary>
    public static string? ProgramOf(string? storedCommand) =>
        string.IsNullOrWhiteSpace(storedCommand) ? null : CommandLine.Split(storedCommand).FileName;
}
