namespace Coursee.Core.Sources;

/// <summary>Reads the folders pinned in Explorer's Quick access (not the frequent ones).</summary>
public interface IQuickAccessReader
{
    /// <summary>Their paths, in Explorer's order. Throws when Quick access cannot be read.</summary>
    IReadOnlyList<string> PinnedFolders();
}
