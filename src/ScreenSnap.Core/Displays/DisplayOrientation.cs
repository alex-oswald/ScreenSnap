namespace ScreenSnap.Core.Displays;

/// <summary>
/// Desired rotation of a monitor, matching the "Display orientation" options in the Windows
/// Settings &gt; System &gt; Display page. Values are ordered so that the two portrait modes are odd,
/// which the display engine uses to decide when to swap width/height for the desktop surface.
/// </summary>
public enum DisplayOrientation
{
    /// <summary>0 degrees (DISPLAYCONFIG_ROTATION_IDENTITY).</summary>
    Landscape = 0,

    /// <summary>90 degrees clockwise (DISPLAYCONFIG_ROTATION_ROTATE90).</summary>
    Portrait = 1,

    /// <summary>180 degrees (DISPLAYCONFIG_ROTATION_ROTATE180).</summary>
    LandscapeFlipped = 2,

    /// <summary>270 degrees clockwise (DISPLAYCONFIG_ROTATION_ROTATE270).</summary>
    PortraitFlipped = 3,
}
