using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Coursee.UI.Platform;

/// <summary>
/// The icon Explorer shows for a file, folder, shortcut or Shell item (<c>shell:AppsFolder\…</c>),
/// read through <c>IShellItemImageFactory</c>. The Shell needs an STA thread: every read runs on one
/// of its own, in turn, so a slow drive never holds the window.
/// </summary>
public sealed class ShellIcons : IDisposable
{
    private readonly BlockingCollection<Request> _requests = [];

    public ShellIcons()
    {
        var thread = new Thread(Serve) { IsBackground = true, Name = "Shell icons" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    /// <summary>The icon of <paramref name="path"/>, <paramref name="size"/> pixels square (frozen); null when the Shell has none.</summary>
    public Task<BitmapSource?> LoadAsync(string path, int size)
    {
        var request = new Request(path, size, new TaskCompletionSource<BitmapSource?>(TaskCreationOptions.RunContinuationsAsynchronously));
        if (!_requests.TryAdd(request))
        {
            request.Done.SetResult(null);
        }

        return request.Done.Task;
    }

    public void Dispose() => _requests.CompleteAdding();

    private sealed record Request(string Path, int Size, TaskCompletionSource<BitmapSource?> Done);

    private void Serve()
    {
        foreach (var request in _requests.GetConsumingEnumerable())
        {
            try
            {
                request.Done.SetResult(Load(request.Path, request.Size));
            }
            catch (Exception ex)
            {
                request.Done.SetException(ex);
            }
        }
    }

    private static BitmapSource? Load(string path, int size)
    {
        var factoryId = typeof(IShellItemImageFactory).GUID;
        if (SHCreateItemFromParsingName(path, IntPtr.Zero, ref factoryId, out var factory) != 0 || factory is null)
        {
            return null;
        }

        try
        {
            if (factory.GetImage(new NativeSize(size, size), IconOnly | BiggerSizeOk, out var bitmap) != 0 || bitmap == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var source = ToBitmapSource(bitmap);
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(bitmap);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(factory);
        }
    }

    /// <summary>
    /// A 32-bit DIB with its transparency (<c>CreateBitmapSourceFromHBitmap</c> drops it), turned
    /// upright when stored bottom-up. Any other bitmap goes through WPF's own conversion.
    /// </summary>
    private static BitmapSource ToBitmapSource(IntPtr bitmap)
    {
        var dib = default(DibSection);
        if (GetObject(bitmap, Marshal.SizeOf<DibSection>(), ref dib) != 0 && dib.Bits != IntPtr.Zero && dib.BitsPixel == 32)
        {
            var source = BitmapSource.Create(
                dib.Width,
                dib.Height,
                96,
                96,
                PixelFormats.Pbgra32,
                null,
                dib.Bits,
                dib.WidthBytes * dib.Height,
                dib.WidthBytes);
            return dib.HeaderHeight > 0 ? new TransformedBitmap(source, new ScaleTransform(1, -1)) : source;
        }

        return Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
    }

    private const int BiggerSizeOk = 0x1;
    private const int IconOnly = 0x4;

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, int flags, out IntPtr bitmap);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeSize(int Width, int Height);

    /// <summary>DIBSECTION: its BITMAP, then the height of its BITMAPINFOHEADER (negative: stored top-down).</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DibSection
    {
        public int Type;
        public int Width;
        public int Height;
        public int WidthBytes;
        public ushort Planes;
        public ushort BitsPixel;
        public IntPtr Bits;
        public uint HeaderSize;
        public int HeaderWidth;
        public int HeaderHeight;
        public ushort HeaderPlanes;
        public ushort HeaderBitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
        public uint RedMask;
        public uint GreenMask;
        public uint BlueMask;
        public IntPtr Section;
        public uint Offset;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string path,
        IntPtr bindContext,
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory? factory);

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr handle, int size, ref DibSection dib);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr handle);
}
