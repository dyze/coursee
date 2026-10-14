using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Coursee.Core.Sources;

namespace Coursee.UI.Platform;

/// <summary>
/// Reads Explorer's Quick access through the Shell COM object. The Shell needs an STA thread, so
/// each read runs on its own; the caller (a background refresh) waits for it. Quick access also
/// shows frequent folders: only those with the <c>System.Home.IsPinned</c> property (Windows 11)
/// are pinned ones.
/// </summary>
public sealed class ShellQuickAccessReader : IQuickAccessReader
{
    private const string QuickAccessFolder = "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public IReadOnlyList<PinnedFolder> PinnedFolders()
    {
        IReadOnlyList<PinnedFolder> folders = [];
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                folders = Read();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true,
            Name = "Quick access",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(Timeout))
        {
            throw new TimeoutException($"Quick access did not answer within {Timeout.TotalSeconds} s");
        }

        if (error is not null)
        {
            ExceptionDispatchInfo.Throw(error);
        }

        return folders;
    }

    private static List<PinnedFolder> Read()
    {
        var type = Type.GetTypeFromProgID("Shell.Application")
            ?? throw new InvalidOperationException("The Windows Shell is not available");
        var shell = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("The Windows Shell is not available");
        object? folder = null;
        object? items = null;
        try
        {
            folder = ((dynamic)shell).NameSpace(QuickAccessFolder);
            if (folder is null)
            {
                throw new InvalidOperationException("Quick access is not available");
            }

            items = ((dynamic)folder).Items();

            var folders = new List<PinnedFolder>();
            int count = ((dynamic)items!).Count;
            for (var index = 0; index < count; index++)
            {
                object item = ((dynamic)items).Item(index);
                try
                {
                    if (AsPinnedFolder(item) is { } pinned)
                    {
                        folders.Add(pinned);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(item);
                }
            }

            return folders;
        }
        finally
        {
            foreach (var comObject in new[] { items, folder, shell })
            {
                if (comObject is not null)
                {
                    Marshal.FinalReleaseComObject(comObject);
                }
            }
        }
    }

    /// <summary>A pinned file system folder, with the name Explorer shows; else null. Virtual ones (This PC, Gallery) have no full path.</summary>
    private static PinnedFolder? AsPinnedFolder(object item)
    {
        dynamic shellItem = item;
        bool isFolder = shellItem.IsFolder;
        string? path = shellItem.Path;
        object? pinned = shellItem.ExtendedProperty("System.Home.IsPinned");
        string? name = shellItem.Name;
        return isFolder && pinned is true && path is not null && Path.IsPathFullyQualified(path)
            ? new PinnedFolder(path, string.IsNullOrWhiteSpace(name) ? Path.GetFileName(path) : name)
            : null;
    }
}
