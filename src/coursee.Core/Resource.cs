using System.Text.Json.Serialization;

namespace Coursee.Core;

public enum ResourceKind
{
    File,
    Folder,
    Url,

    /// <summary>A command line to run, like in the Windows Run dialog (Win+R).</summary>
    Command,
}

/// <summary>Something the launcher can open: a file, a folder, a URL or a command.</summary>
/// <param name="Name">Display name, matched first when searching.</param>
/// <param name="Target">Path, URL or command line passed to the shell when opened.</param>
/// <param name="Kind">What the target is.</param>
/// <param name="Source">Name of the source that produced it.</param>
public sealed record Resource(string Name, string Target, ResourceKind Kind, string Source)
{
    /// <summary>What pressing Enter will do, for display (for example "Open folder").</summary>
    [JsonIgnore]
    public string ActionLabel => ActionLabels.For(Kind);
}
