using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>An item of the Apps folder, the list Windows Start shows.</summary>
/// <param name="Name">The name Start shows.</param>
/// <param name="AppUserModelId">Its app ID (<c>Microsoft.WindowsCalculator_8wekyb3d8bbwe!App</c>).</param>
/// <param name="PackageFamilyName">Its package, for a packaged (Store / MSIX) app; null or empty for a desktop program.</param>
public sealed record AppsFolderItem(string Name, string AppUserModelId, string? PackageFamilyName);

/// <summary>Reads the Apps folder (<c>shell:AppsFolder</c>).</summary>
public interface IAppsFolderReader
{
    /// <summary>Every app Start lists, packaged or not. Throws when the folder cannot be read.</summary>
    IReadOnlyList<AppsFolderItem> Items();
}

/// <summary>
/// The installed packaged apps (Store / MSIX: Windows Terminal, Calculator, Game Pass games...),
/// which have no shortcut in the Start Menu folders. The desktop programs of the Apps folder are
/// left out: the Start Menu source has them.
/// </summary>
public sealed class PackagedAppsSource(IAppsFolderReader reader) : IResourceSource
{
    public const string SourceName = "Packaged apps";

    private const string AppsFolderPrefix = @"shell:AppsFolder\";

    public string Name => SourceName;

    public string DisplayName => "Packaged apps (Store, MSIX)";

    public bool CanBeDisabled => true;

    public string Icon => SourceIcons.PackagedApps;

    public IReadOnlyList<Resource> GetResources() =>
        reader.Items()
            .Where(item => !string.IsNullOrWhiteSpace(item.PackageFamilyName))
            .DistinctBy(item => item.AppUserModelId, StringComparer.OrdinalIgnoreCase)
            .Select(item => new Resource(item.Name, AppsFolderPrefix + item.AppUserModelId, ResourceKind.App, Name))
            .ToList();
}
