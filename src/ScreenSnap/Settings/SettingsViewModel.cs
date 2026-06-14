using System.Collections.ObjectModel;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;

namespace ScreenSnap.Settings;

/// <summary>
/// View model behind the settings window. Wraps the shared <see cref="PresetManager"/>
/// (the single source of truth) so the tray menu and the window stay in sync.
/// </summary>
internal sealed class SettingsViewModel : ObservableObject
{
    private readonly PresetManager _manager;
    private PresetViewModel? _selected;
    private string? _statusMessage;

    public SettingsViewModel(PresetManager manager)
    {
        _manager = manager;
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
}
