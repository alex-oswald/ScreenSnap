namespace ScreenSnap.Core.Displays;

/// <summary>
/// The desired state of a single monitor within a preset. This type is serialized
/// to disk, so it is intentionally a mutable plain-old-data class.
/// </summary>
public sealed class MonitorState
{
    /// <summary>Stable per-monitor identity (the CCD <c>monitorDevicePath</c>).</summary>
    public string DevicePath { get; set; } = string.Empty;

    /// <summary>Human friendly monitor name, persisted for display in the UI.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Whether the monitor should be enabled (part of the desktop) for this preset.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether the monitor should be the primary display for this preset.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Desired X position of the monitor's top-left corner in desktop coordinates.</summary>
    public int X { get; set; }

    /// <summary>Desired Y position of the monitor's top-left corner in desktop coordinates.</summary>
    public int Y { get; set; }

    /// <summary>Desired horizontal resolution in pixels (0 = use the monitor's current/preferred mode).</summary>
    public uint Width { get; set; }

    /// <summary>Desired vertical resolution in pixels (0 = use the monitor's current/preferred mode).</summary>
    public uint Height { get; set; }
}
