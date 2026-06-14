using System.Text.Json;
using ScreenSnap.Core.Displays;

namespace ScreenSnap.Core.Tests;

public class DisplayConfigurationSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    [Fact]
    public void DisplayConfiguration_RoundTripsThroughJson()
    {
        var original = new DisplayConfiguration
        {
            Monitors =
            {
                new MonitorState
                {
                    DevicePath = @"\\?\DISPLAY#DEL1234#5&abcdef&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}",
                    FriendlyName = "DELL U2720Q",
                    Enabled = true,
                    IsPrimary = true,
                    Orientation = DisplayOrientation.Portrait,
                    X = 0,
                    Y = 0,
                    Width = 3840,
                    Height = 2160,
                },
                new MonitorState
                {
                    DevicePath = @"\\?\DISPLAY#GSM5678#5&fedcba&0&UID8449#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}",
                    FriendlyName = "LG TV",
                    Enabled = false,
                    IsPrimary = false,
                    Orientation = DisplayOrientation.Landscape,
                    X = 3840,
                    Y = 0,
                    Width = 0,
                    Height = 0,
                },
            },
        };

        string json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DisplayConfiguration>(json, Options);

        Assert.NotNull(restored);
        Assert.Equal(original.Monitors.Count, restored!.Monitors.Count);

        for (int i = 0; i < original.Monitors.Count; i++)
        {
            var expected = original.Monitors[i];
            var actual = restored.Monitors[i];

            Assert.Equal(expected.DevicePath, actual.DevicePath);
            Assert.Equal(expected.FriendlyName, actual.FriendlyName);
            Assert.Equal(expected.Enabled, actual.Enabled);
            Assert.Equal(expected.IsPrimary, actual.IsPrimary);
            Assert.Equal(expected.Orientation, actual.Orientation);
            Assert.Equal(expected.X, actual.X);
            Assert.Equal(expected.Y, actual.Y);
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);
        }
    }

    [Fact]
    public void DisplayConfiguration_DefaultsToEmptyMonitorList()
    {
        var configuration = new DisplayConfiguration();

        Assert.NotNull(configuration.Monitors);
        Assert.Empty(configuration.Monitors);
    }

    [Fact]
    public void MonitorState_DefaultsToLandscapeOrientation()
    {
        Assert.Equal(DisplayOrientation.Landscape, new MonitorState().Orientation);
    }

    [Theory]
    [InlineData(0u, 0u, "Use current resolution")]
    [InlineData(1920u, 1080u, "1920 \u00d7 1080")]
    public void DisplayMode_FormatsForDisplay(uint width, uint height, string expected)
    {
        var mode = new DisplayMode(width, height);

        Assert.Equal(width == 0 || height == 0, mode.IsUseCurrent);
        Assert.Equal(expected, mode.ToString());
    }
}
