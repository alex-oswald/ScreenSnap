namespace ScreenSnap.Core.Settings;

/// <summary>
/// Persisted application settings (hotkey behaviour and autostart). Stored as JSON in
/// <c>settings.json</c>. Defaults give the user a working Ctrl+Alt + +/- cycle out of the box.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Schema version, for forward-compatible migrations.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Whether the global preset-cycling hotkeys are registered.</summary>
    public bool HotkeysEnabled { get; set; } = true;

    /// <summary>Modifier chord held while pressing +/- to cycle presets. Default Ctrl+Alt.</summary>
    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Alt;

    /// <summary>
    /// When true, also registers <c>{modifiers}+1..9</c> to jump directly to the Nth preset.
    /// Off by default so it doesn't claim extra key combinations unexpectedly.
    /// </summary>
    public bool EnableJumpHotkeys { get; set; }

    /// <summary>Whether ScreenSnap launches automatically at sign-in.</summary>
    public bool RunAtStartup { get; set; }
}
