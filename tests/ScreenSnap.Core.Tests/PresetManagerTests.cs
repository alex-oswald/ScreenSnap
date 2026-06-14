using ScreenSnap.Core.Abstractions;
using ScreenSnap.Core.Displays;
using ScreenSnap.Core.Presets;

namespace ScreenSnap.Core.Tests;

public class PresetManagerTests
{
    private sealed class FakeStore : IPresetStore
    {
        private PresetsDocument _document;
        public int SaveCount { get; private set; }

        public FakeStore(PresetsDocument? document = null) => _document = document ?? new PresetsDocument();

        public PresetsDocument Load() => _document;

        public void Save(PresetsDocument document)
        {
            _document = document;
            SaveCount++;
        }
    }

    private sealed class FakeDisplayService : IDisplayService
    {
        public int ApplyCount { get; private set; }
        public DisplayConfiguration? LastApplied { get; private set; }
        public bool NextResultSucceeds { get; set; } = true;

        public IReadOnlyList<MonitorInfo> GetMonitors() => Array.Empty<MonitorInfo>();

        public DisplayConfiguration CaptureCurrent() => new()
        {
            Monitors = { new MonitorState { DevicePath = "captured", FriendlyName = "Captured", Enabled = true, IsPrimary = true } },
        };

        public DisplayApplyResult Apply(DisplayConfiguration configuration)
        {
            ApplyCount++;
            LastApplied = configuration;
            return new DisplayApplyResult { Success = NextResultSucceeds, Error = NextResultSucceeds ? null : "failed" };
        }
    }

    private static PresetsDocument ThreePresets() => new()
    {
        Presets =
        {
            new Preset { Id = "a", Name = "A" },
            new Preset { Id = "b", Name = "B" },
            new Preset { Id = "c", Name = "C" },
        },
    };

    [Fact]
    public void ApplyNext_StartsAtFirst_ThenWrapsAround()
    {
        var display = new FakeDisplayService();
        var manager = new PresetManager(new FakeStore(ThreePresets()), display);

        Assert.Equal("A", Applied(manager.ApplyNext(), manager));
        Assert.Equal(0, manager.ActiveIndex);
        Assert.Equal("B", Applied(manager.ApplyNext(), manager));
        Assert.Equal("C", Applied(manager.ApplyNext(), manager));
        Assert.Equal("A", Applied(manager.ApplyNext(), manager)); // wrap

        static string Applied(DisplayApplyResult? result, PresetManager m)
        {
            Assert.NotNull(result);
            Assert.True(result!.Success);
            return m.ActivePreset!.Name;
        }
    }

    [Fact]
    public void ApplyPrevious_StartsAtLast_WhenNothingActive()
    {
        var manager = new PresetManager(new FakeStore(ThreePresets()), new FakeDisplayService());

        manager.ApplyPrevious();

        Assert.Equal("C", manager.ActivePreset!.Name);
    }

    [Fact]
    public void Cycle_ReturnsNull_WhenNoPresets()
    {
        var manager = new PresetManager(new FakeStore(), new FakeDisplayService());

        Assert.Null(manager.ApplyNext());
        Assert.Null(manager.ApplyPrevious());
    }

    [Fact]
    public void Apply_DoesNotMarkActive_WhenApplyFails()
    {
        var display = new FakeDisplayService { NextResultSucceeds = false };
        var manager = new PresetManager(new FakeStore(ThreePresets()), display);

        var result = manager.ApplyById("b");

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal(-1, manager.ActiveIndex);
    }

    [Fact]
    public void CaptureNew_AddsPresetAndPersists()
    {
        var store = new FakeStore();
        var manager = new PresetManager(store, new FakeDisplayService());

        bool changed = false;
        manager.PresetsChanged += (_, _) => changed = true;

        var preset = manager.CaptureNew("Desk");

        Assert.Equal("Desk", preset.Name);
        Assert.Single(manager.Presets);
        Assert.Single(preset.Configuration.Monitors);
        Assert.True(store.SaveCount >= 1);
        Assert.True(changed);
    }

    [Fact]
    public void Move_ReordersPresets()
    {
        var manager = new PresetManager(new FakeStore(ThreePresets()), new FakeDisplayService());

        manager.Move(0, 2);

        Assert.Equal(new[] { "B", "C", "A" }, manager.Presets.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Remove_DropsPreset()
    {
        var manager = new PresetManager(new FakeStore(ThreePresets()), new FakeDisplayService());
        var b = manager.Presets[1];

        manager.Remove(b);

        Assert.Equal(new[] { "A", "C" }, manager.Presets.Select(p => p.Name).ToArray());
    }
}
