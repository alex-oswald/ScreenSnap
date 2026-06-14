namespace ScreenSnap.Core.Displays;

/// <summary>
/// A full desktop layout: the desired state of every monitor that participates
/// in a preset. Serialized as the payload of a saved preset.
/// </summary>
public sealed class DisplayConfiguration
{
    /// <summary>The per-monitor desired states that make up this configuration.</summary>
    public List<MonitorState> Monitors { get; set; } = new();
}
