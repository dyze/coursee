namespace Coursee.Core.Tests.Sources;

public class SourceIconsTests
{
    private sealed class IconSource(string name, string displayName, string icon) : IResourceSource
    {
        public string Name => name;

        public string DisplayName => displayName;

        public bool CanBeDisabled => true;

        public string Icon => icon;

        public IReadOnlyList<Resource> GetResources() => [];
    }

    private static SourceIcons Icons() => new(
    [
        new IconSource("Firefox", "Firefox bookmarks", "\uE728"),
        new IconSource("Edge", "Edge favorites", "\uE734"),
    ]);

    [Fact]
    public void IconOf_IsTheIconOfTheSource_IgnoringCase()
    {
        Assert.Equal("\uE728", Icons().IconOf("Firefox"));
        Assert.Equal("\uE734", Icons().IconOf("edge"));
    }

    [Fact]
    public void LabelOf_IsTheDisplayNameOfTheSource() =>
        Assert.Equal("Firefox bookmarks", Icons().LabelOf("Firefox"));

    [Fact]
    public void TypedText_HasItsOwnIcon()
    {
        Assert.Equal(SourceIcons.Typed, Icons().IconOf(FallbackClassifier.SourceName));
        Assert.Equal("What you typed", Icons().LabelOf(FallbackClassifier.SourceName));
    }

    [Fact]
    public void UnknownSource_HasNoIcon_AndItsNameAsLabel()
    {
        Assert.Equal(string.Empty, Icons().IconOf("Removed"));
        Assert.Equal("Removed", Icons().LabelOf("Removed"));
    }

    [Fact]
    public void TwoSourcesWithTheSameName_TheFirstWins()
    {
        var icons = new SourceIcons([new IconSource("A", "First", "1"), new IconSource("A", "Second", "2")]);

        Assert.Equal("1", icons.IconOf("A"));
    }

    [Fact]
    public void RealSources_EachHaveADistinctIcon()
    {
        var resolver = new NoShortcuts();
        IResourceSource[] sources =
        [
            ShortcutsSource.Desktop([], resolver),
            ShortcutsSource.StartMenu([], resolver),
            new FirefoxBookmarksSource(null),
            new EdgeFavoritesSource(null),
            new RunHistorySource(new NoRunHistory()),
            new WindowsSettingsSource(),
        ];

        Assert.All(sources, source => Assert.False(string.IsNullOrEmpty(source.Icon)));
        Assert.Equal(sources.Length + 1, sources.Select(s => s.Icon).Append(SourceIcons.Typed).Distinct().Count());
    }

    private sealed class NoShortcuts : IShortcutResolver
    {
        public string? ResolveTarget(string shortcutPath) => null;
    }

    private sealed class NoRunHistory : IRunHistoryReader
    {
        public IReadOnlyDictionary<string, string> Read() => new Dictionary<string, string>();
    }
}
