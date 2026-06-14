namespace ScreenSnap.Core.Displays;

/// <summary>
/// A read-only snapshot of a monitor that is currently attached to the system
/// (whether or not it is actively part of the desktop).
/// </summary>
public sealed record MonitorInfo
{
    /// <summary>Stable per-monitor identity (the CCD <c>monitorDevicePath</c>).</summary>
    public required string DevicePath { get; init; }

    /// <summary>Human friendly monitor name reported by the OS.</summary>
    public required string FriendlyName { get; init; }

    /// <summary>True when the monitor is part of the active desktop.</summary>
    public bool IsActive { get; init; }

    /// <summary>True when the monitor is the primary display (positioned at the desktop origin).</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Current orientation/rotation of the monitor.</summary>
    public DisplayOrientation Orientation { get; init; }

    /// <summary>X position of the monitor's top-left corner in desktop coordinates.</summary>
    public int X { get; init; }

    /// <summary>Y position of the monitor's top-left corner in desktop coordinates.</summary>
    public int Y { get; init; }

    /// <summary>Active native (un-rotated) horizontal resolution in pixels (0 when inactive).</summary>
    public uint Width { get; init; }

    /// <summary>Active native (un-rotated) vertical resolution in pixels (0 when inactive).</summary>
    public uint Height { get; init; }
}
