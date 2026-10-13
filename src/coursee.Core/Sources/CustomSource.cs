using Coursee.Core.Entries;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>The entries the user made (<see cref="CustomEntryStore"/>), each one as <see cref="CustomEntry.ToResource"/> gives it.</summary>
public sealed class CustomSource(CustomEntryStore store) : IResourceSource
{
    public string Name => CustomEntry.SourceName;

    public string DisplayName => "Custom entries";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.Custom;

    public IReadOnlyList<Resource> GetResources() => store.All.Select(entry => entry.ToResource()).ToList();
}
