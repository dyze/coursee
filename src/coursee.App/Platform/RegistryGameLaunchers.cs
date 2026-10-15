using Coursee.Core.Games;
using Microsoft.Win32;

namespace Coursee.UI.Platform;

/// <summary>Where the game launchers register themselves: Steam's folder, and the games GOG installed.</summary>
public sealed class RegistryGameLaunchers : IGogGamesReader
{
    private const string SteamKey = @"Software\Valve\Steam";
    private const string GogGamesKey = @"SOFTWARE\GOG.com\Games";

    /// <summary>Steam's folder (it writes it with forward slashes), or null when Steam is not installed.</summary>
    public static string? SteamFolder()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SteamKey);
        return key?.GetValue("SteamPath") is string path && !string.IsNullOrWhiteSpace(path)
            ? Path.GetFullPath(path)
            : null;
    }

    /// <summary>GOG writes its 32-bit registry view (<c>WOW6432Node</c> for a 64-bit process); a subkey per game.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Games()
    {
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using var games = machine.OpenSubKey(GogGamesKey);
        if (games is null)
        {
            return [];
        }

        var list = new List<IReadOnlyDictionary<string, string>>();
        foreach (var id in games.GetSubKeyNames())
        {
            using var game = games.OpenSubKey(id);
            if (game is null)
            {
                continue;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in game.GetValueNames())
            {
                if (game.GetValue(name) is string text)
                {
                    values[name] = text;
                }
            }

            list.Add(values);
        }

        return list;
    }
}
