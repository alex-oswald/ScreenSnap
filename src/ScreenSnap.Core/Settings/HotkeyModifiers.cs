namespace ScreenSnap.Core.Settings;

/// <summary>
/// Keyboard modifiers for the preset-cycling hotkey. Values match the Win32
/// <c>MOD_*</c> flags so they can be passed straight to <c>RegisterHotKey</c>.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Win = 0x8,
}
