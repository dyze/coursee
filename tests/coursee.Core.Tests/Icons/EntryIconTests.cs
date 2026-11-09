namespace Coursee.Core.Tests.Icons;

public class EntryIconTests
{
    [Fact]
    public void ToString_IsTheStoredText_ReadBackByParse()
    {
        Assert.Equal("library:game", EntryIcon.Library("game").ToString());
        Assert.Equal("file:ab12.png", EntryIcon.File("ab12.png").ToString());
        Assert.Equal(EntryIcon.Library("game"), EntryIcon.Parse("library:game"));
        Assert.Equal(EntryIcon.File("ab12.png"), EntryIcon.Parse("FILE: ab12.png "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("game")]
    [InlineData("library:")]
    [InlineData("file:  ")]
    [InlineData("file:..")]
    [InlineData("file:.")]
    [InlineData(@"file:..\secret.png")]
    [InlineData("file:sub/a.png")]
    [InlineData(@"file:C:a.png")]
    public void Parse_RefusesWhatIsNoIcon_OrLeavesTheFolder(string? text)
    {
        Assert.Null(EntryIcon.Parse(text));
    }
}
