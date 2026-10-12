using System.Text.Json;
using Coursee.Core.Resources;

namespace Coursee.Core.Sources;

/// <summary>
/// Reads the files of a Chromium-based browser (Edge, and Chrome alike): the <c>Bookmarks</c>
/// file of a profile, and the <c>Local State</c> file that names the last used profile.
/// </summary>
public static class ChromiumBookmarks
{
    public const string BookmarksFile = "Bookmarks";
    public const string LocalStateFile = "Local State";
    public const string DefaultProfile = "Default";

    /// <summary>
    /// The bookmarks in a <c>Bookmarks</c> file: every root (favorites bar, other, mobile...),
    /// folders included, in file order. Only links that can be opened are kept, each URL once.
    /// Throws <see cref="JsonException"/> when the text is not JSON.
    /// </summary>
    /// <param name="browser">The browser the links open in (<see cref="Resource.Browser"/>).</param>
    public static IReadOnlyList<Resource> Parse(string json, string sourceName, string? browser = null)
    {
        using var document = JsonDocument.Parse(json);
        var resources = new List<Resource>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("roots", out var roots)
            && roots.ValueKind == JsonValueKind.Object)
        {
            foreach (var root in roots.EnumerateObject())
            {
                Collect(root.Value, sourceName, browser, resources, seen);
            }
        }

        return resources;
    }

    /// <summary>
    /// The folder of the last used profile under <paramref name="userDataDirectory"/>
    /// (for Edge, <c>%LOCALAPPDATA%\Microsoft\Edge\User Data</c>), else the Default profile.
    /// Returns null when that folder does not exist.
    /// </summary>
    public static string? FindProfile(string userDataDirectory)
    {
        var profile = LastUsedProfile(userDataDirectory) ?? DefaultProfile;
        var directory = Path.Combine(userDataDirectory, profile);
        if (Directory.Exists(directory))
        {
            return directory;
        }

        var fallback = Path.Combine(userDataDirectory, DefaultProfile);
        return Directory.Exists(fallback) ? fallback : null;
    }

    /// <summary>The <c>profile.last_used</c> value of a <c>Local State</c> text, or null.</summary>
    public static string? ParseLastUsedProfile(string localStateJson)
    {
        try
        {
            using var document = JsonDocument.Parse(localStateJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("profile", out var profile)
                && profile.ValueKind == JsonValueKind.Object
                && profile.TryGetProperty("last_used", out var lastUsed)
                && lastUsed.ValueKind == JsonValueKind.String
                && IsPlainFolderName(lastUsed.GetString())
                ? lastUsed.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? LastUsedProfile(string userDataDirectory)
    {
        var path = Path.Combine(userDataDirectory, LocalStateFile);
        try
        {
            return File.Exists(path) ? ParseLastUsedProfile(ReadShared(path)) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Reads a file the browser may have open.</summary>
    public static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>A profile is a folder name such as "Default" or "Profile 1", never a path.</summary>
    private static bool IsPlainFolderName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.IndexOfAny(['\\', '/', ':']) < 0
        && name != "."
        && name != "..";

    private static void Collect(JsonElement node, string sourceName, string? browser, List<Resource> resources, HashSet<string> seen)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var type = Text(node, "type");
        if (type == "url")
        {
            var url = Text(node, "url");
            if (BookmarkUrls.IsOpenable(url) && seen.Add(url!))
            {
                var name = Text(node, "name");
                resources.Add(new Resource(string.IsNullOrWhiteSpace(name) ? url! : name, url!, ResourceKind.Url, sourceName, Browser: browser));
            }

            return;
        }

        if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
            {
                Collect(child, sourceName, browser, resources, seen);
            }
        }
    }

    private static string? Text(JsonElement node, string property) =>
        node.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
