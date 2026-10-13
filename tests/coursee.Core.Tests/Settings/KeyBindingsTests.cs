namespace Coursee.Core.Tests.Settings;

public class KeyBindingsTests
{
    [Fact]
    public void EveryBinding_HasAKnownAreaKeysAndAction()
    {
        string[] areas = [KeyBindings.Launcher, KeyBindings.RowBox, KeyBindings.Settings];

        Assert.All(KeyBindings.All, binding =>
        {
            Assert.Contains(binding.Area, areas);
            Assert.False(string.IsNullOrWhiteSpace(binding.Keys));
            Assert.False(string.IsNullOrWhiteSpace(binding.Action));
        });
    }

    [Fact]
    public void KeysAreListedOncePerArea()
    {
        var keys = KeyBindings.All.Select(binding => (binding.Area, binding.Keys)).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void EveryKeyOfTheHintLines_IsListed()
    {
        var program = new Resource("Tool", @"C:\tool.exe", ResourceKind.File, "Desktop");
        var typed = FallbackClassifier.Classify("notepad", _ => false, _ => false)!;
        var hints = new[]
        {
            ResourceActions.Hint(program),
            ResourceActions.KeysHint(program, inRecents: true, isPinned: false),
            ResourceActions.KeysHint(typed, inRecents: false, isPinned: false),
        };

        var launcherKeys = KeyBindings.All.Where(binding => binding.Area == KeyBindings.Launcher).Select(binding => binding.Keys).ToList();
        var hintKeys = hints
            .SelectMany(hint => hint.Split("  ·  "))
            .Select(part => part[..part.IndexOf(':')])
            .Distinct()
            .ToList();

        Assert.Equal(9, hintKeys.Count);
        Assert.All(hintKeys, key => Assert.Contains(key, launcherKeys));
    }
}
