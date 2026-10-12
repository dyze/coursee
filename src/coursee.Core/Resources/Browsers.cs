namespace Coursee.Core.Resources;

/// <summary>The browsers a bookmark can come from, as stored in <see cref="Resource.Browser"/>.</summary>
public static class Browsers
{
    public const string Firefox = "Firefox";
    public const string Edge = "Edge";

    /// <summary>The file name of the browser's program (as registered in Windows' App Paths), or null for an unknown browser.</summary>
    public static string? ExecutableName(string? browser) => browser?.ToUpperInvariant() switch
    {
        "FIREFOX" => "firefox.exe",
        "EDGE" => "msedge.exe",
        _ => null,
    };
}
