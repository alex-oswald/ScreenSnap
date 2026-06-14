namespace ScreenSnap.Core.Abstractions;

/// <summary>
/// Provides the on-disk locations ScreenSnap uses for persisted data. Abstracted so a
/// future MSIX build can swap the unpackaged <c>%LOCALAPPDATA%</c> paths for the
/// packaged <c>ApplicationData</c> store without touching consumers.
/// </summary>
public interface IStorageLocations
{
    /// <summary>Root directory for ScreenSnap's persisted data. Created on first use.</summary>
    string AppDataDirectory { get; }

    /// <summary>Full path to the JSON file that stores the user's presets.</summary>
    string PresetsFilePath { get; }

    /// <summary>Full path to the JSON file that stores app settings (hotkeys, autostart).</summary>
    string SettingsFilePath { get; }
}
