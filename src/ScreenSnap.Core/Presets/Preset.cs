using ScreenSnap.Core.Displays;

namespace ScreenSnap.Core.Presets;

/// <summary>
/// A named display configuration the user can switch to from the tray menu or a hotkey.
/// </summary>
public sealed class Preset
{
    /// <summary>Stable identifier, used to reference the preset from hotkeys/settings.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    /// <summary>User facing preset name (e.g. "Desk monitors" or "TV / gaming").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The desktop layout applied when this preset is activated.</summary>
    public DisplayConfiguration Configuration { get; set; } = new();
}
