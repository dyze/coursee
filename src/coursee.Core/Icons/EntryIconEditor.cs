using Coursee.Core.Entries;

namespace Coursee.Core.Icons;

/// <summary>
/// Gives entries the icon the user chose, or takes it back: saved in <see cref="EntryStore"/>, the
/// user's files kept in <see cref="IconFileStore"/> while an entry uses them.
/// </summary>
public sealed class EntryIconEditor(EntryStore entries, IconFileStore files)
{
    public IconFileStore Files { get; } = files;

    /// <summary>Gives the entry with this target <paramref name="icon"/>; null takes its chosen icon back.</summary>
    public void Set(string target, EntryIcon? icon)
    {
        entries.SetIcon(target, icon);
        Files.RemoveUnused(entries.Icons
            .Where(choice => choice.Icon.Kind == EntryIconKind.File)
            .Select(choice => choice.Icon.Name));
    }

    /// <summary>Gives the entry with this target an icon of the user's: <paramref name="content"/> is stored, then used.</summary>
    /// <param name="extension">How the content is read back (<c>.png</c>, <c>.svg</c>...).</param>
    public EntryIcon SetFile(string target, byte[] content, string extension)
    {
        var icon = EntryIcon.File(Files.Add(content, extension));
        Set(target, icon);
        return icon;
    }
}
