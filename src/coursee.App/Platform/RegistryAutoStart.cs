using Coursee.Core.Platform;
using Microsoft.Win32;

namespace Coursee.UI.Platform;

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

    public AutoStartStatus Status => AutoStartCommand.Status(ReadCommand(), _executablePath);

    public string? RegisteredProgram => AutoStartCommand.ProgramOf(ReadCommand());

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

    private static string? ReadCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) as string;
    }
}
