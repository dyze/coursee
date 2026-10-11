namespace Coursee.Core.Tests;

public class AutoStartCommandTests
{
    private const string Exe = @"C:\Program Files\coursee\coursee.exe";

    [Fact]
    public void Build_QuotesThePath()
    {
        Assert.Equal("\"C:\\Program Files\\coursee\\coursee.exe\"", AutoStartCommand.Build(Exe));
    }

    [Fact]
    public void IsCurrent_TrueForTheSameExecutable()
    {
        Assert.True(AutoStartCommand.IsCurrent(AutoStartCommand.Build(Exe), Exe));
    }

    [Fact]
    public void IsCurrent_IgnoresCaseAndSurroundingSpaces()
    {
        var stored = "  " + AutoStartCommand.Build(Exe).ToUpperInvariant() + " ";

        Assert.True(AutoStartCommand.IsCurrent(stored, Exe));
    }

    [Fact]
    public void IsCurrent_FalseForAnotherLocation()
    {
        var stored = AutoStartCommand.Build(@"D:\old\coursee.exe");

        Assert.False(AutoStartCommand.IsCurrent(stored, Exe));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsCurrent_FalseWhenNothingIsStored(string? stored)
    {
        Assert.False(AutoStartCommand.IsCurrent(stored, Exe));
    }
}
