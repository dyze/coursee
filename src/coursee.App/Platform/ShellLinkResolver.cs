using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Coursee.Core.Sources;

namespace Coursee.UI.Platform;

/// <summary>
/// Resolves .lnk files through the Windows shell's own ShellLink object. It runs on any thread
/// (its threading model is Both), so the sources read their shortcuts in parallel without COM
/// marshaling. The target is read from the file only: <c>IShellLink.Resolve</c> is never called,
/// as it may search the disk or wait on the network.
/// </summary>
public sealed class ShellLinkResolver : IShortcutResolver
{
    private const int MaxPath = 1024;
    private const uint ReadOnly = 0;

    public string? ResolveTarget(string shortcutPath)
    {
        var link = new ShellLink();
        try
        {
            ((IPersistFile)link).Load(shortcutPath, (int)ReadOnly);

            var target = new StringBuilder(MaxPath);
            ((IShellLinkW)link).GetPath(target, target.Capacity, IntPtr.Zero, 0);
            return target.Length == 0 ? null : target.ToString();
        }
        catch (Exception ex) when (ex is COMException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink;

    /// <summary>Only the first method is declared: the rest of the vtable is never called.</summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file,
            int maxChars,
            IntPtr findData,
            uint flags);
    }
}
