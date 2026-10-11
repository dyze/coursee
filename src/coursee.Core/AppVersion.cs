using System.Reflection;

namespace Coursee.Core;

/// <summary>
/// Reads the product version stamped into an assembly by the build
/// (the single <c>Version</c> property in <c>Directory.Build.props</c>).
/// </summary>
public static class AppVersion
{
    /// <summary>Version of the given assembly, as shown to the user (for example "0.1").</summary>
    public static string Of(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return Format(informational ?? assembly.GetName().Version?.ToString());
    }

    /// <summary>Drops the build metadata the SDK appends after '+' (the source revision).</summary>
    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "unknown";
        }

        var plus = raw.IndexOf('+');
        return plus >= 0 ? raw[..plus] : raw;
    }
}
