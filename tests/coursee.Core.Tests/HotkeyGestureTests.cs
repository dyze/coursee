namespace Coursee.Core.Tests;

public class HotkeyGestureTests
{
    [Fact]
    public void Default_IsCtrlAltSpace()
    {
        Assert.Equal("Ctrl+Alt+Space", HotkeyGesture.Default.ToString());
        Assert.True(HotkeyGesture.Default.IsValid);
    }

    [Fact]
    public void ToString_ListsModifiersInFixedOrder()
    {
        var gesture = new HotkeyGesture(Ctrl: true, Alt: true, Shift: true, Win: true, Key: "K");

        Assert.Equal("Ctrl+Alt+Shift+Win+K", gesture.ToString());
    }

    [Fact]
    public void TryParse_RoundTripsToString()
    {
        var original = new HotkeyGesture(Ctrl: false, Alt: true, Shift: true, Win: false, Key: "F5");

        Assert.True(HotkeyGesture.TryParse(original.ToString(), out var parsed));
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void TryParse_IsCaseInsensitiveAndAcceptsAliases()
    {
        Assert.True(HotkeyGesture.TryParse(" control + WINDOWS + Space ", out var parsed));
        Assert.Equal(new HotkeyGesture(true, false, false, true, "Space"), parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Space")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+A+B")]
    [InlineData("Ctrl++")]
    public void TryParse_RejectsInvalidText(string? text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out var parsed));
        Assert.Equal(HotkeyGesture.Default, parsed);
    }

    [Fact]
    public void IsValid_NeedsAModifierAndAKey()
    {
        Assert.False(new HotkeyGesture(false, false, false, false, "A").IsValid);
        Assert.False(new HotkeyGesture(true, false, false, false, " ").IsValid);
        Assert.True(new HotkeyGesture(true, false, false, false, "A").IsValid);
    }

    [Fact]
    public void IsValid_ShiftAloneIsNotEnough()
    {
        Assert.False(new HotkeyGesture(Ctrl: false, Alt: false, Shift: true, Win: false, Key: "A").IsValid);
        Assert.True(new HotkeyGesture(Ctrl: true, Alt: false, Shift: true, Win: false, Key: "A").IsValid);
        Assert.True(new HotkeyGesture(Ctrl: false, Alt: false, Shift: true, Win: true, Key: "A").IsValid);
    }

    [Fact]
    public void TryParse_RejectsShiftAlone()
    {
        Assert.False(HotkeyGesture.TryParse("Shift+A", out _));
    }
}
