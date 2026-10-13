using System.ComponentModel;
using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>A row of the launcher's list: the resource, and what the user set on its entry.</summary>
public sealed class ResultRow(Resource resource, bool isPinned, string? alias) : INotifyPropertyChanged
{
    private static readonly string PinnedGlyph = ((char)0xE841).ToString();
    private static readonly string UnpinnedGlyph = ((char)0xE718).ToString();

    private bool _isEditingAlias;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Resource Resource { get; } = resource;

    public bool IsPinned { get; } = isPinned;

    public string? Alias { get; } = alias;

    /// <summary>Typed text has no entry of its own: no pin icon.</summary>
    public bool CanPin => ResourceActions.CanPin(Resource);

    /// <summary>Typed text has no entry of its own: no alias icon.</summary>
    public bool CanSetAlias => ResourceActions.CanSetAlias(Resource);

    /// <summary>Segoe Fluent Icons: a filled pin when pinned.</summary>
    public string PinGlyph => IsPinned ? PinnedGlyph : UnpinnedGlyph;

    public string PinToolTip => IsPinned ? "Unpin (Ctrl+P)" : "Pin: listed first when it matches (Ctrl+P)";

    public string AliasToolTip => Alias is null
        ? "Set an alias: a short name that lists this first (F2)"
        : $"Alias: {Alias} (F2 to change)";

    /// <summary>True while the alias box of this row is open (F2).</summary>
    public bool IsEditingAlias
    {
        get => _isEditingAlias;
        set
        {
            if (_isEditingAlias != value)
            {
                _isEditingAlias = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditingAlias)));
            }
        }
    }
}
