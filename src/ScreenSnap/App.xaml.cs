using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;
using ScreenSnap.Services;
using ScreenSnap.Settings;
using ScreenSnap.Tray;
using WinRT.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ScreenSnap;

public partial class App : Application
{
    private DispatcherQueue? _dispatcher;
    private PresetManager? _presetManager;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        var storage = new LocalAppDataStorageLocations();
        var display = new CcdDisplayService();
        var store = new JsonPresetStore(storage);
        _presetManager = new PresetManager(store, display);
        _presetManager.PresetsChanged += (_, _) => RefreshTrayPresets();

        _tray = new TrayIcon();
        _tray.PresetSelected += OnPresetSelected;
        _tray.SettingsRequested += ShowSettings;
        _tray.ExitRequested += OnExitRequested;
        RefreshTrayPresets();

        // A second launch asks the running instance to surface its settings window.
        SingleInstance.Activated += () => _dispatcher?.TryEnqueue(ShowSettings);
        SingleInstance.StartListener();
    }

    private void RefreshTrayPresets()
    {
        if (_presetManager is null || _tray is null)
            return;

        var names = _presetManager.Presets.Select(p => p.Name).ToArray();
        _tray.SetPresets(names);
    }

    private void OnPresetSelected(int index)
    {
        if (_presetManager is null)
            return;

        var presets = _presetManager.Presets;
        if (index < 0 || index >= presets.Count)
            return;

        _presetManager.Apply(presets[index]);
    }

    private void ShowSettings()
    {
        if (_presetManager is null)
            return;

        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_presetManager);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Activate();

        var hwnd = new HWND(WindowNative.GetWindowHandle(_settingsWindow));
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(hwnd);
    }

    private void OnExitRequested()
    {
        _tray?.Dispose();
        _tray = null;

        _settingsWindow?.Close();
        _settingsWindow = null;

        Exit();
    }
}
