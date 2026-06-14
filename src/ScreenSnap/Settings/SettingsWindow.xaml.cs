using Microsoft.UI.Xaml;
using ScreenSnap.Core.Presets;
using ScreenSnap.Core.Settings;

namespace ScreenSnap.Settings;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    internal SettingsWindow(PresetManager manager, AppSettings settings, Action onHotkeysChanged)
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(manager, settings, onHotkeysChanged);
        RootGrid.DataContext = _viewModel;
    }

    private void OnAddCurrent(object sender, RoutedEventArgs e) => _viewModel.AddFromCurrent();

    private void OnDelete(object sender, RoutedEventArgs e) => _viewModel.DeleteSelected();

    private void OnMoveUp(object sender, RoutedEventArgs e) => _viewModel.MoveSelected(-1);

    private void OnMoveDown(object sender, RoutedEventArgs e) => _viewModel.MoveSelected(1);

    private void OnRecapture(object sender, RoutedEventArgs e) => _viewModel.RecaptureSelected();

    private void OnApply(object sender, RoutedEventArgs e) => _viewModel.ApplySelected();

    private void OnSave(object sender, RoutedEventArgs e) => _viewModel.Save();
}
