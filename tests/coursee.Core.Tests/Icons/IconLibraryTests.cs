namespace Coursee.Core.Tests.Icons;

public class IconLibraryTests
{
    [Fact]
    public void Names_AreEveryEmbeddedIcon_InOrder()
    {
        Assert.True(IconLibrary.Names.Count > 100);
        Assert.Equal(IconLibrary.Names.Order(StringComparer.Ordinal), IconLibrary.Names);
        Assert.Contains("game", IconLibrary.Names);
    }

    [Fact]
    public void EveryIcon_IsA24PixelPath()
    {
        Assert.All(IconLibrary.Names, name =>
        {
            var icon = IconLibrary.Find(name);
            Assert.NotNull(icon);
            Assert.Equal(new SvgViewBox(0, 0, 24, 24), icon.ViewBox);
        });
    }

    [Fact]
    public void Find_AnUnknownName_IsNull()
    {
        Assert.Null(IconLibrary.Find("no-such-icon"));
    }
}
