namespace Coursee.Core.Platform;

/// <summary>Registers the launcher's global hotkey with the operating system.</summary>
public interface IHotkeyRegistrar
{
    /// <summary>While true, presses of the hotkey are ignored (used while capturing a new one).</summary>
    bool Paused { get; set; }

    /// <summary>The registered hotkey, or null when none could be registered.</summary>
    HotkeyGesture? Current { get; }

    /// <summary>
    /// Makes <paramref name="gesture"/> the active hotkey. Returns false, keeping the
    /// previous hotkey, when the shortcut cannot be registered (already in use, unknown key).
    /// </summary>
    bool TryRegister(HotkeyGesture gesture);
}
