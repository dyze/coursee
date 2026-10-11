namespace Coursee.Core.Tests;

public class FallbackClassifierTests
{
    private static readonly Func<string, bool> Never = _ => false;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInput_ReturnsNull(string? input)
    {
        Assert.Null(FallbackClassifier.Classify(input, Never, Never));
    }

    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("http://example.com")]
    [InlineData("ftp://host/file.zip")]
    public void SchemeUrls_AreOpenedAsUrls(string input)
    {
        var result = FallbackClassifier.Classify(input, Never, Never);

        Assert.Equal(new Resource(input, input, ResourceKind.Url, "Typed"), result);
    }

    [Fact]
    public void WwwAddress_GetsAnHttpsScheme()
    {
        var result = FallbackClassifier.Classify("www.example.com/docs", Never, Never);

        Assert.Equal(new Resource("www.example.com/docs", "https://www.example.com/docs", ResourceKind.Url, "Typed"), result);
    }

    [Fact]
    public void UrlWithSpaces_IsNotAUrl()
    {
        var result = FallbackClassifier.Classify("https://example.com is down", Never, Never);

        Assert.Equal(ResourceKind.Command, result!.Kind);
    }

    [Fact]
    public void ExistingFolder_IsOpenedAsFolder()
    {
        var result = FallbackClassifier.Classify(@"C:\work", dir => dir == @"C:\work", Never);

        Assert.Equal(new Resource(@"C:\work", @"C:\work", ResourceKind.Folder, "Typed"), result);
    }

    [Fact]
    public void ExistingFile_IsOpenedAsFile()
    {
        var result = FallbackClassifier.Classify(@"C:\work\notes.txt", Never, file => file == @"C:\work\notes.txt");

        Assert.Equal(ResourceKind.File, result!.Kind);
        Assert.Equal(@"C:\work\notes.txt", result.Target);
    }

    [Fact]
    public void QuotedPath_IsUnquotedForTheTargetButKeptInTheName()
    {
        var result = FallbackClassifier.Classify("  \"C:\\Program Files\\App\"  ", dir => dir == @"C:\Program Files\App", Never);

        Assert.Equal(new Resource("\"C:\\Program Files\\App\"", @"C:\Program Files\App", ResourceKind.Folder, "Typed"), result);
    }

    [Fact]
    public void EnvironmentVariables_AreExpanded()
    {
        const string variable = "COURSEE_TEST_BASE";
        Environment.SetEnvironmentVariable(variable, "base");
        try
        {
            var result = FallbackClassifier.Classify("%COURSEE_TEST_BASE%\\sub", dir => dir == "base\\sub", Never);

            Assert.Equal(ResourceKind.Folder, result!.Kind);
            Assert.Equal("base\\sub", result.Target);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Theory]
    [InlineData(@"\\server")]
    [InlineData(@"\\server\share\folder")]
    [InlineData(@"\\server\share\my notes.txt")]
    public void NetworkPath_IsOpenedWithoutTouchingTheDisk(string input)
    {
        Func<string, bool> fail = path => throw new InvalidOperationException("Disk checked for " + path);

        var result = FallbackClassifier.Classify(input, fail, fail);

        Assert.Equal(new Resource(input, input, ResourceKind.NetworkPath, "Typed"), result);
        Assert.Equal("Open", result!.ActionLabel);
    }

    [Fact]
    public void QuotedNetworkPath_IsUnquoted()
    {
        var result = FallbackClassifier.Classify("\"\\\\server\\my share\"", Never, Never);

        Assert.Equal(ResourceKind.NetworkPath, result!.Kind);
        Assert.Equal(@"\\server\my share", result.Target);
    }

    [Fact]
    public void PathThatDoesNotExist_IsRunAsACommand()
    {
        var result = FallbackClassifier.Classify(@"C:\nope\app.exe", Never, Never);

        Assert.Equal(new Resource(@"C:\nope\app.exe", @"C:\nope\app.exe", ResourceKind.Command, "Typed"), result);
    }

    [Theory]
    [InlineData("notepad")]
    [InlineData("calc")]
    [InlineData("cmd /k dir")]
    public void AnythingElse_IsRunAsACommand(string input)
    {
        var result = FallbackClassifier.Classify(input, Never, Never);

        Assert.Equal(new Resource(input, input, ResourceKind.Command, "Typed"), result);
    }

    [Fact]
    public void TypedResources_UseTheTypedSourceName()
    {
        Assert.Equal(FallbackClassifier.SourceName, FallbackClassifier.Classify("notepad", Never, Never)!.Source);
        Assert.Equal("Typed", FallbackClassifier.SourceName);
    }
}
