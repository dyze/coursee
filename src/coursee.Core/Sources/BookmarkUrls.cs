namespace Coursee.Core.Sources;

/// <summary>Which bookmarked addresses are worth listing, for every browser source.</summary>
public static class BookmarkUrls
{
    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https", "ftp", "file" };

    /// <summary>
    /// A web, FTP or file address. Bookmarklets (<c>javascript:</c>), browser pages
    /// (<c>about:</c>, <c>edge://</c>) and anything malformed are not.
    /// </summary>
    public static bool IsOpenable(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && AllowedSchemes.Contains(uri.Scheme);
}
