namespace Coursee.Core.Tests.Resources;

public class ProgramFinderTests
{
    private const string Home = @"C:\Users\me";
    private const string Tools = @"C:\tools";
    private const string Windows = @"C:\Windows";

    private static ProgramFinder Finder(params string[] existingFiles) =>
        new(Home, $"{Tools}; ;{Windows}", ".EXE;.CMD", path => existingFiles.Contains(path, StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void NameWithItsExtension_IsFoundInTheWorkingFolder()
    {
        var file = Path.Combine(Home, "kill rds.cmd");

        Assert.Equal(file, Finder(file).Find("kill rds.cmd"));
    }

    [Fact]
    public void WorkingFolder_ComesBeforePath()
    {
        var inHome = Path.Combine(Home, "tool.cmd");
        var inTools = Path.Combine(Tools, "tool.cmd");

        Assert.Equal(inHome, Finder(inTools, inHome).Find("tool.cmd"));
    }

    [Fact]
    public void BareName_IsFoundInPath_InItsOrder()
    {
        var inTools = Path.Combine(Tools, "notepad.exe");
        var inWindows = Path.Combine(Windows, "notepad.exe");

        Assert.Equal(inTools, Finder(inWindows, inTools).Find("notepad.exe"));
        Assert.Equal(inWindows, Finder(inWindows).Find("notepad.exe"));
    }

    [Fact]
    public void NameWithoutExtension_TriesTheExtensionsOfPathExt()
    {
        var file = Path.Combine(Windows, "notepad.exe");

        // The extension is added as PATHEXT writes it; Windows file names ignore case.
        Assert.Equal(file, Finder(file).Find("notepad"), ignoreCase: true);
        Assert.Null(Finder(Path.Combine(Windows, "notepad.com")).Find("notepad"));
    }

    [Fact]
    public void FullPath_IsOnlyItself()
    {
        var file = @"D:\scripts\run.cmd";

        Assert.Equal(file, Finder(file).Find(file));
        Assert.Null(Finder(Path.Combine(Home, "run.cmd")).Find(@"D:\other\run.cmd"));
    }

    [Fact]
    public void RelativePath_IsLookedForInTheWorkingFolderOnly()
    {
        var relative = @"scripts\run.cmd";
        var inHome = Path.Combine(Home, relative);

        Assert.Equal(inHome, Finder(inHome).Find(relative));
        Assert.Null(Finder(Path.Combine(Tools, relative)).Find(relative));
    }

    [Fact]
    public void Unknown_IsNotFound()
    {
        Assert.False(Finder().Exists("kill"));
        Assert.True(Finder(Path.Combine(Home, "kill.cmd")).Exists("kill"));
    }

    [Fact]
    public void NoPathVariables_StillSearchesTheWorkingFolder_WithTheUsualExtensions()
    {
        var file = Path.Combine(Home, "setup.bat");
        var finder = new ProgramFinder(Home, null, null, path => string.Equals(path, file, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(file, finder.Find("setup"), ignoreCase: true);
    }

    [Fact]
    public void ByDefault_UsesTheRealFileSystem()
    {
        var folder = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "my tool.cmd"), "@echo off");
            var finder = new ProgramFinder(folder, null, null);

            Assert.True(finder.Exists("my tool.cmd"));
            Assert.False(finder.Exists("my"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
