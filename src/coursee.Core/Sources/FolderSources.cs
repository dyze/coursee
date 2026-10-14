namespace Coursee.Core.Sources;

/// <summary>
/// Rules of the folder sources: a folder the user added, or one pinned in Explorer's Quick access.
/// A folder source is named after its path (<c>Folder:D:\code</c>), whichever way it came, so its
/// recents, hidden entries and on / off state survive a move from one to the other.
/// </summary>
public static class FolderSources
{
    private const string NamePrefix = "Folder:";

    /// <summary>The source name of a folder.</summary>
    public static string NameOf(string folder) => NamePrefix + Normalize(folder);

    /// <summary>True for the name of a folder source (the Desktop has its own name).</summary>
    public static bool IsFolderName(string sourceName) =>
        sourceName.StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>The folder of a folder source name, or null for another source.</summary>
    public static string? FolderOf(string sourceName) =>
        IsFolderName(sourceName) ? sourceName[NamePrefix.Length..] : null;

    /// <summary>The path as compared and stored: trimmed, without a trailing separator (<c>D:\</c> stays).</summary>
    public static string Normalize(string folder) => Path.TrimEndingDirectorySeparator(folder.Trim());

    public static bool AreSame(string folder, string other) =>
        string.Equals(Normalize(folder), Normalize(other), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A network share (<c>\\server\share</c>) or a mapped network drive. Never listed: an offline
    /// one can block for seconds. Asking the drive type does not touch the network.
    /// </summary>
    public static bool IsNetwork(string folder)
    {
        if (folder.StartsWith(@"\\", StringComparison.Ordinal) || folder.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        var root = Path.GetPathRoot(folder);
        try
        {
            return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
