namespace Coursee.Core.Tests.Settings;

public sealed class SettingsStoreOptionsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void Defaults_AreCtrlAltSpaceAndTwentyRecents()
    {
        var store = new SettingsStore(FilePath);

        Assert.Equal(HotkeyGesture.Default, store.Hotkey);
        Assert.Equal(20, store.RecentsCapacity);
    }

    [Fact]
    public void Hotkey_IsPersisted()
    {
        var gesture = new HotkeyGesture(true, false, true, false, "K");
        new SettingsStore(FilePath).SetHotkey(gesture);

        Assert.Equal(gesture, new SettingsStore(FilePath).Hotkey);
    }

    [Fact]
    public void SetHotkey_RejectsAnInvalidGesture()
    {
        var store = new SettingsStore(FilePath);

        Assert.Throws<ArgumentException>(() => store.SetHotkey(new HotkeyGesture(false, false, false, false, "A")));
        Assert.Equal(HotkeyGesture.Default, store.Hotkey);
    }

    [Fact]
    public void TypedCommands_AreAllowedByDefault_AndTheNoticeIsNotSeen()
    {
        var store = new SettingsStore(FilePath);

        Assert.True(store.AllowTypedCommands);
        Assert.False(store.SafetyNoticeSeen);
    }

    [Fact]
    public void TypedCommandsAndTheNotice_ArePersisted()
    {
        var store = new SettingsStore(FilePath);
        store.SetAllowTypedCommands(false);
        store.SetSafetyNoticeSeen();

        var reloaded = new SettingsStore(FilePath);

        Assert.False(reloaded.AllowTypedCommands);
        Assert.True(reloaded.SafetyNoticeSeen);
    }

    [Fact]
    public void UpdateCheck_IsOnByDefault_AndCanBeSwitchedOff()
    {
        Assert.True(new SettingsStore(FilePath).CheckForUpdates);

        new SettingsStore(FilePath).SetCheckForUpdates(false);

        Assert.False(new SettingsStore(FilePath).CheckForUpdates);
    }

    [Fact]
    public void Theme_FollowsWindowsByDefault()
    {
        Assert.Equal(ThemeChoice.System, new SettingsStore(FilePath).Theme);
    }

    [Theory]
    [InlineData(ThemeChoice.Light)]
    [InlineData(ThemeChoice.Dark)]
    [InlineData(ThemeChoice.System)]
    public void Theme_IsPersisted(ThemeChoice theme)
    {
        new SettingsStore(FilePath).SetTheme(theme);

        Assert.Equal(theme, new SettingsStore(FilePath).Theme);
    }

    [Theory]
    [InlineData("{\"Theme\":\"Purple\"}")]
    [InlineData("{\"Theme\":\"7\"}")]
    [InlineData("{\"Theme\":null}")]
    [InlineData("{}")]
    public void UnknownOrMissingTheme_FollowsWindows(string json)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, json);

        Assert.Equal(ThemeChoice.System, new SettingsStore(FilePath).Theme);
    }

    [Fact]
    public void StoredTheme_IgnoresCase()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{\"Theme\":\"dark\"}");

        Assert.Equal(ThemeChoice.Dark, new SettingsStore(FilePath).Theme);
    }

    [Fact]
    public void LauncherPosition_IsCenteredByDefault()
    {
        Assert.Equal(LauncherPosition.Centered, new SettingsStore(FilePath).LauncherPosition);
    }

    [Theory]
    [InlineData(LauncherPosition.Centered)]
    [InlineData(LauncherPosition.AtMouse)]
    [InlineData(LauncherPosition.LastPosition)]
    public void LauncherPosition_IsPersisted(LauncherPosition position)
    {
        new SettingsStore(FilePath).SetLauncherPosition(position);

        Assert.Equal(position, new SettingsStore(FilePath).LauncherPosition);
    }

    [Theory]
    [InlineData("{\"LauncherPosition\":\"Somewhere\"}", LauncherPosition.Centered)]
    [InlineData("{\"LauncherPosition\":\"9\"}", LauncherPosition.Centered)]
    [InlineData("{\"LauncherPosition\":\"atmouse\"}", LauncherPosition.AtMouse)]
    [InlineData("{\"Theme\":\"Dark\"}", LauncherPosition.Centered)]
    public void StoredLauncherPosition_UnknownIsCentered_CaseIgnored(string json, LauncherPosition expected)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, json);

        Assert.Equal(expected, new SettingsStore(FilePath).LauncherPosition);
    }

    [Fact]
    public void RecentsCapacity_IsPersisted()
    {
        new SettingsStore(FilePath).SetRecentsCapacity(10);

        Assert.Equal(10, new SettingsStore(FilePath).RecentsCapacity);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(500, 100)]
    public void SetRecentsCapacity_IsClamped(int requested, int expected)
    {
        var store = new SettingsStore(FilePath);

        store.SetRecentsCapacity(requested);

        Assert.Equal(expected, store.RecentsCapacity);
    }

    [Fact]
    public void UnreadableValues_FallBackToDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{ "Hotkey": "nonsense", "RecentsCapacity": 0 }""");

        var store = new SettingsStore(FilePath);

        Assert.Equal(HotkeyGesture.Default, store.Hotkey);
        Assert.Equal(20, store.RecentsCapacity);
    }

    [Fact]
    public void FileFromAnOlderVersion_KeepsDisabledSources()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{ "DisabledSources": ["Firefox"] }""");

        var store = new SettingsStore(FilePath);

        Assert.False(store.IsSourceEnabled("Firefox"));
        Assert.Equal(HotkeyGesture.Default, store.Hotkey);
        Assert.Equal(20, store.RecentsCapacity);
    }

    [Fact]
    public void ChangingOneSetting_KeepsTheOthers()
    {
        var store = new SettingsStore(FilePath);
        store.SetSourceEnabled("Firefox", false);
        store.SetRecentsCapacity(30);
        store.SetHotkey(new HotkeyGesture(false, true, false, false, "F2"));
        store.SetLauncherPosition(LauncherPosition.AtMouse);

        var reloaded = new SettingsStore(FilePath);

        Assert.Equal(LauncherPosition.AtMouse, reloaded.LauncherPosition);
        Assert.False(reloaded.IsSourceEnabled("Firefox"));
        Assert.Equal(30, reloaded.RecentsCapacity);
        Assert.Equal("Alt+F2", reloaded.Hotkey.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
