namespace Coursee.Core.Tests.Platform;

public class AutoStartCommandTests
{
    private const string Exe = @"C:\Program Files\coursee\coursee.exe";

    [Fact]
    public void Build_QuotesThePath_AndStartsHidden()
    {
        Assert.Equal("\"C:\\Program Files\\coursee\\coursee.exe\" --hidden", AutoStartCommand.Build(Exe));
    }

    [Fact]
    public void Status_ThisCopy_ForTheBuiltCommand()
    {
        Assert.Equal(AutoStartStatus.ThisCopy, AutoStartCommand.Status(AutoStartCommand.Build(Exe), Exe));
    }

    [Fact]
    public void Status_IgnoresCaseAndSurroundingSpaces()
    {
        var stored = "  " + AutoStartCommand.Build(Exe).ToUpperInvariant() + " ";

        Assert.Equal(AutoStartStatus.ThisCopy, AutoStartCommand.Status(stored, Exe));
    }

    [Fact]
    public void Status_EntryFromBeforeTheHiddenArgument_IsOutdated()
    {
        // What older versions of coursee and of the installer wrote.
        Assert.Equal(AutoStartStatus.ThisCopyOutdated, AutoStartCommand.Status($"\"{Exe}\"", Exe));
    }

    [Fact]
    public void Status_AnotherLocation_IsAnotherCopy()
    {
        Assert.Equal(AutoStartStatus.OtherCopy, AutoStartCommand.Status(AutoStartCommand.Build(@"D:\old\coursee.exe"), Exe));
        Assert.Equal(AutoStartStatus.OtherCopy, AutoStartCommand.Status("\"D:\\old\\coursee.exe\"", Exe));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Status_OffWhenNothingIsStored(string? stored)
    {
        Assert.Equal(AutoStartStatus.Off, AutoStartCommand.Status(stored, Exe));
    }

    [Fact]
    public void ProgramOf_IsThePathWithoutArguments()
    {
        Assert.Equal(@"D:\old\coursee.exe", AutoStartCommand.ProgramOf("\"D:\\old\\coursee.exe\" --hidden"));
        Assert.Null(AutoStartCommand.ProgramOf(" "));
    }

    [Theory]
    [InlineData(new[] { "--hidden" }, true)]
    [InlineData(new[] { "--HIDDEN" }, true)]
    [InlineData(new[] { "other", "--hidden" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--hide" }, false)]
    public void StartsHidden_LooksForTheHiddenArgument(string[] arguments, bool expected)
    {
        Assert.Equal(expected, AutoStartCommand.StartsHidden(arguments));
    }
}
