using ScreenSnap.Core.Abstractions;
using ScreenSnap.Core.Settings;

namespace ScreenSnap.Core.Tests;

public class JsonSettingsStoreTests : IDisposable
{
    private sealed class TempStorageLocations : IStorageLocations
    {
        public TempStorageLocations(string root)
        {
            AppDataDirectory = root;
            PresetsFilePath = Path.Combine(root, "presets.json");
            SettingsFilePath = Path.Combine(root, "settings.json");
        }

        public string AppDataDirectory { get; }
        public string PresetsFilePath { get; }
        public string SettingsFilePath { get; }
    }

    private readonly string _root;
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ScreenSnapTests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_root);
        _store = new JsonSettingsStore(new TempStorageLocations(_root));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var settings = _store.Load();

        Assert.True(settings.HotkeysEnabled);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.Modifiers);
        Assert.False(settings.EnableJumpHotkeys);
        Assert.False(settings.RunAtStartup);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileCorrupt()
    {
        File.WriteAllText(Path.Combine(_root, "settings.json"), "{ not valid json");

        var settings = _store.Load();

        Assert.True(settings.HotkeysEnabled);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.Modifiers);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsSettings()
    {
        var settings = new AppSettings
        {
            HotkeysEnabled = false,
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Win,
            EnableJumpHotkeys = true,
            RunAtStartup = true,
        };

        _store.Save(settings);
        var loaded = _store.Load();

        Assert.False(loaded.HotkeysEnabled);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Win, loaded.Modifiers);
        Assert.True(loaded.EnableJumpHotkeys);
        Assert.True(loaded.RunAtStartup);
    }

    [Fact]
    public void Save_WritesModifiersAsStringNames()
    {
        _store.Save(new AppSettings { Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt });

        string json = File.ReadAllText(Path.Combine(_root, "settings.json"));

        Assert.Contains("Control", json);
        Assert.Contains("Alt", json);
    }
}
