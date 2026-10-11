namespace Coursee.Core;

/// <summary>
/// A global hotkey such as Ctrl+Alt+Space. The key is stored by name (the WPF key name),
/// so Core stays free of Windows types.
/// </summary>
public sealed record HotkeyGesture(bool Ctrl, bool Alt, bool Shift, bool Win, string Key)
{
    public static HotkeyGesture Default { get; } = new(Ctrl: true, Alt: true, Shift: false, Win: false, Key: "Space");

    /// <summary>A usable hotkey has a key and at least one modifier.</summary>
    public bool IsValid => (Ctrl || Alt || Shift || Win) && !string.IsNullOrWhiteSpace(Key);

    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (Alt)
        {
            parts.Add("Alt");
        }

        if (Shift)
        {
            parts.Add("Shift");
        }

        if (Win)
        {
            parts.Add("Win");
        }

        parts.Add(Key);
        return string.Join('+', parts);
    }

    /// <summary>Parses text such as "Ctrl+Alt+Space". Returns false for anything not a valid hotkey.</summary>
    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = Default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        bool ctrl = false, alt = false, shift = false, win = false;
        string? key = null;

        foreach (var part in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    ctrl = true;
                    break;
                case "alt":
                    alt = true;
                    break;
                case "shift":
                    shift = true;
                    break;
                case "win":
                case "windows":
                    win = true;
                    break;
                default:
                    if (key is not null)
                    {
                        return false;
                    }

                    key = part;
                    break;
            }
        }

        if (key is null)
        {
            return false;
        }

        var parsed = new HotkeyGesture(ctrl, alt, shift, win, key);
        if (!parsed.IsValid)
        {
            return false;
        }

        gesture = parsed;
        return true;
    }
}
