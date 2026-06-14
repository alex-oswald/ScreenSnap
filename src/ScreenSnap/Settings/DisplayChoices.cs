using System.Collections.Generic;
using ScreenSnap.Core.Displays;

namespace ScreenSnap.Settings;

/// <summary>
/// A selectable resolution shown in the preset editor. <see cref="Width"/>/<see cref="Height"/>
/// of 0 is the "keep current resolution" sentinel. A record (reference type with value equality)
/// so the bound <c>ComboBox.SelectedItem</c> resolves against the items in the list.
/// </summary>
internal sealed record ResolutionChoice(uint Width, uint Height)
{
    public bool IsUseCurrent => Width == 0 || Height == 0;

    public override string ToString() => IsUseCurrent ? "Use current resolution" : $"{Width} \u00d7 {Height}";
}

/// <summary>
/// A selectable display orientation shown in the preset editor, with the same labels Windows uses
/// in Settings &gt; System &gt; Display &gt; "Display orientation".
/// </summary>
internal sealed record OrientationChoice(DisplayOrientation Value, string Label)
{
    public override string ToString() => Label;

    public static readonly IReadOnlyList<OrientationChoice> All = new[]
    {
        new OrientationChoice(DisplayOrientation.Landscape, "Landscape"),
        new OrientationChoice(DisplayOrientation.Portrait, "Portrait"),
        new OrientationChoice(DisplayOrientation.LandscapeFlipped, "Landscape (flipped)"),
        new OrientationChoice(DisplayOrientation.PortraitFlipped, "Portrait (flipped)"),
    };
}
