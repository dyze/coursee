namespace Coursee.Core;

/// <summary>A profile path as written in profiles.ini.</summary>
/// <param name="RawPath">The path value, possibly relative to the Firefox root folder.</param>
/// <param name="IsRelative">True when <paramref name="RawPath"/> is relative to the Firefox root folder.</param>
public sealed record ProfileLocation(string RawPath, bool IsRelative);

/// <summary>
/// Finds the default Firefox profile from <c>profiles.ini</c>
/// (on Windows, <c>%APPDATA%\Mozilla\Firefox</c>).
/// </summary>
public static class FirefoxProfileLocator
{
    /// <summary>Returns the default profile folder, or null when none can be determined.</summary>
    public static string? FindDefaultProfile(string firefoxRoot)
    {
        var iniPath = Path.Combine(firefoxRoot, "profiles.ini");
        if (!File.Exists(iniPath))
        {
            return null;
        }

        string text;
        try
        {
            text = File.ReadAllText(iniPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var location = ParseDefault(text);
        if (location is null)
        {
            return null;
        }

        var directory = location.IsRelative
            ? Path.Combine(firefoxRoot, location.RawPath)
            : location.RawPath;

        return Directory.Exists(directory) ? directory : null;
    }

    /// <summary>
    /// Picks the default profile from the text of profiles.ini. Order of preference:
    /// the installation's Default entry, then a profile marked Default=1, then the first profile.
    /// </summary>
    public static ProfileLocation? ParseDefault(string iniText)
    {
        var sections = ParseSections(iniText);
        var profiles = sections
            .Where(s => s.Name.StartsWith("Profile", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var install = sections.FirstOrDefault(s =>
            s.Name.StartsWith("Install", StringComparison.OrdinalIgnoreCase)
            && s.Values.ContainsKey("Default"));

        if (install is not null)
        {
            var path = install.Values["Default"];
            var owner = profiles.FirstOrDefault(s => s.Values.GetValueOrDefault("Path") == path);
            return new ProfileLocation(path, owner is null || IsRelative(owner));
        }

        var chosen = profiles.FirstOrDefault(s => s.Values.GetValueOrDefault("Default") == "1" && s.Values.ContainsKey("Path"))
            ?? profiles.FirstOrDefault(s => s.Values.ContainsKey("Path"));

        return chosen is null ? null : new ProfileLocation(chosen.Values["Path"], IsRelative(chosen));
    }

    private static bool IsRelative(IniSection section) =>
        section.Values.GetValueOrDefault("IsRelative", "1") == "1";

    private sealed record IniSection(string Name, Dictionary<string, string> Values);

    private static List<IniSection> ParseSections(string text)
    {
        var sections = new List<IniSection>();
        IniSection? current = null;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is ';' or '#')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                current = new IniSection(line[1..^1], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                sections.Add(current);
                continue;
            }

            var separator = line.IndexOf('=');
            if (current is not null && separator > 0)
            {
                current.Values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
        }

        return sections;
    }
}
