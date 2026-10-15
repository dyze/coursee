namespace Coursee.Core.Tests.Games;

public class VdfNodeTests
{
    [Fact]
    public void KeysAndNestedNodes_AreRead()
    {
        var root = VdfNode.Parse("""
            "AppState"
            {
                "appid"     "620"
                "name"      "Portal 2"
                "UserConfig"
                {
                    "language"  "english"
                }
            }
            """);

        var state = root["AppState"]!;
        Assert.Null(state.Value);
        Assert.Equal("620", state.ValueOf("appid"));
        Assert.Equal("Portal 2", state.ValueOf("name"));
        Assert.Equal("english", state["UserConfig"]!.ValueOf("language"));
        Assert.Equal(new[] { "appid", "name", "UserConfig" }, state.Children.Keys);
    }

    [Fact]
    public void Keys_IgnoreCase()
    {
        Assert.NotNull(VdfNode.Parse("\"LibraryFolders\" { }")["libraryfolders"]);
    }

    [Fact]
    public void AMissingKey_IsNull()
    {
        var root = VdfNode.Parse("\"a\" \"1\"");

        Assert.Null(root["b"]);
        Assert.Null(root.ValueOf("b"));
    }

    [Fact]
    public void Escapes_AreUnderstood()
    {
        var root = VdfNode.Parse(@"""path"" ""D:\\Steam \""Library\""\tx\ny""");

        Assert.Equal("D:\\Steam \"Library\"\tx\ny", root.ValueOf("path"));
    }

    [Fact]
    public void CommentsAndBareStrings_AreAllowed()
    {
        var root = VdfNode.Parse("""
            // written by Steam
            key value // trailing
            other "{"
            """);

        Assert.Equal("value", root.ValueOf("key"));
        Assert.Equal("{", root.ValueOf("other"));
    }

    [Fact]
    public void ARepeatedKey_KeepsTheFirst()
    {
        Assert.Equal("1", VdfNode.Parse("\"a\" \"1\" \"a\" \"2\"").ValueOf("a"));
    }

    [Fact]
    public void AnEmptyText_HasNoKeys()
    {
        Assert.Empty(VdfNode.Parse("  // nothing\n").Children);
    }

    [Theory]
    [InlineData("\"a\" {")]
    [InlineData("\"a\" { \"b\" \"1\" } }")]
    [InlineData("{ \"a\" \"1\" }")]
    [InlineData("\"a\"")]
    [InlineData("\"a\" { \"b\" }")]
    [InlineData("\"a\" \"1")]
    public void MalformedText_IsAFormatError(string text)
    {
        Assert.Throws<FormatException>(() => VdfNode.Parse(text));
    }
}
