using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Coursee.Core;
using Coursee.Core.Updates;

namespace Coursee.UI.Platform;

/// <summary>
/// Asks GitHub's API for the latest release of coursee: coursee's only network access. No
/// sign-in; nothing is sent but the request itself (with coursee's version as user agent).
/// </summary>
public sealed class GitHubReleaseFeed : IReleaseFeed
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly string _version;

    public GitHubReleaseFeed(string version)
    {
        _version = version;
    }

    public async Task<Release?> LatestAsync(CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ProjectLinks.LatestReleaseApi);

        // GitHub's API needs a user agent.
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("coursee", _version));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await Client.SendAsync(request, cancellation).ConfigureAwait(false);

        // No release published yet (or the repository is not public).
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return Release.FromGitHubJson(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
    }
}
