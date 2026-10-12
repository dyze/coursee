using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using Coursee.Core.Platform;

namespace Coursee.UI.Platform;

/// <summary>Registers the global hotkey through a message-only window. The hotkey can be changed at runtime.</summary>
public sealed class HotkeyService : IHotkeyRegistrar, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0xC0DE;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly HwndSource _source;
    private readonly Action _onPressed;
    private HotkeyGesture? _current;

    public HotkeyService(Action onPressed)
    {
        _onPressed = onPressed;

        _source = new HwndSource(new HwndSourceParameters("coursee-hotkey")
        {
            ParentWindow = HwndMessage,
            Width = 0,
            Height = 0,
        });
        _source.AddHook(WndProc);
    }

    public bool Paused { get; set; }

    public bool TryRegister(HotkeyGesture gesture)
    {
        var previous = _current;
        if (previous is not null)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
            _current = null;
        }

        if (Register(gesture))
        {
            _current = gesture;
            return true;
        }

        if (previous is not null && Register(previous))
        {
            _current = previous;
        }

        return false;
    }

    private bool Register(HotkeyGesture gesture)
    {
        if (!gesture.IsValid
            || !char.IsLetter(gesture.Key[0])
            || !Enum.TryParse<Key>(gesture.Key, ignoreCase: true, out var key)
            || !Enum.IsDefined(key))
        {
            return false;
        }

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey == 0)
        {
            return false;
        }

        var modifiers = ModNoRepeat;
        if (gesture.Ctrl)
        {
            modifiers |= ModControl;
        }

        if (gesture.Alt)
        {
            modifiers |= ModAlt;
        }

        if (gesture.Shift)
        {
            modifiers |= ModShift;
        }

        if (gesture.Win)
        {
            modifiers |= ModWin;
        }

        return RegisterHotKey(_source.Handle, HotkeyId, modifiers, virtualKey);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            if (!Paused)
            {
                _onPressed();
            }

            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterHotKey(_source.Handle, HotkeyId);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
