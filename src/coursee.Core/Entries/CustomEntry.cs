using Coursee.Core.Resources;

namespace Coursee.Core.Entries;

/// <summary>An entry the user made: a name of their own, and a target (URL, folder, file or command).</summary>
public sealed record CustomEntry(string Name, string Target)
{
    /// <summary>The <see cref="Resource.Source"/> of custom entries, and the name of their source.</summary>
    public const string SourceName = "Custom";

    /// <summary>
    /// The name offered for an entry made from <paramref name="typed"/> (a path added from Explorer):
    /// a folder's name, a file's name without its extension, a network path's last part (file or
    /// folder is not checked there). Anything else, and a drive, keeps its name. Both separators
    /// are read, so the rule is the same on any OS.
    /// </summary>
    public static string NameFor(Resource typed)
    {
        if (typed.Kind is not (ResourceKind.File or ResourceKind.Folder or ResourceKind.NetworkPath))
        {
            return typed.Name;
        }

        var path = typed.Target.TrimEnd('\\', '/');
        var last = path[(path.LastIndexOfAny(['\\', '/']) + 1)..];
        var extension = typed.Kind == ResourceKind.File ? last.LastIndexOf('.') : -1;
        var name = extension > 0 ? last[..extension] : last;
        return name.Length == 0 || name.EndsWith(':') ? typed.Name : name;
    }

    /// <summary>
    /// The resource of this entry: what its target is (URL, folder, file, network path or command)
    /// is decided as for typed text. The existence checks can be replaced (tests); by default the
    /// file system is used, except for network paths, which are never checked.
    /// </summary>
    public Resource ToResource(Func<string, bool>? directoryExists = null, Func<string, bool>? fileExists = null) =>
        FallbackClassifier.Classify(Target, directoryExists, fileExists)! with
        {
            Name = Name,
            Source = SourceName,
        };
}
