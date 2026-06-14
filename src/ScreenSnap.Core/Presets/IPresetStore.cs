namespace ScreenSnap.Core.Presets;

/// <summary>
/// Loads and persists the user's presets.
/// </summary>
public interface IPresetStore
{
    /// <summary>Reads the presets document from disk, returning an empty document if none exists.</summary>
    PresetsDocument Load();

    /// <summary>Writes the presets document to disk atomically.</summary>
    void Save(PresetsDocument document);
}
