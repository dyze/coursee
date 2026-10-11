using System.Text.Json;

namespace Coursee.Core;

/// <summary>
/// User settings persisted as JSON. Only the list of disabled resource sources is stored,
/// so every source is on by default. A missing or corrupt file means defaults.
/// </summary>
public sealed class SettingsStore
{
    private readonly string _path;
    private readonly HashSet<string> _disabledSources;

    public SettingsStore(string path)
    {
        _path = path;
        _disabledSources = Load(path);
    }

    public bool IsSourceEnabled(string sourceName) => !_disabledSources.Contains(sourceName);

    public void SetSourceEnabled(string sourceName, bool enabled)
    {
        var changed = enabled ? _disabledSources.Remove(sourceName) : _disabledSources.Add(sourceName);
        if (changed)
        {
            Save();
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new SettingsData(_disabledSources.Order(StringComparer.OrdinalIgnoreCase).ToList());
        File.WriteAllText(_path, JsonSerializer.Serialize(data));
    }

    private static HashSet<string> Load(string path)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (File.Exists(path))
            {
                var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path));
                foreach (var name in data?.DisabledSources ?? [])
                {
                    result.Add(name);
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            result.Clear();
        }

        return result;
    }

    internal sealed record SettingsData(List<string>? DisabledSources);
}
