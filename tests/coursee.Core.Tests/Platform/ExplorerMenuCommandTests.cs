namespace Coursee.Core.Tests.Platform;

public class ExplorerMenuCommandTests
{
    private const string Exe = @"C:\Program Files\coursee\coursee.exe";

    [Fact]
    public void Build_QuotesThePathAndTheItem()
    {
        Assert.Equal("\"C:\\Program Files\\coursee\\coursee.exe\" --add \"%1\"", ExplorerMenuCommand.Build(Exe));
    }

    [Fact]
    public void PathToAdd_IsTheArgumentAfterAdd()
    {
        Assert.Equal(@"C:\my files\a.txt", ExplorerMenuCommand.PathToAdd(["--add", @"C:\my files\a.txt"]));
        Assert.Equal(@"D:\code", ExplorerMenuCommand.PathToAdd(["--hidden", "--ADD", @" D:\code "]));
    }

    public static readonly TheoryData<string[]> WithoutAPath =
    [
        [],
        ["--hidden"],
        ["--add"],
        ["--add", "  "],
        [@"C:\a.txt", "--add"],
    ];

    [Theory]
    [MemberData(nameof(WithoutAPath))]
    public void PathToAdd_NullWithoutAPath(string[] arguments)
    {
        Assert.Null(ExplorerMenuCommand.PathToAdd(arguments));
    }

    [Fact]
    public void Status_ThisCopy_ForTheBuiltCommand_IgnoringCase()
    {
        Assert.Equal(ExplorerMenuStatus.ThisCopy, ExplorerMenuCommand.Status(ExplorerMenuCommand.Build(Exe), Exe));
        Assert.Equal(ExplorerMenuStatus.ThisCopy, ExplorerMenuCommand.Status(ExplorerMenuCommand.Build(Exe).ToUpperInvariant(), Exe));
    }

    [Fact]
    public void Status_AnotherLocation_IsAnotherCopy()
    {
        Assert.Equal(ExplorerMenuStatus.OtherCopy, ExplorerMenuCommand.Status(ExplorerMenuCommand.Build(@"D:\old\coursee.exe"), Exe));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Status_OffWhenNothingIsStored(string? stored)
    {
        Assert.Equal(ExplorerMenuStatus.Off, ExplorerMenuCommand.Status(stored, Exe));
        Assert.Null(ExplorerMenuCommand.ProgramOf(stored));
    }

    [Fact]
    public void ProgramOf_IsThePathWithoutArguments()
    {
        Assert.Equal(@"D:\old\coursee.exe", ExplorerMenuCommand.ProgramOf(ExplorerMenuCommand.Build(@"D:\old\coursee.exe")));
    }
}
