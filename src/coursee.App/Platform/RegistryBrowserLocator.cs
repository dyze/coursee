using System.Security;
using Coursee.Core.Platform;
using Microsoft.Win32;

namespace Coursee.UI.Platform;

/// <summary>
/// Finds an installed program through Windows' App Paths registry keys (per user first, then for
/// the machine), where browsers register their executable.
/// </summary>
public sealed class RegistryBrowserLocator : IBrowserLocator
{
    private const string AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\";

    public string? FindExecutable(string executableName)
    {
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using var key = root.OpenSubKey(AppPathsKey + executableName);
                if (key?.GetValue(null) is string value)
                {
                    var path = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            {
                // Unreadable key: try the next one.
            }
        }

        return null;
    }
}
