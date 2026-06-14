using ScreenSnap.Core.Abstractions;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;

namespace ScreenSnap.Core.Tests;

public class JsonPresetStoreTests : IDisposable
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
    private readonly JsonPresetStore _store;

    public JsonPresetStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ScreenSnapTests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_root);
        _store = new JsonPresetStore(new TempStorageLocations(_root));
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
    public void Load_ReturnsEmptyDocument_WhenFileMissing()
    {
        var document = _store.Load();

        Assert.NotNull(document);
        Assert.Empty(document.Presets);
    }

    [Fact]
    public void Load_ReturnsEmptyDocument_WhenFileCorrupt()
    {
        File.WriteAllText(Path.Combine(_root, "presets.json"), "{ this is not valid json");

        var document = _store.Load();

        Assert.NotNull(document);
        Assert.Empty(document.Presets);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsPresets()
    {
        var document = new PresetsDocument
        {
            Presets =
            {
                new Preset
                {
                    Id = "desk",
                    Name = "Desk monitors",
                    Configuration = new DisplayConfiguration
                    {
                        Monitors =
                        {
                            new MonitorState { DevicePath = "A", FriendlyName = "Dell", Enabled = true, IsPrimary = true, Width = 3840, Height = 1600 },
                            new MonitorState { DevicePath = "B", FriendlyName = "TV", Enabled = false },
                        },
                    },
                },
                new Preset { Id = "tv", Name = "TV / gaming" },
            },
        };

        _store.Save(document);
        var loaded = _store.Load();

        Assert.Equal(2, loaded.Presets.Count);

        var desk = loaded.Presets[0];
        Assert.Equal("desk", desk.Id);
        Assert.Equal("Desk monitors", desk.Name);
        Assert.Equal(2, desk.Configuration.Monitors.Count);
        Assert.True(desk.Configuration.Monitors[0].IsPrimary);
        Assert.Equal(3840u, desk.Configuration.Monitors[0].Width);
        Assert.False(desk.Configuration.Monitors[1].Enabled);

        Assert.Equal("tv", loaded.Presets[1].Id);
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        _store.Save(new PresetsDocument { Presets = { new Preset { Id = "one", Name = "One" } } });
        _store.Save(new PresetsDocument { Presets = { new Preset { Id = "two", Name = "Two" } } });

        var loaded = _store.Load();

        Assert.Single(loaded.Presets);
        Assert.Equal("two", loaded.Presets[0].Id);
    }
}
