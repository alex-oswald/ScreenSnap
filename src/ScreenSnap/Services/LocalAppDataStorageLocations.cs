using ScreenSnap.Core.Abstractions;

namespace ScreenSnap.Services;

/// <summary>
/// Unpackaged storage locations rooted at <c>%LOCALAPPDATA%\ScreenSnap</c>.
/// A future MSIX build can swap this for an <c>ApplicationData</c>-based implementation.
/// </summary>
internal sealed class LocalAppDataStorageLocations : IStorageLocations
{
    public LocalAppDataStorageLocations()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenSnap");
        Directory.CreateDirectory(root);

        AppDataDirectory = root;
        PresetsFilePath = Path.Combine(root, "presets.json");
        SettingsFilePath = Path.Combine(root, "settings.json");
    }

    public string AppDataDirectory { get; }

    public string PresetsFilePath { get; }

    public string SettingsFilePath { get; }
}
