using System.Diagnostics;
using Coursee.Core.Resources;

namespace Coursee.UI.Platform;

/// <summary>How a command line is started, like the Windows Run dialog (Win+R).</summary>
public static class CommandStart
{
    /// <summary>
    /// Starts in the user's folder, as Win+R does, and finds the program there or in PATH, so
    /// that a file whose name has spaces runs without quotes (<c>kill rds.cmd</c>).
    /// </summary>
    public static ProcessStartInfo For(string commandLine)
    {
        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var finder = new ProgramFinder(
            userFolder,
            Environment.GetEnvironmentVariable("PATH"),
            Environment.GetEnvironmentVariable("PATHEXT"));

        var (program, arguments) = CommandLine.Split(commandLine, finder.Exists);
        return new ProcessStartInfo(finder.Find(program) ?? program, arguments) { WorkingDirectory = userFolder };
    }
}
