namespace Coursee.Core;

/// <summary>
/// Decides what Enter does with text that matches no resource, like the Windows Run dialog:
/// open a URL, open an existing folder or file, or run a command.
/// </summary>
public static class FallbackClassifier
{
    /// <summary>The <see cref="Resource.Source"/> given to resources built from typed text.</summary>
    public const string SourceName = "Typed";

    /// <summary>
    /// Classifies <paramref name="input"/>. Returns null for empty text. The existence checks
    /// can be replaced (tests); by default the real file system is used.
    /// </summary>
    public static Resource? Classify(
        string? input,
        Func<string, bool>? directoryExists = null,
        Func<string, bool>? fileExists = null)
    {
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var url = AsUrl(text);
        if (url is not null)
        {
            return new Resource(text, url, ResourceKind.Url, SourceName);
        }

        var path = ExpandedPath(text);
        if (LooksLikePath(path))
        {
            directoryExists ??= Directory.Exists;
            fileExists ??= File.Exists;

            if (directoryExists(path))
            {
                return new Resource(text, path, ResourceKind.Folder, SourceName);
            }

            if (fileExists(path))
            {
                return new Resource(text, path, ResourceKind.File, SourceName);
            }
        }

        return new Resource(text, text, ResourceKind.Command, SourceName);
    }

    /// <summary>A URL with a scheme (http, https, ftp) or starting with "www.". Returns the URL to open, or null.</summary>
    private static string? AsUrl(string text)
    {
        if (text.Any(char.IsWhiteSpace))
        {
            return null;
        }

        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && text.Length > "www.".Length)
        {
            return "https://" + text;
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "ftp"
            ? text
            : null;
    }

    private static string ExpandedPath(string text)
    {
        var unquoted = text.Length >= 2 && text[0] == '"' && text[^1] == '"'
            ? text[1..^1]
            : text;

        return Environment.ExpandEnvironmentVariables(unquoted);
    }

    private static bool LooksLikePath(string path) =>
        Path.IsPathRooted(path) || path.Contains('\\') || path.Contains('/');
}
