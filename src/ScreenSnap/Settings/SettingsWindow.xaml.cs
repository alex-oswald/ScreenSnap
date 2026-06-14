using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenSnap.Core.Presets;
using ScreenSnap.Core.Settings;

namespace ScreenSnap.Settings;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private bool _allowClose;

    internal SettingsWindow(PresetManager manager, AppSettings settings, Action onSettingsChanged)
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(manager, settings, onSettingsChanged);
        RootGrid.DataContext = _viewModel;

        Title = "ScreenSnap";

        ConfigureTitleBar();
        ConfigureSystemBackdrop();

        // X / Alt-F4 should leave ScreenSnap running in the tray, not exit the app.
        AppWindow.Closing += OnAppWindowClosing;
    }

    /// <summary>
    /// Tear the window down for real. Used by the tray "Exit" command so that the
    /// AppWindow.Closing handler doesn't cancel the shutdown.
    /// </summary>
    internal void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    private void ConfigureTitleBar()
    {
        // Extend the app canvas under the system title bar so we can paint a Mica
        // background end-to-end and present a custom caption row.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppTitleBar.SizeChanged += (_, _) => UpdateTitleBarPaddingColumns();
        AppTitleBar.Loaded += (_, _) => UpdateTitleBarPaddingColumns();
        AppWindow.Changed += OnAppWindowChanged;

        // Load the window icon for both the system caption (SetIcon) and the title-bar
        // ImageIcon. The .ico is copied next to the executable as Content.
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ScreenSnap.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
            AppTitleBarIcon.Source = new BitmapImage(new Uri(iconPath));
        }
    }

    private void ConfigureSystemBackdrop()
    {
        // Mica gives the window the same long-lived "settings"-style backdrop as
        // first-party Windows 11 apps. On older Windows the property setter is a
        // graceful no-op and the window falls back to the system default chrome.
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        // RightInset / LeftInset can change when the DPI or caption-button layout
        // changes (e.g. maximize on a different monitor); keep the padding in sync.
        if (args.DidPresenterChange || args.DidSizeChange)
            UpdateTitleBarPaddingColumns();
    }

    private void UpdateTitleBarPaddingColumns()
    {
        if (!ExtendsContentIntoTitleBar)
            return;

        var xamlRoot = AppTitleBar.XamlRoot;
        if (xamlRoot is null)
            return;

        // AppWindow inset values are in physical pixels; convert to DIPs for the Grid.
        double scale = xamlRoot.RasterizationScale;
        if (scale <= 0)
            scale = 1;

        var titleBar = AppWindow.TitleBar;
        LeftPaddingColumn.Width = new GridLength(titleBar.LeftInset / scale);
        RightPaddingColumn.Width = new GridLength(titleBar.RightInset / scale);
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
            return;

        // ScreenSnap lives in the tray; closing the window just hides it so a later
        // tray double-click (or the tray "Settings" menu item) can bring it back.
        args.Cancel = true;
        AppWindow.Hide();
    }

    private void OnAddCurrent(object sender, RoutedEventArgs e) => _viewModel.AddFromCurrent();

    private void OnDelete(object sender, RoutedEventArgs e) => _viewModel.DeleteSelected();

    private void OnMoveUp(object sender, RoutedEventArgs e) => _viewModel.MoveSelected(-1);

    private void OnMoveDown(object sender, RoutedEventArgs e) => _viewModel.MoveSelected(1);

    private void OnRecapture(object sender, RoutedEventArgs e) => _viewModel.RecaptureSelected();

    private void OnApply(object sender, RoutedEventArgs e) => _viewModel.ApplySelected();

    private void OnSave(object sender, RoutedEventArgs e) => _viewModel.Save();
}
