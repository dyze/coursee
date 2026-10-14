namespace Coursee.Core.Sources;

/// <summary>A folder pinned in Quick access.</summary>
/// <param name="Path">Its full path.</param>
/// <param name="Name">The name Explorer shows (translated: <c>Bureau</c> for the Desktop in French).</param>
public sealed record PinnedFolder(string Path, string Name);

/// <summary>Reads the folders pinned in Explorer's Quick access (not the frequent ones).</summary>
public interface IQuickAccessReader
{
    /// <summary>In Explorer's order. Throws when Quick access cannot be read.</summary>
    IReadOnlyList<PinnedFolder> PinnedFolders();
}
