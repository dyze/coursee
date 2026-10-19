namespace Coursee.Core.Platform;

/// <summary>What a starting copy does when another coursee already runs.</summary>
public enum InstanceConflictKind
{
    /// <summary>The same build: show the running one, as if the hotkey was pressed.</summary>
    SameBuild,

    /// <summary>Another build: offer to close it and start this one.</summary>
    OtherBuild,

    /// <summary>A copy that publishes no identity (0.8.1 and older): it cannot be asked to close.</summary>
    Unknown,
}

/// <summary>Compares a starting copy with the running one, and words what the user is told.</summary>
public static class InstanceConflict
{
    public const string UnknownMessage =
        "Another coursee is already running, of a version that cannot be closed from here.\n\n"
        + "Quit it from its icon in the notification area, then start this one again.";

    public const string NotClosedMessage =
        "The running coursee did not close.\n\n"
        + "Quit it from its icon in the notification area, then start this one again.";

    public static InstanceConflictKind Compare(InstanceIdentity? running, InstanceIdentity starting) =>
        running is null ? InstanceConflictKind.Unknown
        : running.IsSameBuildAs(starting) ? InstanceConflictKind.SameBuild
        : InstanceConflictKind.OtherBuild;

    public static string ReplaceQuestion(InstanceIdentity running, InstanceIdentity starting) =>
        $"{running.Describe()}, is already running.\n\n"
        + $"This copy is {starting.Describe()}.\n\n"
        + "Close the running one and start this one?";
}
