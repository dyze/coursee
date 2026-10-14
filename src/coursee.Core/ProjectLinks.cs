namespace Coursee.Core;

/// <summary>The project's web pages, shown in Settings > About, and the address the update check asks.</summary>
public static class ProjectLinks
{
    public const string Repository = "https://github.com/dyze/coursee";

    public const string Issues = Repository + "/issues";

    public const string Releases = Repository + "/releases";

    /// <summary>GitHub's API for the latest release (not a pre-release, not a draft).</summary>
    public const string LatestReleaseApi = "https://api.github.com/repos/dyze/coursee/releases/latest";
}
