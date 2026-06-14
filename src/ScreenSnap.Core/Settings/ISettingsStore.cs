namespace ScreenSnap.Core.Settings;

/// <summary>Loads and persists <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>Reads settings from disk, returning defaults when none exist or the file is corrupt.</summary>
    AppSettings Load();

    /// <summary>Writes settings to disk atomically.</summary>
    void Save(AppSettings settings);
}
