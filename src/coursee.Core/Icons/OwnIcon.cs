using Coursee.Core.Resources;
using Coursee.Core.Sources;

namespace Coursee.Core.Icons;

/// <summary>Where an entry's own icon is read from, as Explorer shows it, when the user chose none.</summary>
public static class OwnIcon
{
    /// <summary>
    /// The file, folder or Shell item whose icon the entry shows; null when it has none of its own
    /// (a URL, a command, a setting): its row shows its source's icon. A network path is never read
    /// (an offline share blocks for seconds).
    /// </summary>
    /// <param name="isNetwork">Tells a network path; <see cref="FolderSources.IsNetwork"/> by default (tests replace it).</param>
    public static string? PathOf(Resource resource, Func<string, bool>? isNetwork = null)
    {
        var path = resource.IconPath ?? resource.Kind switch
        {
            ResourceKind.File or ResourceKind.Folder => resource.OpenTarget,

            // shell:AppsFolder\<app ID>: the Shell knows its icon.
            ResourceKind.App => resource.Target,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return (isNetwork ?? FolderSources.IsNetwork)(path) ? null : path;
    }
}
