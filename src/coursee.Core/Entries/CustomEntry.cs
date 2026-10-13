using Coursee.Core.Resources;

namespace Coursee.Core.Entries;

/// <summary>An entry the user made: a name of their own, and a target (URL, folder, file or command).</summary>
public sealed record CustomEntry(string Name, string Target)
{
    /// <summary>The <see cref="Resource.Source"/> of custom entries, and the name of their source.</summary>
    public const string SourceName = "Custom";

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
