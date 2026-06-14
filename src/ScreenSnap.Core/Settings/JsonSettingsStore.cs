using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenSnap.Core.Abstractions;

namespace ScreenSnap.Core.Settings;

/// <summary>
/// JSON-backed <see cref="ISettingsStore"/>. Writes are atomic (temp file + replace) and a
/// missing or corrupt file falls back to defaults so the app always starts cleanly.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IStorageLocations _locations;

    public JsonSettingsStore(IStorageLocations locations)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    /// <inheritdoc />
    public AppSettings Load()
    {
        string path = _locations.SettingsFilePath;
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(_locations.AppDataDirectory);

        string path = _locations.SettingsFilePath;
        string tempPath = path + ".tmp";

        string json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
            File.Replace(tempPath, path, destinationBackupFileName: null);
        else
            File.Move(tempPath, path);
    }
}
