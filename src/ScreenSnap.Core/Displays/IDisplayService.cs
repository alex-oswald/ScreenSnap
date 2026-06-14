namespace ScreenSnap.Core.Displays;

/// <summary>
/// Enumerates monitors and applies saved display configurations.
/// </summary>
public interface IDisplayService
{
    /// <summary>Returns every monitor currently attached to the system (active and inactive).</summary>
    IReadOnlyList<MonitorInfo> GetMonitors();

    /// <summary>
    /// Lists the resolutions Windows reports for the given monitor, in native (landscape) pixels,
    /// largest first. Returns an empty list when the monitor's modes can't be enumerated.
    /// </summary>
    IReadOnlyList<DisplayMode> GetAvailableModes(string devicePath);

    /// <summary>Captures the current desktop layout as an editable configuration.</summary>
    DisplayConfiguration CaptureCurrent();

    /// <summary>Applies the supplied configuration to the system.</summary>
    DisplayApplyResult Apply(DisplayConfiguration configuration);
}
