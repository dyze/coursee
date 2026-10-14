namespace Coursee.Core.Tests;

public class AppFilesTests
{
    private static readonly string Data = Path.Combine(Path.GetTempPath(), "coursee-data");
    private static readonly string Program = Path.Combine(Path.GetTempPath(), "coursee-program");

    [Fact]
    public void TheUserFiles_AreInTheDataFolder()
    {
        var files = new AppFiles(Data, Program);

        Assert.Equal(Path.Combine(Data, "settings.json"), files.Settings);
        Assert.Equal(Path.Combine(Data, "entries.json"), files.Entries);
        Assert.Equal(Path.Combine(Data, "custom.json"), files.CustomEntries);
        Assert.Equal(Path.Combine(Data, "recents.json"), files.Recents);
        Assert.Equal(Path.Combine(Data, "windows.json"), files.WindowPositions);
        Assert.Equal(Path.Combine(Data, "logs"), files.Logs);
        Assert.Equal(Data, files.DataDirectory);
    }

    [Fact]
    public void All_ListsEveryFileOnce_ThenTheProgram()
    {
        var files = new AppFiles(Data, Program);

        Assert.Equal(7, files.All.Count);
        Assert.Equal(files.All.Count, files.All.Select(file => file.Path).Distinct().Count());
        Assert.Equal(new AppFile("Settings", files.Settings), files.All[0]);
        Assert.Equal(new AppFile("Program", Program), files.All[^1]);
    }

    [Fact]
    public void AnExistingFile_IsSelectedInItsFolder()
    {
        var path = Path.Combine(Data, "settings.json");

        Assert.Equal($"/select,\"{path}\"", AppFiles.ShowInFolderArguments(path, _ => true));
    }

    [Fact]
    public void AFolder_IsSelectedWithoutItsTrailingSeparator()
    {
        Assert.Equal($"/select,\"{Program}\"", AppFiles.ShowInFolderArguments(Program + Path.DirectorySeparatorChar, _ => true));
    }

    [Fact]
    public void AMissingFile_OpensTheFolderItWillBeIn()
    {
        var path = Path.Combine(Data, "custom.json");

        Assert.Equal($"\"{Data}\"", AppFiles.ShowInFolderArguments(path, _ => false));
    }

    [Fact]
    public void ByDefault_TheFileSystemIsAsked()
    {
        var missing = Path.Combine(Data, Guid.NewGuid().ToString("N"));

        Assert.Equal($"\"{Data}\"", AppFiles.ShowInFolderArguments(missing));
        Assert.StartsWith("/select,", AppFiles.ShowInFolderArguments(Path.GetTempPath()));
    }
}
