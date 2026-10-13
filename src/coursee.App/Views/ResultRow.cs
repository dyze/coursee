using System.ComponentModel;
using Coursee.Core.Resources;

namespace Coursee.UI.Views;

/// <summary>What the text box of a row is open for.</summary>
public enum RowEdit
{
    None,

    /// <summary>F2: the alias of the entry.</summary>
    Alias,

    /// <summary>Ctrl+N or the + button, on typed text: the name of the custom entry it becomes.</summary>
    CustomEntryName,
}

/// <summary>A row of the launcher's list: the resource, and what the user set on its entry.</summary>
public sealed class ResultRow(Resource resource, bool isPinned, string? alias) : INotifyPropertyChanged
{
    private static readonly string PinnedGlyph = ((char)0xE840).ToString();
    private static readonly string UnpinnedGlyph = ((char)0xE718).ToString();

    private RowEdit _edit;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Resource Resource { get; } = resource;

    public bool IsPinned { get; } = isPinned;

    public string? Alias { get; } = alias;

    /// <summary>Typed text has no entry of its own: no pin icon.</summary>
    public bool CanPin => ResourceActions.CanPin(Resource);

    /// <summary>Typed text has no entry of its own: no alias icon.</summary>
    public bool CanSetAlias => ResourceActions.CanSetAlias(Resource);

    /// <summary>Typed text has no entry of its own: no hide icon.</summary>
    public bool CanHide => ResourceActions.CanHide(Resource);

    /// <summary>
    /// Segoe Fluent Icons: a tilted pin when pinned, an upright one otherwise. When pinned, the window
    /// also fills it, with the PinnedFill glyph (E841) drawn over this one: alone it is incomplete.
    /// </summary>
    public string PinGlyph => IsPinned ? PinnedGlyph : UnpinnedGlyph;

    public string PinToolTip => IsPinned ? "Unpin (Ctrl+P)" : "Pin: listed first when it matches (Ctrl+P)";

    public string AliasToolTip => Alias is null
        ? "Set an alias: a short name that lists this first (F2)"
        : $"Alias: {Alias} (F2 to change)";

    /// <summary>What the text box of this row is open for, if anything.</summary>
    public RowEdit Edit
    {
        get => _edit;
        set
        {
            if (_edit != value)
            {
                _edit = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Edit)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditing)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditToolTip)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditMaxLength)));
            }
        }
    }

    /// <summary>True while the text box of this row is open.</summary>
    public bool IsEditing => Edit != RowEdit.None;

    public string EditToolTip => Edit == RowEdit.CustomEntryName
        ? "Name of the new custom entry: Enter saves, Esc cancels"
        : "Alias: Enter saves, Esc cancels, empty removes it";

    public int EditMaxLength => Edit == RowEdit.CustomEntryName ? 100 : 40;
}
