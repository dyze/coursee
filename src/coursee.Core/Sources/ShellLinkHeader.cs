namespace Coursee.Core.Sources;

/// <summary>
/// Reads what a .lnk file says about its target, from the file alone: the attributes its target
/// had when the shortcut was saved ([MS-SHLLINK] ShellLinkHeader). Used for targets on the
/// network, which are never touched: an offline share can block for many seconds.
/// </summary>
public static class ShellLinkHeader
{
    private const int HeaderSize = 0x4C;
    private const int LinkClsidOffset = 0x04;
    private const int FileAttributesOffset = 0x18;
    private const int FileAttributeDirectory = 0x10;
    private static readonly Guid LinkClsid = new("00021401-0000-0000-c000-000000000046");

    /// <summary>True when the target was a folder, false when a file; null when this is no readable .lnk file.</summary>
    public static bool? TargetIsFolder(string shortcutPath)
    {
        var header = new byte[HeaderSize];
        try
        {
            using var file = File.OpenRead(shortcutPath);
            file.ReadExactly(header);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (BitConverter.ToInt32(header, 0) != HeaderSize
            || new Guid(header.AsSpan(LinkClsidOffset, 16)) != LinkClsid)
        {
            return null;
        }

        return (BitConverter.ToInt32(header, FileAttributesOffset) & FileAttributeDirectory) != 0;
    }
}
