namespace Coursee.Core.Platform;

/// <summary>
/// Which build a running coursee is, and where it runs from. The first copy publishes it, so a later
/// copy can tell whether the one running is the same build.
/// </summary>
/// <param name="Build">The full informational version: "0.8.1+&lt;commit&gt;[.dirty]" (see <see cref="AppVersion"/>).</param>
/// <param name="ExecutablePath">The running coursee.exe.</param>
public sealed record InstanceIdentity(string Build, string ExecutablePath)
{
    private const char Separator = '\n';

    /// <summary>Same build: same version, commit and dirty mark. The path does not matter.</summary>
    public bool IsSameBuildAs(InstanceIdentity other) => string.Equals(Build, other.Build, StringComparison.Ordinal);

    /// <summary>"coursee 0.8.1 (97bddb2), from C:\...\coursee.exe".</summary>
    public string Describe() => $"coursee {AppVersion.FormatWithCommit(Build)}, from {ExecutablePath}";

    /// <summary>The text published by the running copy (a path cannot hold a line break).</summary>
    public string Encode() => $"{Build}{Separator}{ExecutablePath}";

    /// <summary>The identity read from <see cref="Encode"/>'s text; null when the text is not one.</summary>
    public static InstanceIdentity? Decode(string? text)
    {
        var parts = text?.Split(Separator);
        return parts is [var build, var path] && build.Length > 0 && path.Length > 0
            ? new InstanceIdentity(build, path)
            : null;
    }
}
