namespace Coursee.Core.Tests.Icons;

public class SvgIconTests
{
    [Fact]
    public void Read_APlainPath_KeepsItsDataAndViewBox()
    {
        var icon = SvgIcon.Read("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="#000000" d="M 1 1 L 2 2 z"/></svg>""");

        Assert.Equal(new SvgIcon(new SvgViewBox(0, 0, 24, 24), "M 1 1 L 2 2 z"), icon);
    }

    [Fact]
    public void Read_JoinsThePaths_InGroupsToo()
    {
        var icon = SvgIcon.Read("""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="2,2,20,20">
              <title>Two</title>
              <path d="M1 1z"/>
              <g fill="red"><path d=" M2 2z "/></g>
              <path d=""/>
            </svg>
            """);

        Assert.Equal(new SvgIcon(new SvgViewBox(2, 2, 20, 20), "M1 1z M2 2z"), icon);
    }

    [Fact]
    public void Read_WithoutViewBox_UsesTheSize()
    {
        var icon = SvgIcon.Read("""<svg width="32px" height="16"><path d="M1 1z"/></svg>""");

        Assert.Equal(new SvgViewBox(0, 0, 32, 16), icon?.ViewBox);
    }

    [Fact]
    public void Read_IgnoresAnEditorsElementsAndDefinitions()
    {
        var icon = SvgIcon.Read("""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" viewBox="0 0 24 24">
              <sodipodi:namedview />
              <defs><rect width="1" height="1"/><path d="M9 9z"/></defs>
              <path d="M1 1z"/>
            </svg>
            """);

        Assert.Equal("M1 1z", icon?.PathData);
    }

    [Theory]
    [InlineData("not xml")]
    [InlineData("""<html><path d="M1 1z"/></html>""")]
    [InlineData("""<svg viewBox="0 0 24 24"></svg>""")]
    [InlineData("""<svg viewBox="0 0 24 24"><circle r="4"/><path d="M1 1z"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 24 24"><path d="M1 1z" transform="scale(2)"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 24 24"><g transform="rotate(5)"><path d="M1 1z"/></g></svg>""")]
    [InlineData("""<svg viewBox="0 0 24"><path d="M1 1z"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 0 24"><path d="M1 1z"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 a 24"><path d="M1 1z"/></svg>""")]
    [InlineData("""<svg><path d="M1 1z"/></svg>""")]
    [InlineData("""<svg width="0" height="10"><path d="M1 1z"/></svg>""")]
    public void Read_RefusesWhatIsNotPlainPaths(string svg)
    {
        Assert.Null(SvgIcon.Read(svg));
    }
}
