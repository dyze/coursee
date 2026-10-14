namespace Coursee.Core.Tests.Resources;

public class CommandLineTests
{
    [Fact]
    public void ProgramOnly()
    {
        Assert.Equal(("notepad", ""), CommandLine.Split("notepad"));
    }

    [Fact]
    public void ProgramAndArguments()
    {
        Assert.Equal(("notepad", "a.txt"), CommandLine.Split("notepad a.txt"));
    }

    [Fact]
    public void ExtraSpaces_AreTrimmed()
    {
        Assert.Equal(("cmd", "/k dir"), CommandLine.Split("  cmd   /k dir "));
    }

    [Fact]
    public void QuotedProgram_MayContainSpaces()
    {
        Assert.Equal((@"C:\Program Files\x.exe", "/s /q"), CommandLine.Split("\"C:\\Program Files\\x.exe\" /s /q"));
    }

    [Fact]
    public void UnterminatedQuote_TakesTheRestAsTheProgram()
    {
        Assert.Equal((@"C:\Program Files\x.exe", ""), CommandLine.Split("\"C:\\Program Files\\x.exe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_GivesEmptyParts(string input)
    {
        Assert.Equal(("", ""), CommandLine.Split(input));
    }

    private static Func<string, bool> Programs(params string[] programs) => programs.Contains;

    [Fact]
    public void ExistingProgramWithSpaces_IsTheWholeText()
    {
        Assert.Equal(("kill rds.cmd", ""), CommandLine.Split("kill rds.cmd", Programs("kill rds.cmd")));
    }

    [Fact]
    public void ExistingProgramWithSpaces_KeepsItsArguments()
    {
        Assert.Equal(("my tool.cmd", "-v now"), CommandLine.Split("my tool.cmd  -v now", Programs("my tool.cmd")));
    }

    [Fact]
    public void ShortestExistingProgram_Wins()
    {
        Assert.Equal(("notepad", "my notes.txt"), CommandLine.Split("notepad my notes.txt", Programs("notepad", "notepad my")));
    }

    [Fact]
    public void NoExistingProgram_SplitsAtTheFirstSpace()
    {
        Assert.Equal(("kill", "rds.cmd"), CommandLine.Split("kill rds.cmd", Programs()));
    }

    [Fact]
    public void QuotedProgram_IsNotLookedUp()
    {
        Assert.Equal(("a b", "c"), CommandLine.Split("\"a b\" c", _ => throw new InvalidOperationException()));
    }
}
