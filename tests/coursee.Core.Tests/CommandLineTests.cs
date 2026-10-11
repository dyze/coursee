namespace Coursee.Core.Tests;

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
}
