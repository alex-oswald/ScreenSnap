using System.Collections.ObjectModel;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;

namespace ScreenSnap.Settings;

/// <summary>Editable view of a <see cref="Preset"/> and its monitors.</summary>
internal sealed class PresetViewModel : ObservableObject
{
    private readonly IDisplayService _display;
    private string _name;

    public PresetViewModel(Preset model, IDisplayService display)
    {
        Model = model;
        _display = display;
        _name = model.Name;
        Monitors = new ObservableCollection<MonitorRowViewModel>();
        RebuildMonitors();
    }

    public Preset Model { get; }

    public ObservableCollection<MonitorRowViewModel> Monitors { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    /// <summary>Name shown in the presets list, with a fallback for blank names.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(_name) ? "(unnamed preset)" : _name;

    /// <summary>Rebuilds the monitor rows from the model (e.g. after re-capturing the layout).</summary>
    public void RebuildMonitors()
    {
        Monitors.Clear();
        foreach (var monitor in Model.Configuration.Monitors)
        {
            var modes = _display.GetAvailableModes(monitor.DevicePath);
            Monitors.Add(new MonitorRowViewModel(monitor, modes) { PrimarySelected = OnPrimarySelected });
        }
    }

    /// <summary>Writes buffered edits (name + monitors) back into the model.</summary>
    public void Commit()
    {
        Model.Name = string.IsNullOrWhiteSpace(_name) ? "Untitled preset" : _name.Trim();
        foreach (var monitor in Monitors)
            monitor.Commit();
    }

    private void OnPrimarySelected(MonitorRowViewModel chosen)
    {
        foreach (var monitor in Monitors)
        {
            if (!ReferenceEquals(monitor, chosen))
                monitor.IsPrimary = false;
        }
    }
}
