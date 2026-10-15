using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Coursee.UI.Platform;

/// <summary>
/// Lists a Shell folder (Quick access, the Apps folder) through the Shell COM object. The Shell
/// needs an STA thread, so each read runs on its own; the caller (a background refresh) waits for
/// it, up to a timeout. Every COM object is released before the thread ends.
/// </summary>
public static class ShellFolder
{
    /// <param name="folder">A Shell path, such as <c>shell:AppsFolder</c>.</param>
    /// <param name="name">What the folder is, for the thread and the errors ("Quick access").</param>
    /// <param name="select">What to keep of an item (a <c>FolderItem</c>, read with <c>dynamic</c>); null skips it.</param>
    public static IReadOnlyList<T> Items<T>(string folder, string name, TimeSpan timeout, Func<object, T?> select)
        where T : class
    {
        IReadOnlyList<T> items = [];
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                items = Read(folder, name, select);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true,
            Name = name,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(timeout))
        {
            throw new TimeoutException($"{name} did not answer within {timeout.TotalSeconds} s");
        }

        if (error is not null)
        {
            ExceptionDispatchInfo.Throw(error);
        }

        return items;
    }

    private static List<T> Read<T>(string folderPath, string name, Func<object, T?> select)
        where T : class
    {
        var type = Type.GetTypeFromProgID("Shell.Application")
            ?? throw new InvalidOperationException("The Windows Shell is not available");
        var shell = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("The Windows Shell is not available");
        object? folder = null;
        object? items = null;
        try
        {
            folder = ((dynamic)shell).NameSpace(folderPath);
            if (folder is null)
            {
                throw new InvalidOperationException($"{name} is not available");
            }

            items = ((dynamic)folder).Items();

            var selected = new List<T>();
            int count = ((dynamic)items!).Count;
            for (var index = 0; index < count; index++)
            {
                object item = ((dynamic)items).Item(index);
                try
                {
                    if (select(item) is { } kept)
                    {
                        selected.Add(kept);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(item);
                }
            }

            return selected;
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
}
