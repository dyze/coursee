using System.Text.Json;

namespace Coursee.Core.Updates;

/// <summary>A published release: its version (the tag, <c>0.8</c> or <c>v0.8</c>) and its web page.</summary>
public sealed record Release(string Version, string Page)
{
    /// <summary>
    /// Reads GitHub's answer for a release (<c>tag_name</c>, <c>html_url</c>). Null when one of
    /// them is missing or the text is not JSON.
    /// </summary>
    public static Release? FromGitHubJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("tag_name", out var tag) && tag.ValueKind == JsonValueKind.String
                && root.TryGetProperty("html_url", out var page) && page.ValueKind == JsonValueKind.String
                ? new Release(tag.GetString()!, page.GetString()!)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// True when <paramref name="candidate"/> is a later version than <paramref name="current"/>
    /// (both <c>MAJOR.MINOR</c>, a leading <c>v</c> allowed). False when either cannot be read.
    /// </summary>
    public static bool IsNewer(string candidate, string current) =>
        TryParse(candidate, out var later) && TryParse(current, out var now) && later > now;

    private static bool TryParse(string text, out System.Version version)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }

        return System.Version.TryParse(trimmed, out version!);
    }
}

/// <summary>Where the latest release is asked (GitHub, in App).</summary>
public interface IReleaseFeed
{
    /// <summary>The latest release, or null when there is none. Throws on a network or server failure.</summary>
    Task<Release?> LatestAsync(CancellationToken cancellation);
}
