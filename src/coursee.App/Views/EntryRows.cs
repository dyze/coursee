using Coursee.Core.Entries;

namespace Coursee.UI.Views;

/// <summary>A hidden entry in Settings > Entries.</summary>
public sealed class HiddenEntryRow(HiddenEntry entry, string sourceLabel)
{
    public HiddenEntry Entry { get; } = entry;

    public string Name => Entry.Name;

    public string SourceLabel { get; } = sourceLabel;

    /// <summary>The source first: a long target is cut at the end.</summary>
    public string Detail => $"{SourceLabel}  ·  {Entry.Target}";

    public string ToolTip => $"{Entry.Name}\n{Entry.Target}\n{SourceLabel}";
}

/// <summary>A custom entry in Settings > Entries: its boxes edit <see cref="Name"/> and <see cref="Target"/> until saved.</summary>
public sealed class CustomEntryRow(CustomEntry entry)
{
    public CustomEntry Entry { get; } = entry;

    public string Name { get; set; } = entry.Name;

    public string Target { get; set; } = entry.Target;
}

/// <summary>A chosen icon in Settings > Entries.</summary>
/// <param name="Visual">The icon, drawn; null when it cannot be (its file gone).</param>
public sealed record IconRow(EntryIconChoice Choice, IconVisual? Visual)
{
    public string Target => Choice.Target;

    /// <summary>One of coursee's icons by its name; a file of the user's as such.</summary>
    public string Detail => Choice.Icon.Kind == Core.Icons.EntryIconKind.Library ? Choice.Icon.Name : "Your own image";
}

/// <summary>An alias in Settings > Entries: its box edits <see cref="Alias"/> until saved.</summary>
public sealed class AliasRow(EntryAlias entry)
{
    public string Alias { get; set; } = entry.Alias;

    public string Target { get; } = entry.Target;
}
