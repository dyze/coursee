using Coursee.Core.Platform;
using Microsoft.Win32;

namespace Coursee.UI.Platform;

/// <summary>
/// The <i>Add to coursee</i> item of Explorer's right-click menu, as a classic shell verb in the
/// per-user classes (HKCU): no administrator rights. On Windows 11 it is under <i>Show more options</i>.
/// The installer writes the same keys (<c>coursee.iss</c>).
/// </summary>
public sealed class RegistryExplorerMenu(string executablePath) : IExplorerMenu
{
    // Every file, and every folder.
    private static readonly string[] VerbKeys =
    [
        @"Software\Classes\*\shell\coursee",
        @"Software\Classes\Directory\shell\coursee",
    ];

    public ExplorerMenuStatus Status => ExplorerMenuCommand.Status(ReadCommand(), executablePath);

    public string? RegisteredProgram => ExplorerMenuCommand.ProgramOf(ReadCommand());

    public void SetEnabled(bool enabled)
    {
        foreach (var verbKey in VerbKeys)
        {
            if (enabled)
            {
                Write(verbKey);
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(verbKey, throwOnMissingSubKey: false);
            }
        }
    }

    private void Write(string verbKey)
    {
        using var verb = Registry.CurrentUser.CreateSubKey(verbKey);
        verb.SetValue(null, ExplorerMenuCommand.Label);
        verb.SetValue("Icon", $"\"{executablePath}\",0");

        // One item at a time: each becomes a custom entry named in the launcher.
        verb.SetValue("MultiSelectModel", "Single");

        using var command = verb.CreateSubKey("command");
        command.SetValue(null, ExplorerMenuCommand.Build(executablePath));
    }

    private static string? ReadCommand()
    {
        using var command = Registry.CurrentUser.OpenSubKey(VerbKeys[0] + @"\command");
        return command?.GetValue(null) as string;
    }
}
