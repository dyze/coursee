using Microsoft.Data.Sqlite;

namespace Coursee.Core.Sources;

/// <summary>
/// Reads a browser's SQLite file while the browser runs: it keeps the file locked, and its newest
/// changes sit in the <c>-wal</c> file, so both are copied to a temporary folder and read there.
/// </summary>
public static class SqliteSnapshot
{
    /// <summary>What tells that the file or its <c>-wal</c> changed: their times and sizes.</summary>
    public static string Stamp(string database) => StampOf(database) + "|" + StampOf(database + "-wal");

    /// <summary>Copies <paramref name="database"/> (and its <c>-wal</c>), reads the copy, then deletes it.</summary>
    /// <exception cref="SqliteException">The file is not a database, or not the one expected.</exception>
    /// <exception cref="IOException">It cannot be copied.</exception>
    public static T Read<T>(string database, Func<SqliteConnection, T> read)
    {
        var temp = Path.Combine(Path.GetTempPath(), "coursee-sqlite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var copy = Path.Combine(temp, Path.GetFileName(database));
            File.Copy(database, copy);
            if (File.Exists(database + "-wal"))
            {
                File.Copy(database + "-wal", copy + "-wal");
            }

            // Not read-only: SQLite writes the -shm file of a database in WAL mode, even to read it.
            using var connection = new SqliteConnection($"Data Source={copy};Pooling=False");
            connection.Open();
            return read(connection);
        }
        finally
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A leftover temp folder is harmless.
            }
        }
    }

    private static string StampOf(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? $"{info.LastWriteTimeUtc.Ticks}:{info.Length}" : "-";
    }
}
