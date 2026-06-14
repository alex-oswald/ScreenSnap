using ScreenSnap.Core.Displays;

namespace ScreenSnap.Core.Presets;

/// <summary>
/// In-memory owner of the user's presets. Coordinates persistence (<see cref="IPresetStore"/>)
/// and application (<see cref="IDisplayService"/>), and tracks which preset is currently active
/// so the tray menu and hotkeys can cycle through them.
/// </summary>
public sealed class PresetManager
{
    private readonly IPresetStore _store;
    private readonly IDisplayService _display;
    private PresetsDocument _document;
    private int _activeIndex = -1;

    public PresetManager(IPresetStore store, IDisplayService display)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _document = _store.Load();
    }

    /// <summary>Raised whenever the set of presets changes (add/remove/reorder/edit/reload).</summary>
    public event EventHandler? PresetsChanged;

    /// <summary>The display engine, exposed for UI that needs to enumerate monitors.</summary>
    public IDisplayService Display => _display;

    /// <summary>The current presets, in tray-menu order.</summary>
    public IReadOnlyList<Preset> Presets => _document.Presets;

    /// <summary>Index of the most recently applied preset, or -1 if none.</summary>
    public int ActiveIndex => _activeIndex;

    /// <summary>The most recently applied preset, or null if none.</summary>
    public Preset? ActivePreset => _activeIndex >= 0 && _activeIndex < _document.Presets.Count
        ? _document.Presets[_activeIndex]
        : null;

    /// <summary>Applies a specific preset and marks it active on success.</summary>
    public DisplayApplyResult Apply(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var result = _display.Apply(preset.Configuration);
        if (result.Success)
            _activeIndex = _document.Presets.IndexOf(preset);

        return result;
    }

    /// <summary>Applies the preset with the given id, or returns null if no such preset exists.</summary>
    public DisplayApplyResult? ApplyById(string id)
    {
        var preset = _document.Presets.FirstOrDefault(p => p.Id == id);
        return preset is null ? null : Apply(preset);
    }

    /// <summary>Applies the next preset in the list, wrapping around. Null if there are no presets.</summary>
    public DisplayApplyResult? ApplyNext() => Cycle(1);

    /// <summary>Applies the previous preset in the list, wrapping around. Null if there are no presets.</summary>
    public DisplayApplyResult? ApplyPrevious() => Cycle(-1);

    private DisplayApplyResult? Cycle(int direction)
    {
        int count = _document.Presets.Count;
        if (count == 0)
            return null;

        int next = _activeIndex < 0
            ? (direction > 0 ? 0 : count - 1)
            : (((_activeIndex + direction) % count) + count) % count;

        return Apply(_document.Presets[next]);
    }

    /// <summary>Captures the current desktop layout as a new preset and persists it.</summary>
    public Preset CaptureNew(string name)
    {
        var preset = new Preset
        {
            Name = string.IsNullOrWhiteSpace(name) ? "New preset" : name.Trim(),
            Configuration = _display.CaptureCurrent(),
        };

        _document.Presets.Add(preset);
        Persist();
        return preset;
    }

    /// <summary>Adds an already-built preset and persists.</summary>
    public void Add(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        _document.Presets.Add(preset);
        Persist();
    }

    /// <summary>Removes a preset and persists.</summary>
    public void Remove(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (_document.Presets.Remove(preset))
        {
            if (_activeIndex >= _document.Presets.Count)
                _activeIndex = -1;
            Persist();
        }
    }

    /// <summary>Moves a preset from one position to another and persists.</summary>
    public void Move(int oldIndex, int newIndex)
    {
        var presets = _document.Presets;
        if (oldIndex < 0 || oldIndex >= presets.Count || newIndex < 0 || newIndex >= presets.Count || oldIndex == newIndex)
            return;

        var preset = presets[oldIndex];
        presets.RemoveAt(oldIndex);
        presets.Insert(newIndex, preset);
        _activeIndex = -1;
        Persist();
    }

    /// <summary>Persists the current document and raises <see cref="PresetsChanged"/>.</summary>
    public void Persist()
    {
        _store.Save(_document);
        PresetsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Reloads presets from disk, discarding unsaved in-memory changes.</summary>
    public void Reload()
    {
        _document = _store.Load();
        _activeIndex = -1;
        PresetsChanged?.Invoke(this, EventArgs.Empty);
    }
}
