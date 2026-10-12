using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// A fixed list of Windows settings pages and tools: <c>ms-settings:</c> pages, management
/// consoles, Control Panel items. coursee only opens them; it never changes a setting.
/// Search matches the name and the target only, so the names say what people look for
/// ("Installed apps (uninstall programs)").
/// </summary>
public sealed class WindowsSettingsSource : IResourceSource
{
    public const string SourceName = "Windows settings";

    private static readonly (string Name, string Target)[] Entries =
    [
        // Settings app
        ("Installed apps (uninstall programs)", "ms-settings:appsfeatures"),
        ("Default apps", "ms-settings:defaultapps"),
        ("Startup apps", "ms-settings:startupapps"),
        ("Network and internet", "ms-settings:network"),
        ("Wi-Fi", "ms-settings:network-wifi"),
        ("Advanced network settings", "ms-settings:network-advancedsettings"),
        ("Proxy", "ms-settings:network-proxy"),
        ("VPN", "ms-settings:network-vpn"),
        ("Bluetooth and devices", "ms-settings:bluetooth"),
        ("Printers and scanners", "ms-settings:printers"),
        ("Mouse and touchpad", "ms-settings:mousetouchpad"),
        ("Typing (keyboard)", "ms-settings:typing"),
        ("Display (resolution, scaling, monitors)", "ms-settings:display"),
        ("Sound", "ms-settings:sound"),
        ("Notifications", "ms-settings:notifications"),
        ("Power and battery (sleep)", "ms-settings:powersleep"),
        ("Storage (disk space)", "ms-settings:storagesense"),
        ("Personalization (background, colors)", "ms-settings:personalization"),
        ("Fonts", "ms-settings:fonts"),
        ("Date and time", "ms-settings:dateandtime"),
        ("Language and region", "ms-settings:regionlanguage"),
        ("Accounts", "ms-settings:yourinfo"),
        ("Privacy and security", "ms-settings:privacy"),
        ("Windows Security (antivirus)", "ms-settings:windowsdefender"),
        ("Windows Update", "ms-settings:windowsupdate"),
        ("About this PC (system info, PC name)", "ms-settings:about"),

        // Consoles and tools
        ("Services", "services.msc"),
        ("Device Manager", "devmgmt.msc"),
        ("Event Viewer", "eventvwr.msc"),
        ("Disk Management (partitions)", "diskmgmt.msc"),
        ("Computer Management", "compmgmt.msc"),
        ("Task Scheduler", "taskschd.msc"),
        ("Task Manager", "taskmgr.exe"),
        ("Registry Editor", "regedit.exe"),
        ("System Information", "msinfo32.exe"),
        ("Advanced system settings (performance, virtual memory)", "SystemPropertiesAdvanced.exe"),
        ("Environment variables", "rundll32.exe sysdm.cpl,EditEnvironmentVariables"),

        // Control Panel
        ("Control Panel", "control.exe"),
        ("Windows Tools (administrative tools)", "control.exe admintools"),
        ("Programs and Features (classic uninstall)", "control.exe appwiz.cpl"),
        ("Network Connections (adapters)", "control.exe ncpa.cpl"),
        ("Windows Defender Firewall", "control.exe firewall.cpl"),
        ("Internet Options", "control.exe inetcpl.cpl"),
        ("Power Options (classic)", "control.exe powercfg.cpl"),
        ("Sound devices (classic)", "control.exe mmsys.cpl"),
        ("Credential Manager (saved passwords)", "control.exe /name Microsoft.CredentialManager"),
    ];

    private readonly IReadOnlyList<Resource> _resources =
        Entries.Select(entry => new Resource(entry.Name, entry.Target, ResourceKind.Setting, SourceName)).ToList();

    public string Name => SourceName;

    public string DisplayName => "Windows settings and tools";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.WindowsSettings;

    public IReadOnlyList<Resource> GetResources() => _resources;
}
