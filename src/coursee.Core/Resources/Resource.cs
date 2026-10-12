using System.Text.Json.Serialization;

namespace Coursee.Core.Resources;

/// <summary>What a resource is. Stored as a number in recents.json: add new kinds at the end only.</summary>
public enum ResourceKind
{
    File,
    Folder,
    Url,

    /// <summary>A command line to run, like in the Windows Run dialog (Win+R).</summary>
    Command,

    /// <summary>
    /// A network file or folder (such as <c>\\server\share</c>), not checked on disk because
    /// the check could block. The shell opens it either way.
    /// </summary>
    NetworkPath,
}

/// <summary>Something the launcher can open: a file, a folder, a URL or a command.</summary>
/// <param name="Name">Display name, matched first when searching.</param>
/// <param name="Target">Path, URL or command line shown and searched (for a shortcut, what it points to).</param>
/// <param name="Kind">What the target is.</param>
/// <param name="Source">Name of the source that produced it.</param>
/// <param name="LaunchPath">
/// What is passed to the shell when it differs from <paramref name="Target"/>. A Desktop shortcut
/// is opened through its .lnk file so that its arguments and working folder are kept.
/// </param>
public sealed record Resource(string Name, string Target, ResourceKind Kind, string Source, string? LaunchPath = null)
{
    /// <summary>What pressing Enter will do, for display (for example "Open folder").</summary>
    [JsonIgnore]
    public string ActionLabel => ActionLabels.For(Kind);

    /// <summary>The path, URL or command line actually opened.</summary>
    [JsonIgnore]
    public string OpenTarget => string.IsNullOrWhiteSpace(LaunchPath) ? Target : LaunchPath;
}
