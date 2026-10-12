using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// A provider of resources. New sources (Firefox bookmarks, ...) implement this
/// interface and are registered at startup; the UI never changes.
/// </summary>
public interface IResourceSource
{
    string Name { get; }

    IReadOnlyList<Resource> GetResources();
}
