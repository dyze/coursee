namespace Coursee.Core;

/// <summary>The changelog shown in Settings > About, as plain text.</summary>
public static class Changelog
{
    /// <summary>The text of the file, or a line saying it was not found (a build without it).</summary>
    public static string Read(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Changelog not found: {path}";
        }
    }
}
