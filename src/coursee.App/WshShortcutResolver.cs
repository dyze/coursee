using System.Runtime.InteropServices;
using Coursee.Core;

namespace Coursee.UI;

/// <summary>Resolves .lnk files through the Windows Script Host COM object.</summary>
public sealed class WshShortcutResolver : IShortcutResolver
{
    public string? ResolveTarget(string shortcutPath)
    {
        var type = Type.GetTypeFromProgID("WScript.Shell");
        if (type is null)
        {
            return null;
        }

        object? shell = null;
        object? link = null;
        try
        {
            shell = Activator.CreateInstance(type);
            if (shell is null)
            {
                return null;
            }

            link = ((dynamic)shell).CreateShortcut(shortcutPath);
            string? target = ((dynamic)link).TargetPath;
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            return null;
        }
        finally
        {
            if (link is not null)
            {
                Marshal.FinalReleaseComObject(link);
            }

            if (shell is not null)
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
