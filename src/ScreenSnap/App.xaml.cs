using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ScreenSnap.Core.Abstractions;
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
    private IAutostartService? _autostart;
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

        _autostart = new RegistryAutostartService();
        ReconcileAutostart();

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

    private void OnPresetSelected(int index) => ApplyPresetAt(index);

    private void ApplyPresetAt(int index)
    {
        if (_presetManager is null)
            return;

        var presets = _presetManager.Presets;
        if (index < 0 || index >= presets.Count)
            return;

        var preset = presets[index];
        var result = _presetManager.Apply(preset);
        NotifyApplied(preset.Name, result);
    }

    private void ApplyNext() => NotifyApplied(_presetManager?.ActivePreset?.Name, _presetManager?.ApplyNext());

    private void ApplyPrevious() => NotifyApplied(_presetManager?.ActivePreset?.Name, _presetManager?.ApplyPrevious());

    private void NotifyApplied(string? presetName, DisplayApplyResult? result)
    {
        if (_tray is null || result is null)
            return;

        if (result.Success)
        {
            string name = string.IsNullOrWhiteSpace(presetName) ? "preset" : presetName!;
            string message = result.MissingMonitors.Count > 0
                ? $"Switched to \u201c{name}\u201d. Skipped {result.MissingMonitors.Count} disconnected monitor(s)."
                : $"Switched to \u201c{name}\u201d.";
            _tray.ShowBalloon("ScreenSnap", message);
        }
        else
        {
            _tray.ShowBalloon("ScreenSnap", result.Error ?? "Couldn't apply the preset.", isError: true);
        }
    }

    private void OnHotkeyPressed(int hotkeyId) => _hotkeys?.Handle(hotkeyId);

    private void ApplyHotkeys()
    {
        if (_hotkeys is null)
            return;

        bool registered = _hotkeys.Configure(
            _settings,
            onNext: ApplyNext,
            onPrevious: ApplyPrevious,
            onJump: ApplyPresetAt);

        if (_settings.HotkeysEnabled && !registered)
        {
            _tray?.ShowBalloon(
                "ScreenSnap",
                "Couldn't register the preset hotkeys. Another app may already use that key combination.",
                isError: true);
        }
    }

    private void ReconcileAutostart()
    {
        if (_autostart is null)
            return;

        try
        {
            if (_settings.RunAtStartup && !_autostart.IsEnabled)
                _autostart.Enable();
            else if (!_settings.RunAtStartup && _autostart.IsEnabled)
                _autostart.Disable();
        }
        catch
        {
            // Registry access can be restricted by policy; autostart is best-effort.
        }
    }

    private void OnSettingsChanged()
    {
        _settingsStore?.Save(_settings);
        ApplyHotkeys();
        ReconcileAutostart();
    }

    private void ShowSettings()
    {
        if (_presetManager is null)
            return;

        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_presetManager, _settings, OnSettingsChanged);
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
