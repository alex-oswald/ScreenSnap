using System.Collections.ObjectModel;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;
using ScreenSnap.Core.Settings;

namespace ScreenSnap.Settings;

/// <summary>
/// View model behind the settings window. Wraps the shared <see cref="PresetManager"/>
/// (the single source of truth) so the tray menu and the window stay in sync.
/// </summary>
internal sealed class SettingsViewModel : ObservableObject
{
    private readonly PresetManager _manager;
    private readonly AppSettings _settings;
    private readonly Action _onSettingsChanged;
    private PresetViewModel? _selected;
    private string? _statusMessage;

    public SettingsViewModel(PresetManager manager, AppSettings settings, Action onSettingsChanged)
    {
        _manager = manager;
        _settings = settings;
        _onSettingsChanged = onSettingsChanged;

        Presets = new ObservableCollection<PresetViewModel>();
        foreach (var preset in manager.Presets)
            Presets.Add(new PresetViewModel(preset));

        _selected = Presets.FirstOrDefault();
    }

    public ObservableCollection<PresetViewModel> Presets { get; }

    public PresetViewModel? Selected
    {
        get => _selected;
        set
        {
            if (SetField(ref _selected, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => _selected is not null;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    // --- Hotkey configuration ---------------------------------------------------------

    public bool HotkeysEnabled
    {
        get => _settings.HotkeysEnabled;
        set
        {
            if (_settings.HotkeysEnabled == value)
                return;

            _settings.HotkeysEnabled = value;
            OnPropertyChanged();
            ApplyHotkeys();
        }
    }

    public bool ModControl
    {
        get => _settings.Modifiers.HasFlag(HotkeyModifiers.Control);
        set => SetModifier(HotkeyModifiers.Control, value);
    }

    public bool ModAlt
    {
        get => _settings.Modifiers.HasFlag(HotkeyModifiers.Alt);
        set => SetModifier(HotkeyModifiers.Alt, value);
    }

    public bool ModShift
    {
        get => _settings.Modifiers.HasFlag(HotkeyModifiers.Shift);
        set => SetModifier(HotkeyModifiers.Shift, value);
    }

    public bool ModWin
    {
        get => _settings.Modifiers.HasFlag(HotkeyModifiers.Win);
        set => SetModifier(HotkeyModifiers.Win, value);
    }

    public bool EnableJumpHotkeys
    {
        get => _settings.EnableJumpHotkeys;
        set
        {
            if (_settings.EnableJumpHotkeys == value)
                return;

            _settings.EnableJumpHotkeys = value;
            OnPropertyChanged();
            ApplyHotkeys();
        }
    }

    /// <summary>Human-readable summary of the current chord, e.g. "Hold Ctrl+Alt, then press + …".</summary>
    public string HotkeyHint
    {
        get
        {
            string chord = DescribeModifiers();
            return $"Hold {chord}, then press + for the next preset or − for the previous one.";
        }
    }

    /// <summary>Whether ScreenSnap launches automatically when the user signs in.</summary>
    public bool RunAtStartup
    {
        get => _settings.RunAtStartup;
        set
        {
            if (_settings.RunAtStartup == value)
                return;

            _settings.RunAtStartup = value;
            OnPropertyChanged();
            _onSettingsChanged();
            StatusMessage = value ? "ScreenSnap will start with Windows." : "Startup entry removed.";
        }
    }

    /// <summary>Captures the current desktop layout as a new preset.</summary>
    public void AddFromCurrent()
    {
        var preset = _manager.CaptureNew("New preset");
        var vm = new PresetViewModel(preset);
        Presets.Add(vm);
        Selected = vm;
        StatusMessage = "Captured the current layout as a new preset.";
    }

    /// <summary>Removes the selected preset.</summary>
    public void DeleteSelected()
    {
        if (_selected is null)
            return;

        int index = Presets.IndexOf(_selected);
        _manager.Remove(_selected.Model);
        Presets.Remove(_selected);
        Selected = Presets.Count == 0 ? null : Presets[Math.Min(index, Presets.Count - 1)];
        StatusMessage = "Preset deleted.";
    }

    /// <summary>Moves the selected preset up (-1) or down (+1) in the list.</summary>
    public void MoveSelected(int direction)
    {
        if (_selected is null)
            return;

        int index = Presets.IndexOf(_selected);
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= Presets.Count)
            return;

        _manager.Move(index, newIndex);
        Presets.Move(index, newIndex);
        OnPropertyChanged(nameof(Selected));
    }

    /// <summary>Commits edits to the selected preset and applies it to the system.</summary>
    public DisplayApplyResult? ApplySelected()
    {
        if (_selected is null)
            return null;

        _selected.Commit();
        _manager.Persist();

        var result = _manager.Apply(_selected.Model);
        StatusMessage = result.Success
            ? $"Applied \"{_selected.Model.Name}\"."
            : $"Could not apply: {result.Error}";
        return result;
    }

    /// <summary>Re-captures the current desktop layout into the selected preset.</summary>
    public void RecaptureSelected()
    {
        if (_selected is null)
            return;

        _selected.Model.Configuration = _manager.Display.CaptureCurrent();
        _selected.RebuildMonitors();
        _manager.Persist();
        StatusMessage = "Updated the preset with the current layout.";
    }

    /// <summary>Commits all edits and persists.</summary>
    public void Save()
    {
        foreach (var preset in Presets)
            preset.Commit();

        _manager.Persist();
        StatusMessage = "Saved.";
    }

    private void SetModifier(HotkeyModifiers flag, bool enabled)
    {
        HotkeyModifiers current = _settings.Modifiers;
        HotkeyModifiers updated = enabled ? current | flag : current & ~flag;
        if (updated == current)
            return;

        if (updated == HotkeyModifiers.None)
        {
            // Refuse a modifier-less chord so we never grab a bare +/- globally.
            StatusMessage = "Keep at least one modifier for the hotkey.";
            OnPropertyChanged(nameof(ModControl));
            OnPropertyChanged(nameof(ModAlt));
            OnPropertyChanged(nameof(ModShift));
            OnPropertyChanged(nameof(ModWin));
            return;
        }

        _settings.Modifiers = updated;
        OnPropertyChanged(nameof(ModControl));
        OnPropertyChanged(nameof(ModAlt));
        OnPropertyChanged(nameof(ModShift));
        OnPropertyChanged(nameof(ModWin));
        OnPropertyChanged(nameof(HotkeyHint));
        ApplyHotkeys();
    }

    private void ApplyHotkeys()
    {
        _onSettingsChanged();
        StatusMessage = _settings.HotkeysEnabled
            ? $"Hotkeys enabled ({DescribeModifiers()} + / −)."
            : "Hotkeys disabled.";
    }

    private string DescribeModifiers()
    {
        var parts = new List<string>();
        if (ModControl)
            parts.Add("Ctrl");
        if (ModAlt)
            parts.Add("Alt");
        if (ModShift)
            parts.Add("Shift");
        if (ModWin)
            parts.Add("Win");

        return parts.Count == 0 ? "(no modifier)" : string.Join("+", parts);
    }
}
