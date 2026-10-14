namespace Coursee.Core.Tests.Settings;

public sealed class SettingsStoreFoldersTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void TheDesktop_IsTheDefaultFolder()
    {
        Assert.Equal(new[] { SettingsStore.DesktopFolder }, new SettingsStore(FilePath).Folders);
    }

    [Fact]
    public void AddedFolders_ArePersisted_InTheirOrder()
    {
        var store = new SettingsStore(FilePath);

        Assert.True(store.AddFolder(@"D:\code"));
        Assert.True(store.AddFolder(@"D:\notes"));

        Assert.Equal(new[] { SettingsStore.DesktopFolder, @"D:\code", @"D:\notes" }, new SettingsStore(FilePath).Folders);
    }

    [Fact]
    public void AFolderIsAddedOnce_IgnoringCase()
    {
        var store = new SettingsStore(FilePath);
        store.AddFolder(@"D:\code");

        Assert.False(store.AddFolder(@"d:\CODE"));
        Assert.Equal(2, store.Folders.Count);
    }

    [Fact]
    public void TheDesktop_CanBeRemoved_AndStaysRemoved()
    {
        var store = new SettingsStore(FilePath);

        Assert.True(store.RemoveFolder("desktop"));

        Assert.Empty(new SettingsStore(FilePath).Folders);
    }

    [Fact]
    public void RemovingAnUnknownFolder_ReturnsFalse()
    {
        Assert.False(new SettingsStore(FilePath).RemoveFolder(@"D:\nowhere"));
    }

    [Fact]
    public void BlankAndRepeatedFoldersInTheFile_AreIgnored()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{ "Folders": ["D:\\code", " ", "d:\\code", "Desktop"] }""");

        Assert.Equal(new[] { @"D:\code", "Desktop" }, new SettingsStore(FilePath).Folders);
    }

    [Fact]
    public void Folders_IsACopy()
    {
        var store = new SettingsStore(FilePath);
        var before = store.Folders;

        store.AddFolder(@"D:\code");

        Assert.Single(before);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
