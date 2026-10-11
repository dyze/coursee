namespace Coursee.Core;

public enum ResourceKind
{
    File,
    Folder,
    Url,
}

/// <summary>Something the launcher can open: a file, a folder or a URL.</summary>
/// <param name="Name">Display name, matched first when searching.</param>
/// <param name="Target">Path or URL passed to the shell when opened.</param>
/// <param name="Kind">What the target is.</param>
/// <param name="Source">Name of the source that produced it.</param>
public sealed record Resource(string Name, string Target, ResourceKind Kind, string Source);
