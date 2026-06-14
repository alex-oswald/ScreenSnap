using System.Collections.Generic;
using System.Linq;
using ScreenSnap.Core.Displays;

namespace ScreenSnap.Settings;

/// <summary>
/// Editable view of a single <see cref="MonitorState"/> within a preset. Edits are buffered
/// and written back to the model on <see cref="Commit"/>.
/// </summary>
internal sealed class MonitorRowViewModel : ObservableObject
{
    private readonly MonitorState _model;
    private bool _enabled;
    private bool _isPrimary;
    private ResolutionChoice _selectedResolution;
    private OrientationChoice _selectedOrientation;

    public MonitorRowViewModel(MonitorState model, IReadOnlyList<DisplayMode> availableModes)
    {
        _model = model;
        FriendlyName = string.IsNullOrWhiteSpace(model.FriendlyName) ? model.DevicePath : model.FriendlyName;
        _enabled = model.Enabled;
        _isPrimary = model.IsPrimary;

        Resolutions = BuildResolutions(model, availableModes);
        _selectedResolution = Resolutions.FirstOrDefault(r => r.Width == model.Width && r.Height == model.Height)
            ?? Resolutions[0];
        _selectedOrientation = Orientations.FirstOrDefault(o => o.Value == model.Orientation)
            ?? Orientations[0];
    }

    /// <summary>Invoked when this monitor becomes primary, so the parent can clear the others.</summary>
    public Action<MonitorRowViewModel>? PrimarySelected { get; set; }

    public string FriendlyName { get; }

    public string DevicePath => _model.DevicePath;

    public IReadOnlyList<ResolutionChoice> Resolutions { get; }

    public IReadOnlyList<OrientationChoice> Orientations { get; } = OrientationChoice.All;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetField(ref _enabled, value) && !value)
                IsPrimary = false;
        }
    }

    public bool IsPrimary
    {
        get => _isPrimary;
        set
        {
            if (SetField(ref _isPrimary, value) && value)
            {
                Enabled = true;
                PrimarySelected?.Invoke(this);
            }
        }
    }

    public ResolutionChoice SelectedResolution
    {
        get => _selectedResolution;
        set => SetField(ref _selectedResolution, value);
    }

    public OrientationChoice SelectedOrientation
    {
        get => _selectedOrientation;
        set => SetField(ref _selectedOrientation, value);
    }

    /// <summary>Writes the buffered values back into the underlying model.</summary>
    public void Commit()
    {
        _model.Enabled = _enabled;
        _model.IsPrimary = _isPrimary;
        _model.Width = _selectedResolution.Width;
        _model.Height = _selectedResolution.Height;
        _model.Orientation = _selectedOrientation.Value;
        // X/Y are left untouched: the desktop arrangement is captured automatically, not edited here.
    }

    private static IReadOnlyList<ResolutionChoice> BuildResolutions(
        MonitorState model, IReadOnlyList<DisplayMode> availableModes)
    {
        var list = new List<ResolutionChoice> { new(0, 0) };
        list.AddRange(availableModes.Where(m => !m.IsUseCurrent).Select(m => new ResolutionChoice(m.Width, m.Height)));

        // Make sure the preset's stored resolution is always selectable, even if the monitor is
        // currently disconnected (so its modes can't be enumerated).
        if (model.Width != 0 && model.Height != 0 &&
            !list.Any(r => r.Width == model.Width && r.Height == model.Height))
        {
            list.Add(new ResolutionChoice(model.Width, model.Height));
        }

        return list;
    }
}
