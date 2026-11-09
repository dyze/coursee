using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// A provider of resources. New sources (Firefox bookmarks, ...) implement this
/// interface and are registered at startup; the UI never changes.
/// </summary>
public interface IResourceSource
{
    /// <summary>Stable key: stored in settings.json and recents.json, used as the log category.</summary>
    string Name { get; }

    /// <summary>What Settings shows for the source (for example "Firefox bookmarks").</summary>
    string DisplayName { get; }

    /// <summary>True when Settings offers a checkbox to switch the source off.</summary>
    bool CanBeDisabled { get; }

    /// <summary>
    /// The icon of the source, shown on its rows and in Settings: the name of one of
    /// <see cref="Icons.IconLibrary"/>'s icons (for example "bookmark"). See <see cref="SourceIcons"/>.
    /// </summary>
    string Icon { get; }

    IReadOnlyList<Resource> GetResources();
}
