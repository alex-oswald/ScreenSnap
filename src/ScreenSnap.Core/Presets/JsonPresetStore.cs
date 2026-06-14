using System.Text.Json;
using ScreenSnap.Core.Abstractions;

namespace ScreenSnap.Core.Presets;

/// <summary>
/// JSON-backed <see cref="IPresetStore"/>. Writes are atomic (temp file + replace) and a
/// missing or corrupt file is treated as "no presets yet" rather than an error so the app
/// always starts cleanly.
/// </summary>
public sealed class JsonPresetStore : IPresetStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IStorageLocations _locations;

    public JsonPresetStore(IStorageLocations locations)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    /// <inheritdoc />
    public PresetsDocument Load()
    {
        string path = _locations.PresetsFilePath;
        if (!File.Exists(path))
            return new PresetsDocument();

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new PresetsDocument();

            return JsonSerializer.Deserialize<PresetsDocument>(json, SerializerOptions) ?? new PresetsDocument();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable file: start fresh rather than crashing the app.
            return new PresetsDocument();
        }
    }

    /// <inheritdoc />
    public void Save(PresetsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Directory.CreateDirectory(_locations.AppDataDirectory);

        string path = _locations.PresetsFilePath;
        string tempPath = path + ".tmp";

        string json = JsonSerializer.Serialize(document, SerializerOptions);
        File.WriteAllText(tempPath, json);

        // Atomic replace so a crash mid-write cannot corrupt the existing file.
        if (File.Exists(path))
            File.Replace(tempPath, path, destinationBackupFileName: null);
        else
            File.Move(tempPath, path);
    }
}
