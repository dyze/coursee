namespace Coursee.Core.Tests.Search;

public class TextFoldingTests
{
    [Theory]
    [InlineData("Événements", "Evenements")]
    [InlineData("Paramètres", "Parametres")]
    [InlineData("Ça déçoit", "Ca decoit")]
    [InlineData("naïve façade", "naive facade")]
    [InlineData(@"C:\Users\René\Bureau", @"C:\Users\Rene\Bureau")]
    public void RemovesAccents(string text, string expected)
    {
        Assert.Equal(expected, TextFolding.Fold(text));
    }

    [Fact]
    public void AsciiText_IsReturnedUnchanged()
    {
        const string text = "https://example.com/Docs?q=1";

        Assert.Same(text, TextFolding.Fold(text));
    }

    [Fact]
    public void LettersWithoutAccents_AreKept()
    {
        Assert.Equal("Straße œuvre", TextFolding.Fold("Straße œuvre"));
    }

    [Fact]
    public void AreEqual_IgnoresCaseAndAccents()
    {
        Assert.True(TextFolding.AreEqual("ÉTÉ", "ete"));
        Assert.False(TextFolding.AreEqual("ete", "etes"));
    }
}
