using System.Security.Cryptography;

namespace Coursee.Core.Icons;

/// <summary>What a file the user picks as an icon is, by its extension.</summary>
public enum IconFileType
{
    /// <summary>An image read as it is: ico, png, jpg, bmp, gif.</summary>
    Image,

    /// <summary>A program or library (exe, dll): its icon is taken from it.</summary>
    Program,

    /// <summary>An SVG, used when it is made of plain paths (<see cref="SvgIcon"/>).</summary>
    Svg,
}

/// <summary>
/// The icon files the user chose, copied into coursee's icons folder so that they stay when the
/// original is moved or deleted, and travel with a portable copy. A file is named by its content
/// (two entries with the same image share it); one no entry uses is deleted.
/// </summary>
public sealed class IconFileStore(string folder)
{
    private const int HashLength = 16;

    public string Folder { get; } = folder;

    /// <summary>What can be read from <paramref name="path"/>; null for a file that cannot be an icon.</summary>
    public static IconFileType? TypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".ico" or ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => IconFileType.Image,
        ".exe" or ".dll" => IconFileType.Program,
        ".svg" => IconFileType.Svg,
        _ => null,
    };

    /// <summary>The path of a stored file.</summary>
    public string PathOf(string name) => Path.Combine(Folder, name);

    /// <summary>Stores an icon (written once per content) and returns its name in the folder.</summary>
    /// <param name="extension">How it is read back (<c>.png</c>, <c>.svg</c>...).</param>
    public string Add(byte[] content, string extension)
    {
        var name = Convert.ToHexStringLower(SHA256.HashData(content))[..HashLength] + extension.ToLowerInvariant();
        var path = PathOf(name);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(path, content);
        }

        return name;
    }

    /// <summary>Deletes the stored files not in <paramref name="used"/>. Returns how many were deleted.</summary>
    public int RemoveUnused(IEnumerable<string> used)
    {
        if (!Directory.Exists(Folder))
        {
            return 0;
        }

        var kept = new HashSet<string>(used, StringComparer.OrdinalIgnoreCase);
        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(Folder).Where(file => !kept.Contains(Path.GetFileName(file))))
        {
            File.Delete(file);
            removed++;
        }

        return removed;
    }
}
