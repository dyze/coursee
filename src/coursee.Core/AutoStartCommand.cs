namespace Coursee.Core;

/// <summary>
/// The command line registered to start coursee at sign-in. Kept free of any
/// Windows API so the rules can be unit-tested on any OS.
/// </summary>
public static class AutoStartCommand
{
    /// <summary>The executable path, quoted so that paths containing spaces work.</summary>
    public static string Build(string executablePath) => $"\"{executablePath}\"";

    /// <summary>
    /// True when the stored command starts this exact executable. A command pointing
    /// at another location (for example after the app was moved) does not count,
    /// so switching auto start on again rewrites it.
    /// </summary>
    public static bool IsCurrent(string? storedCommand, string executablePath) =>
        !string.IsNullOrWhiteSpace(storedCommand)
        && string.Equals(storedCommand.Trim(), Build(executablePath), StringComparison.OrdinalIgnoreCase);
}
