using Coursee.Core.Sources;
using Microsoft.Win32;

namespace Coursee.UI.Platform;

/// <summary>Reads the Windows Run dialog history from the per-user RunMRU registry key.</summary>
public sealed class RegistryRunHistoryReader : IRunHistoryReader
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";

    public IReadOnlyDictionary<string, string> Read()
    {
        var values = new Dictionary<string, string>();

        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (key is null)
        {
            return values;
        }

        foreach (var name in key.GetValueNames())
        {
            if (key.GetValue(name) is string text)
            {
                values[name] = text;
            }
        }

        return values;
    }
}
