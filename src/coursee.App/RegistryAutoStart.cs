using Coursee.Core;
using Microsoft.Win32;

namespace Coursee.UI;

/// <summary>
/// Auto start through the per-user Run key (HKCU). It needs no administrator rights
/// and can be reviewed or disabled from Windows Settings > Apps > Startup.
/// </summary>
public sealed class RegistryAutoStart : IAutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "coursee";

    private readonly string _executablePath;

    public RegistryAutoStart(string executablePath)
    {
        _executablePath = executablePath;
    }

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return AutoStartCommand.IsCurrent(key?.GetValue(ValueName) as string, _executablePath);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(ValueName, AutoStartCommand.Build(_executablePath));
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
