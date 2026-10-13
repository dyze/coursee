namespace Coursee.Core.Tests.Settings;

public class SettingsCatalogTests
{
    private sealed class NamedSource(string name, string displayName, bool canBeDisabled) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => displayName;

        public bool CanBeDisabled => canBeDisabled;

        public string Icon => string.Empty;

        public IReadOnlyList<Resource> GetResources() => [];
    }

    private static SettingsCatalog Catalog() => new(
    [
        new NamedSource("Desktop", "Desktop shortcuts", canBeDisabled: false),
        new NamedSource("Firefox", "Firefox bookmarks", canBeDisabled: true),
        new NamedSource("Windows Run", "Windows Run history (Win+R)", canBeDisabled: true),
    ]);

    [Fact]
    public void EverySwitchableSource_GetsAnEntryInTheSourcesTab()
    {
        var sources = Catalog().All.Where(entry => entry.Tab == SettingsCatalog.SourcesTab).ToList();

        Assert.Equal(new[] { "Firefox bookmarks", "Windows Run history (Win+R)" }, sources.Select(entry => entry.Label));
        Assert.Equal(SettingsCatalog.SourceId("Firefox"), sources[0].Id);
    }

    [Fact]
    public void EveryEntryBelongsToAKnownTab()
    {
        var catalog = Catalog();

        Assert.All(catalog.All, entry => Assert.Contains(entry.Tab, catalog.Tabs));
    }

    [Fact]
    public void IdsAreUnique()
    {
        var ids = Catalog().All.Select(entry => entry.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyQuery_FindsNothing(string? query)
    {
        Assert.Empty(Catalog().Find(query));
    }

    [Fact]
    public void Find_MatchesTheLabel_IgnoringCase()
    {
        Assert.Equal(SettingsCatalog.HotkeyId, Assert.Single(Catalog().Find("HOTKEY")).Id);
    }

    [Fact]
    public void Find_MatchesAKeyword()
    {
        Assert.Contains(Catalog().Find("shortcut"), entry => entry.Id == SettingsCatalog.HotkeyId);
    }

    [Fact]
    public void Find_MatchesTheTabName()
    {
        var found = Catalog().Find("logging");

        Assert.Contains(found, entry => entry.Id == SettingsCatalog.DebugLoggingId);
        Assert.Contains(found, entry => entry.Id == SettingsCatalog.LogWindowId);
    }

    [Fact]
    public void Find_NeedsEveryWord()
    {
        var found = Catalog().Find("firefox bookmarks");

        Assert.Equal(SettingsCatalog.SourceId("Firefox"), Assert.Single(found).Id);
        Assert.Empty(Catalog().Find("firefox hotkey"));
    }

    [Fact]
    public void Find_DarkFindsTheTheme()
    {
        Assert.Equal(SettingsCatalog.ThemeId, Assert.Single(Catalog().Find("dark")).Id);
    }

    [Theory]
    [InlineData("mouse")]
    [InlineData("launcher position")]
    [InlineData("centre")]
    public void Find_FindsTheLauncherPosition(string query)
    {
        Assert.Contains(Catalog().Find(query), entry => entry.Id == SettingsCatalog.LauncherPositionId);
        Assert.Equal(SettingsCatalog.GeneralTab, Catalog().All.Single(entry => entry.Id == SettingsCatalog.LauncherPositionId).Tab);
    }

    [Theory]
    [InlineData("accents")]
    [InlineData("diacritic")]
    public void Find_FindsIgnoreAccents(string query)
    {
        Assert.Equal(SettingsCatalog.IgnoreAccentsId, Assert.Single(Catalog().Find(query)).Id);
        Assert.Equal(SettingsCatalog.GeneralTab, Catalog().All.Single(entry => entry.Id == SettingsCatalog.IgnoreAccentsId).Tab);
    }

    [Theory]
    [InlineData("hidden")]
    [InlineData("restore")]
    [InlineData("custom")]
    [InlineData("alias")]
    [InlineData("unpin")]
    public void Find_FindsTheEntries(string query)
    {
        Assert.Contains(Catalog().Find(query), entry => entry.Id == SettingsCatalog.EntriesId);
        Assert.Equal(SettingsCatalog.EntriesTab, Catalog().All.Single(entry => entry.Id == SettingsCatalog.EntriesId).Tab);
    }

    [Fact]
    public void Tabs_EntriesComeAfterSources()
    {
        Assert.Equal(
            new[] { SettingsCatalog.GeneralTab, SettingsCatalog.AppearanceTab, SettingsCatalog.SourcesTab, SettingsCatalog.EntriesTab, SettingsCatalog.LoggingTab },
            Catalog().Tabs);
    }

    [Fact]
    public void Find_KeepsTheCatalogOrder()
    {
        var found = Catalog().Find("source");

        Assert.Equal(new[] { SettingsCatalog.SourceId("Firefox"), SettingsCatalog.SourceId("Windows Run") }, found.Select(entry => entry.Id));
    }
}
