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

    /// <summary>
    /// A Windows settings page or tool (<c>ms-settings:</c> URI, <c>.msc</c> console, program
    /// with its arguments), opened through the shell like a command.
    /// </summary>
    Setting,
}

/// <summary>Something the launcher can open: a file, a folder, a URL, a command or a Windows setting.</summary>
/// <param name="Name">Display name, matched first when searching.</param>
/// <param name="Target">Path, URL or command line shown and searched (for a shortcut, what it points to).</param>
/// <param name="Kind">What the target is.</param>
/// <param name="Source">Name of the source that produced it.</param>
/// <param name="LaunchPath">
/// What is passed to the shell when it differs from <paramref name="Target"/>. A Desktop shortcut
/// is opened through its .lnk file so that its arguments and working folder are kept.
/// </param>
/// <param name="Browser">
/// For a URL from a browser's bookmarks, that browser (see <see cref="Browsers"/>): the URL opens
/// in it rather than in the default browser. Null for everything else.
/// </param>
public sealed record Resource(
    string Name,
    string Target,
    ResourceKind Kind,
    string Source,
    string? LaunchPath = null,
    string? Browser = null)
{
    /// <summary>What pressing Enter will do, for display (for example "Open folder").</summary>
    [JsonIgnore]
    public string ActionLabel => ActionLabels.For(Kind, Browser);

    /// <summary>What pressing Enter will do, for its icon.</summary>
    [JsonIgnore]
    public ResourceAction Action => ActionLabels.ActionFor(Kind, Browser);

    /// <summary>The path, URL or command line actually opened.</summary>
    [JsonIgnore]
    public string OpenTarget => string.IsNullOrWhiteSpace(LaunchPath) ? Target : LaunchPath;
}
