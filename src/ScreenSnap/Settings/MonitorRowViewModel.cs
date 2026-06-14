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
    private double _x;
    private double _y;
    private double _width;
    private double _height;

    public MonitorRowViewModel(MonitorState model)
    {
        _model = model;
        FriendlyName = string.IsNullOrWhiteSpace(model.FriendlyName) ? model.DevicePath : model.FriendlyName;
        _enabled = model.Enabled;
        _isPrimary = model.IsPrimary;
        _x = model.X;
        _y = model.Y;
        _width = model.Width;
        _height = model.Height;
    }

    /// <summary>Invoked when this monitor becomes primary, so the parent can clear the others.</summary>
    public Action<MonitorRowViewModel>? PrimarySelected { get; set; }

    public string FriendlyName { get; }

    public string DevicePath => _model.DevicePath;

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

    public double X { get => _x; set => SetField(ref _x, value); }

    public double Y { get => _y; set => SetField(ref _y, value); }

    public double Width { get => _width; set => SetField(ref _width, value); }

    public double Height { get => _height; set => SetField(ref _height, value); }

    /// <summary>Writes the buffered values back into the underlying model.</summary>
    public void Commit()
    {
        _model.Enabled = _enabled;
        _model.IsPrimary = _isPrimary;
        _model.X = ToInt(_x);
        _model.Y = ToInt(_y);
        _model.Width = ToUInt(_width);
        _model.Height = ToUInt(_height);
    }

    private static int ToInt(double value) => double.IsNaN(value) ? 0 : (int)Math.Round(value);

    private static uint ToUInt(double value) => double.IsNaN(value) || value < 0 ? 0u : (uint)Math.Round(value);
}
