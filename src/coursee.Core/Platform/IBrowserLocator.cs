namespace Coursee.Core.Platform;

/// <summary>Finds an installed program, such as a browser, by the file name of its executable.</summary>
public interface IBrowserLocator
{
    /// <summary>The full path of <paramref name="executableName"/> (for example "firefox.exe"), or null when it is not installed.</summary>
    string? FindExecutable(string executableName);
}
