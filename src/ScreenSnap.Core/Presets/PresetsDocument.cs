namespace ScreenSnap.Core.Presets;

/// <summary>
/// Root document persisted to <c>presets.json</c>. Versioned so the on-disk schema can
/// evolve without breaking older files.
/// </summary>
public sealed class PresetsDocument
{
    /// <summary>Schema version of the persisted document.</summary>
    public int Version { get; set; } = 1;

    /// <summary>The ordered list of presets. Order is the order shown in the tray menu.</summary>
    public List<Preset> Presets { get; set; } = new();
}
