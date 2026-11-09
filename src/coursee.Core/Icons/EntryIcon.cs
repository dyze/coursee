namespace Coursee.Core.Icons;

/// <summary>Where an icon the user chose for an entry comes from.</summary>
public enum EntryIconKind
{
    /// <summary>One of coursee's icons (<see cref="IconLibrary"/>), by name.</summary>
    Library,

    /// <summary>A file of the user's, copied into coursee's icons folder (<see cref="IconFileStore"/>), by its name there.</summary>
    File,
}

/// <summary>The icon the user chose for an entry. Stored in <c>entries.json</c> as <c>library:game</c> or <c>file:3f2a….png</c>.</summary>
public sealed record EntryIcon(EntryIconKind Kind, string Name)
{
    private const string LibraryPrefix = "library:";
    private const string FilePrefix = "file:";

    public static EntryIcon Library(string name) => new(EntryIconKind.Library, name);

    public static EntryIcon File(string name) => new(EntryIconKind.File, name);

    /// <summary>The icon of a stored text; null when it is not one (a file edited by hand).</summary>
    public static EntryIcon? Parse(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var (kind, prefix) = text.StartsWith(LibraryPrefix, StringComparison.OrdinalIgnoreCase)
            ? (EntryIconKind.Library, LibraryPrefix)
            : (EntryIconKind.File, FilePrefix);
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var name = text[prefix.Length..].Trim();
        // A file name in the icons folder, never a path out of it.
        return name is "" or "." or ".." || name.IndexOfAny(['/', '\\', ':']) >= 0 ? null : new EntryIcon(kind, name);
    }

    /// <summary>The text stored for this icon.</summary>
    public override string ToString() => (Kind == EntryIconKind.Library ? LibraryPrefix : FilePrefix) + Name;
}
