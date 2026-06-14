namespace ScreenSnap.Core.Displays;

/// <summary>
/// Enumerates monitors and applies saved display configurations.
/// </summary>
public interface IDisplayService
{
    /// <summary>Returns every monitor currently attached to the system (active and inactive).</summary>
    IReadOnlyList<MonitorInfo> GetMonitors();

    /// <summary>Captures the current desktop layout as an editable configuration.</summary>
    DisplayConfiguration CaptureCurrent();

    /// <summary>Applies the supplied configuration to the system.</summary>
    DisplayApplyResult Apply(DisplayConfiguration configuration);
}
