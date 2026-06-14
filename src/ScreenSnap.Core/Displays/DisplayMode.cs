namespace ScreenSnap.Core.Displays;

/// <summary>
/// A selectable display resolution in native (landscape) pixels, as reported by Windows for a
/// given monitor. <see cref="Width"/> and <see cref="Height"/> of 0 is a sentinel meaning
/// "keep the monitor's current resolution".
/// </summary>
public readonly record struct DisplayMode(uint Width, uint Height)
{
    /// <summary>True for the "keep current resolution" sentinel.</summary>
    public bool IsUseCurrent => Width == 0 || Height == 0;

    public override string ToString() => IsUseCurrent ? "Use current resolution" : $"{Width} \u00d7 {Height}";
}
