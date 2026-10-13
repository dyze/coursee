using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>A row of the launcher's list: the resource, and what the user set on its entry.</summary>
public sealed class ResultRow(Resource resource, bool isPinned)
{
    private static readonly string PinnedGlyph = ((char)0xE841).ToString();
    private static readonly string UnpinnedGlyph = ((char)0xE718).ToString();

    public Resource Resource { get; } = resource;

    public bool IsPinned { get; } = isPinned;

    /// <summary>Typed text has no entry of its own: no pin icon.</summary>
    public bool CanPin => ResourceActions.CanPin(Resource);

    /// <summary>Segoe Fluent Icons: a filled pin when pinned.</summary>
    public string PinGlyph => IsPinned ? PinnedGlyph : UnpinnedGlyph;

    public string PinToolTip => IsPinned ? "Unpin (Ctrl+P)" : "Pin: listed first when it matches (Ctrl+P)";
}
