using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Coursee.UI;

/// <summary>Registers the global hotkey (Ctrl+Alt+Space) through a message-only window.</summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0xC0DE;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkSpace = 0x20;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly HwndSource _source;
    private readonly Action _onPressed;

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

        if (!RegisterHotKey(_source.Handle, HotkeyId, ModControl | ModAlt | ModNoRepeat, VkSpace))
        {
            _source.Dispose();
            throw new InvalidOperationException("Could not register the hotkey Ctrl+Alt+Space (already in use?).");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _onPressed();
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
