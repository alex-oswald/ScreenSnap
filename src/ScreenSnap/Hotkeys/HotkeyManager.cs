using ScreenSnap.Core.Settings;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace ScreenSnap.Hotkeys;

/// <summary>
/// Registers global preset-cycling hotkeys on a window handle via <c>RegisterHotKey</c> and
/// routes <c>WM_HOTKEY</c> ids back to actions. The default chord is Ctrl+Alt + <c>+</c>/<c>-</c>
/// (both the main-row and numpad keys), with optional Ctrl+Alt+1..9 jump-to-preset bindings.
/// </summary>
internal sealed class HotkeyManager : IDisposable
{
    // Virtual-key codes (stable values).
    private const uint VK_OEM_PLUS = 0xBB;
    private const uint VK_OEM_MINUS = 0xBD;
    private const uint VK_ADD = 0x6B;
    private const uint VK_SUBTRACT = 0x6D;

    private readonly HWND _hwnd;
    private readonly Dictionary<int, Action> _actions = new();
    private readonly List<int> _registeredIds = new();
    private int _nextId = 1;

    public HotkeyManager(HWND hwnd)
    {
        _hwnd = hwnd;
    }

    /// <summary>True when at least one hotkey is currently registered.</summary>
    public bool HasRegistrations => _registeredIds.Count > 0;

    /// <summary>
    /// (Re)registers hotkeys to match <paramref name="settings"/>. Existing registrations are
    /// cleared first. Returns false if hotkeys were enabled but none could be registered
    /// (typically because another app already owns the chord).
    /// </summary>
    public bool Configure(AppSettings settings, Action onNext, Action onPrevious, Action<int> onJump)
    {
        UnregisterAll();

        if (!settings.HotkeysEnabled)
            return true;

        HOT_KEY_MODIFIERS modifiers = ToWin32(settings.Modifiers) | HOT_KEY_MODIFIERS.MOD_NOREPEAT;

        // "+" cycles to the next preset; "-" to the previous one.
        Register(modifiers, VK_OEM_PLUS, onNext);
        Register(modifiers, VK_ADD, onNext);
        Register(modifiers, VK_OEM_MINUS, onPrevious);
        Register(modifiers, VK_SUBTRACT, onPrevious);

        if (settings.EnableJumpHotkeys)
        {
            for (int n = 1; n <= 9; n++)
            {
                int presetIndex = n - 1;
                Register(modifiers, (uint)(0x30 + n), () => onJump(presetIndex)); // VK_1..VK_9
                Register(modifiers, (uint)(0x60 + n), () => onJump(presetIndex)); // VK_NUMPAD1..VK_NUMPAD9
            }
        }

        return HasRegistrations;
    }

    /// <summary>Handles a <c>WM_HOTKEY</c> id, returning true if it mapped to an action.</summary>
    public bool Handle(int hotkeyId)
    {
        if (_actions.TryGetValue(hotkeyId, out var action))
        {
            action();
            return true;
        }

        return false;
    }

    private void Register(HOT_KEY_MODIFIERS modifiers, uint virtualKey, Action action)
    {
        int id = _nextId++;
        if (PInvoke.RegisterHotKey(_hwnd, id, modifiers, virtualKey))
        {
            _registeredIds.Add(id);
            _actions[id] = action;
        }
    }

    private void UnregisterAll()
    {
        foreach (int id in _registeredIds)
            PInvoke.UnregisterHotKey(_hwnd, id);

        _registeredIds.Clear();
        _actions.Clear();
        _nextId = 1;
    }

    public void Dispose() => UnregisterAll();

    private static HOT_KEY_MODIFIERS ToWin32(HotkeyModifiers modifiers)
    {
        HOT_KEY_MODIFIERS result = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            result |= HOT_KEY_MODIFIERS.MOD_ALT;
        if (modifiers.HasFlag(HotkeyModifiers.Control))
            result |= HOT_KEY_MODIFIERS.MOD_CONTROL;
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            result |= HOT_KEY_MODIFIERS.MOD_SHIFT;
        if (modifiers.HasFlag(HotkeyModifiers.Win))
            result |= HOT_KEY_MODIFIERS.MOD_WIN;
        return result;
    }
}
