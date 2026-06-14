using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;
using ScreenSnap.Core.Settings;
using ScreenSnap.Hotkeys;
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
    private ISettingsStore? _settingsStore;
    private AppSettings _settings = new();
    private HotkeyManager? _hotkeys;
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

        _settingsStore = new JsonSettingsStore(storage);
        _settings = _settingsStore.Load();

        _tray = new TrayIcon();
        _tray.PresetSelected += OnPresetSelected;
        _tray.SettingsRequested += ShowSettings;
        _tray.ExitRequested += OnExitRequested;
        _tray.HotkeyPressed += OnHotkeyPressed;
        RefreshTrayPresets();

        _hotkeys = new HotkeyManager(_tray.WindowHandle);
        ApplyHotkeys();

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

    private void OnHotkeyPressed(int hotkeyId) => _hotkeys?.Handle(hotkeyId);

    private void ApplyHotkeys()
    {
        _hotkeys?.Configure(
            _settings,
            onNext: () => _presetManager?.ApplyNext(),
            onPrevious: () => _presetManager?.ApplyPrevious(),
            onJump: OnPresetSelected);
    }

    private void SaveAndApplyHotkeys()
    {
        _settingsStore?.Save(_settings);
        ApplyHotkeys();
    }

    private void ShowSettings()
    {
        if (_presetManager is null)
            return;

        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_presetManager, _settings, SaveAndApplyHotkeys);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Activate();

        var hwnd = new HWND(WindowNative.GetWindowHandle(_settingsWindow));
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(hwnd);
    }

    private void OnExitRequested()
    {
        _hotkeys?.Dispose();
        _hotkeys = null;

        _tray?.Dispose();
        _tray = null;

        _settingsWindow?.Close();
        _settingsWindow = null;

        Exit();
    }
}
