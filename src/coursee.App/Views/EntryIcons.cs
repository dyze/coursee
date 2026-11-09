using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;
using Coursee.Core.Entries;
using Coursee.Core.Icons;
using Coursee.Core.Logging;
using Coursee.Core.Resources;
using Coursee.UI.Platform;

namespace Coursee.UI.Views;

/// <summary>
/// The icons of the rows: the one the user chose (one of coursee's, or a file of theirs), else the
/// entry's own as Explorer shows it (<see cref="OwnIcon"/>) or, for a web address, its site's icon
/// (<see cref="SiteIcons"/>), else none (the row keeps its source's
/// icon). Each is read once; an entry's own icon is read in the background and shown when ready.
/// Used on the UI thread.
/// </summary>
public sealed class EntryIcons(EntryStore entries, IconFileStore files, ShellIcons shell, SiteIcons sites, ILog log)
{
    /// <summary>Pixels of an icon read from Windows: a 32-pixel row icon at 200 % scaling.</summary>
    private const int RowPixels = 64;

    /// <summary>Pixels of a program's icon kept as the user's file.</summary>
    private const int KeptPixels = 64;

    private const string LogCategory = "Icons";

    private readonly Dictionary<string, IconVisual?> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<IconVisual?>> _own = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<IconVisual?>> _sites = new(StringComparer.Ordinal);

    /// <summary>The visual of a chosen icon; null when it cannot be drawn (its file gone, say).</summary>
    public IconVisual? OfChoice(EntryIcon icon) =>
        icon.Kind == EntryIconKind.Library ? LibraryIcons.Of(icon.Name) : OfFile(icon.Name);

    /// <summary>Gives <paramref name="row"/> its icon: at once when known, else once read.</summary>
    public void Show(ResultRow row)
    {
        if (entries.IconOf(row.Resource.Target) is { } chosen && OfChoice(chosen) is { } visual)
        {
            row.Icon = visual;
            return;
        }

        var resource = row.Resource;
        var own = OwnIcon.PathOf(resource) is { } path ? Own(path)
            : resource.Kind == ResourceKind.Url ? Site(resource.Target, resource.Browser)
            : null;
        if (own is null)
        {
            return;
        }

        if (own.IsCompletedSuccessfully)
        {
            row.Icon = own.Result;
        }
        else
        {
            ShowWhenRead(row, own);
        }
    }

    /// <summary>
    /// What a file the user picked keeps as its icon: an image as it is, an SVG of plain paths, a
    /// program's icon as a PNG. Null when it cannot be one (the reason is logged).
    /// </summary>
    public async Task<(byte[] Content, string Extension)?> ReadPicked(string path)
    {
        try
        {
            switch (IconFileStore.TypeOf(path))
            {
                case IconFileType.Image:
                    var image = await File.ReadAllBytesAsync(path);
                    return Decode(new MemoryStream(image), RowPixels) is null ? null : (image, Path.GetExtension(path));

                case IconFileType.Svg:
                    var svg = await File.ReadAllTextAsync(path);
                    return SvgIcon.Read(svg) is { } icon && IconVisual.OfSvg(icon, nonZero: true) is not null
                        ? (Encoding.UTF8.GetBytes(svg), ".svg")
                        : null;

                case IconFileType.Program:
                    return await shell.LoadAsync(path, KeptPixels) is { } program ? (Png(program), ".png") : null;

                default:
                    return null;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or COMException or FileFormatException or ArgumentException)
        {
            log.Warn(LogCategory, $"Cannot read {path} as an icon: {ex.Message}");
            return null;
        }
    }

    private async void ShowWhenRead(ResultRow row, Task<IconVisual?> own) => row.Icon = await own;

    /// <summary>A file of the user's, in the icons folder: named by its content, so read once.</summary>
    private IconVisual? OfFile(string name)
    {
        if (_files.TryGetValue(name, out var visual))
        {
            return visual;
        }

        var path = files.PathOf(name);
        try
        {
            visual = IconFileStore.TypeOf(path) switch
            {
                IconFileType.Svg => SvgIcon.Read(File.ReadAllText(path)) is { } icon ? IconVisual.OfSvg(icon, nonZero: true) : null,
                IconFileType.Image => DecodeFile(path),
                _ => null,
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or COMException or FileFormatException)
        {
            log.Warn(LogCategory, $"Cannot read the icon {path}: {ex.Message}");
            visual = null;
        }

        _files[name] = visual;
        return visual;
    }

    /// <summary>An entry's own icon, read once per path: an image file as it is (Steam's icons), anything else through the Shell.</summary>
    private Task<IconVisual?> Own(string path)
    {
        if (!_own.TryGetValue(path, out var own))
        {
            own = ReadOwn(path);
            _own[path] = own;
        }

        return own;
    }

    /// <summary>
    /// A web site's icon, as a browser keeps it (<see cref="SiteIcons"/>), read once per site and
    /// browser. None found is asked again next time: the browser may keep it by then.
    /// </summary>
    private Task<IconVisual?>? Site(string url, string? browser)
    {
        if (SiteIcons.HostOf(url) is not { } host)
        {
            return null;
        }

        var key = $"{browser}|{host}";
        if (_sites.TryGetValue(key, out var site) && !(site.IsCompletedSuccessfully && site.Result is null))
        {
            return site;
        }

        site = ReadSite(url, browser);
        _sites[key] = site;
        return site;
    }

    private async Task<IconVisual?> ReadSite(string url, string? browser)
    {
        try
        {
            return await Task.Run(() => sites.IconFor(url, browser) is { } data && Decode(new MemoryStream(data), RowPixels) is { } image
                ? IconVisual.OfImage(image)
                : null);
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException or COMException)
        {
            log.Debug(LogCategory, $"Cannot decode the site icon of {url}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private async Task<IconVisual?> ReadOwn(string path)
    {
        try
        {
            var image = IconFileStore.TypeOf(path) == IconFileType.Image
                ? await Task.Run(() => DecodeFile(path)?.Image as BitmapSource)
                : await shell.LoadAsync(path, RowPixels);
            return image is null ? null : IconVisual.OfImage(image);
        }
        catch (Exception ex)
        {
            // A file gone, a Shell item without icon: the row keeps its source's icon.
            log.Debug(LogCategory, $"No icon for {path}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static IconVisual? DecodeFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Decode(stream, RowPixels) is { } image ? IconVisual.OfImage(image) : null;
    }

    /// <summary>The frame closest above <paramref name="pixels"/> (an .ico holds several sizes), else the largest; frozen.</summary>
    private static BitmapSource? Decode(Stream stream, int pixels)
    {
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames
            .OrderBy(candidate => candidate.PixelWidth >= pixels ? candidate.PixelWidth : int.MaxValue - candidate.PixelWidth)
            .FirstOrDefault();
        frame?.Freeze();
        return frame;
    }

    private static byte[] Png(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
