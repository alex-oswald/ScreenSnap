using ScreenSnap.Core.Displays;

namespace ScreenSnap.Core.Tests;

/// <summary>
/// Smoke tests that exercise the real CCD interop on the host machine. These assert only
/// invariants that hold for any topology so they remain stable regardless of how many
/// displays are attached to the test agent.
/// </summary>
public class CcdDisplayServiceTests
{
    private readonly IDisplayService _service = new CcdDisplayService();

    [Fact]
    public void GetMonitors_DoesNotThrow_AndReturnsConsistentData()
    {
        var monitors = _service.GetMonitors();

        Assert.NotNull(monitors);

        foreach (var monitor in monitors)
        {
            Assert.False(string.IsNullOrWhiteSpace(monitor.DevicePath));
            Assert.False(string.IsNullOrWhiteSpace(monitor.FriendlyName));
        }

        // Device paths uniquely identify a monitor, so there should be no duplicates.
        var distinctPaths = monitors.Select(m => m.DevicePath).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Assert.Equal(monitors.Count, distinctPaths);

        // At most one monitor can be primary at a time.
        Assert.True(monitors.Count(m => m.IsPrimary) <= 1);
    }

    [Fact]
    public void CaptureCurrent_MatchesEnumeratedMonitors()
    {
        var monitors = _service.GetMonitors();
        var configuration = _service.CaptureCurrent();

        Assert.NotNull(configuration);
        Assert.Equal(monitors.Count, configuration.Monitors.Count);

        var enumeratedPaths = monitors.Select(m => m.DevicePath).OrderBy(p => p, StringComparer.Ordinal);
        var capturedPaths = configuration.Monitors.Select(m => m.DevicePath).OrderBy(p => p, StringComparer.Ordinal);
        Assert.Equal(enumeratedPaths, capturedPaths);

        // An active monitor is captured as enabled.
        foreach (var monitor in monitors.Where(m => m.IsActive))
        {
            var captured = configuration.Monitors.Single(m => m.DevicePath == monitor.DevicePath);
            Assert.True(captured.Enabled);
        }
    }
}
