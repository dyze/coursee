namespace Coursee.Core.Tests.Sources;

public class WindowsSettingsSourceTests
{
    private static IReadOnlyList<Resource> Resources() => new WindowsSettingsSource().GetResources();

    [Fact]
    public void Source_IsSwitchable_WithItsOwnIcon()
    {
        var source = new WindowsSettingsSource();

        Assert.Equal("Windows settings", source.Name);
        Assert.True(source.CanBeDisabled);
        Assert.Equal(SourceIcons.WindowsSettings, source.Icon);
    }

    [Fact]
    public void Entries_AreSettingsOfThisSource()
    {
        Assert.NotEmpty(Resources());
        Assert.All(Resources(), r =>
        {
            Assert.Equal(ResourceKind.Setting, r.Kind);
            Assert.Equal(WindowsSettingsSource.SourceName, r.Source);
            Assert.False(string.IsNullOrWhiteSpace(r.Name));
            Assert.False(string.IsNullOrWhiteSpace(r.Target));
        });
    }

    [Fact]
    public void Entries_HaveDistinctNamesAndTargets()
    {
        Assert.Equal(Resources().Count, Resources().Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(Resources().Count, Resources().Select(r => r.Target).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("Installed apps (uninstall programs)", "ms-settings:appsfeatures")]
    [InlineData("Advanced network settings", "ms-settings:network-advancedsettings")]
    [InlineData("Services", "services.msc")]
    [InlineData("Advanced system settings (performance, virtual memory)", "SystemPropertiesAdvanced.exe")]
    public void Entries_IncludeTheProposedOnes(string name, string target) =>
        Assert.Contains(Resources(), r => r.Name == name && r.Target == target);

    [Theory]
    [InlineData("uninstall", "ms-settings:appsfeatures")]
    [InlineData("services", "services.msc")]
    [InlineData("advanced network", "ms-settings:network-advancedsettings")]
    [InlineData("environment", "rundll32.exe sysdm.cpl,EditEnvironmentVariables")]
    public void Entries_AreFoundByWhatPeopleType(string query, string expectedTarget) =>
        Assert.Equal(expectedTarget, SearchEngine.Search(Resources(), query)[0].Target);

    [Fact]
    public void Entries_AreOpenedAsSettings()
    {
        var page = Resources().First(r => r.Target.StartsWith("ms-settings:", StringComparison.Ordinal));

        Assert.Equal("Open setting", page.ActionLabel);
        Assert.Equal(ResourceAction.OpenSetting, page.Action);
        Assert.False(ResourceActions.CanShowInFolder(page));
    }

    [Fact]
    public void ConsolesAndPrograms_CanRunAsAdministrator_PagesCannot()
    {
        Assert.True(ResourceActions.CanRunAsAdministrator(Resources().Single(r => r.Target == "services.msc")));
        Assert.True(ResourceActions.CanRunAsAdministrator(Resources().Single(r => r.Target == "rundll32.exe sysdm.cpl,EditEnvironmentVariables")));
        Assert.False(ResourceActions.CanRunAsAdministrator(Resources().Single(r => r.Target == "ms-settings:windowsupdate")));
    }
}
