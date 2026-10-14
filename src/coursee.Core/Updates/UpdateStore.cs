using System.Text.Json;

namespace Coursee.Core.Updates;

/// <summary>
/// What the update check found, persisted as JSON: when it last ran, the latest release, and the
/// release the user has seen announced (with the version installed then). Kept apart from the
/// settings: it is state, not a choice of the user. A missing or corrupt file means never checked.
/// </summary>
public sealed class UpdateStore
{
    private readonly string _path;

    public UpdateStore(string path)
    {
        _path = path;
        var data = Load(path);
        LastCheck = data?.LastCheck;
        Latest = data?.LatestVersion is { } version && data.LatestPage is { } page ? new Release(version, page) : null;
        AcknowledgedVersion = data?.AcknowledgedVersion;
        AcknowledgedBy = data?.AcknowledgedBy;
    }

    public DateTimeOffset? LastCheck { get; private set; }

    /// <summary>The latest release found, or null (none, or never checked).</summary>
    public Release? Latest { get; private set; }

    /// <summary>The release the user has seen announced (in Settings > About), or null.</summary>
    public string? AcknowledgedVersion { get; private set; }

    /// <summary>The version installed when <see cref="AcknowledgedVersion"/> was seen.</summary>
    public string? AcknowledgedBy { get; private set; }

    /// <summary>Remembers a check. A failed one keeps the release found before.</summary>
    public void Record(DateTimeOffset time, Release? latest, bool succeeded)
    {
        LastCheck = time;
        if (succeeded)
        {
            Latest = latest;
        }

        Save();
    }

    /// <summary>Remembers that the user has seen <paramref name="releaseVersion"/> announced, while running <paramref name="installedVersion"/>.</summary>
    public void Acknowledge(string releaseVersion, string installedVersion)
    {
        AcknowledgedVersion = releaseVersion;
        AcknowledgedBy = installedVersion;
        Save();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new UpdateData(LastCheck, Latest?.Version, Latest?.Page, AcknowledgedVersion, AcknowledgedBy);
        File.WriteAllText(_path, JsonSerializer.Serialize(data));
    }

    private static UpdateData? Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<UpdateData>(File.ReadAllText(path)) : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal sealed record UpdateData(
        DateTimeOffset? LastCheck,
        string? LatestVersion,
        string? LatestPage,
        string? AcknowledgedVersion = null,
        string? AcknowledgedBy = null);
}
