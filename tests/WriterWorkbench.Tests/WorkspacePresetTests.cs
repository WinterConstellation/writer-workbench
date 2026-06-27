using WriterWorkbench.Core.Workspace;

namespace WriterWorkbench.Tests;

public sealed class WorkspacePresetTests
{
    [Fact]
    public void StoresPresetSlotAndStartupFlag()
    {
        var preset = new WorkspacePreset(
            Slot: 1,
            Name: "Main desk",
            Region: MonitorRegion.Full,
            AutoApplyOnStartup: true,
            Placement: new WindowPlacement(10, 20, 1200, 800, "Normal"));

        Assert.Equal(1, preset.Slot);
        Assert.True(preset.AutoApplyOnStartup);
        Assert.Equal(MonitorRegion.Full, preset.Region);
        Assert.Equal(1200, preset.Placement?.Width);
    }

    [Fact]
    public void StoresDetachedWorkbenchWindowPlacements()
    {
        var preset = new WorkspacePreset(
            Slot: 2,
            Name: "Multi desk",
            Region: MonitorRegion.Full,
            AutoApplyOnStartup: false,
            Placement: new WindowPlacement(10, 20, 1200, 800, "Normal"),
            DetachedWindows:
            [
                new WorkspaceDetachedWindowPlacement(
                    "preview",
                    new WindowPlacement(1920, 0, 960, 1040, "Normal")),
                new WorkspaceDetachedWindowPlacement(
                    "relationship-map",
                    new WindowPlacement(2880, 0, 960, 1040, "Maximized"))
            ]);

        Assert.Equal(2, preset.DetachedWindows?.Count);
        Assert.Equal("preview", preset.DetachedWindows?[0].SurfaceId);
        Assert.Equal(2880, preset.DetachedWindows?[1].Placement.Left);
    }

    [Fact]
    public async Task PersistsPresetToDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchPresetTests", Guid.NewGuid().ToString("N"), "workspace.presets.json");
        var service = new WorkspacePresetService(path);
        var preset = new WorkspacePreset(
            1,
            "Main desk",
            MonitorRegion.Full,
            true,
            new WindowPlacement(30, 40, 1400, 900, "Maximized"));

        await service.SaveAsync(preset, CancellationToken.None);

        var loadedService = new WorkspacePresetService(path);
        await loadedService.LoadAsync(CancellationToken.None);
        var loaded = loadedService.Get(1);

        Assert.NotNull(loaded);
        Assert.Equal("Main desk", loaded.Name);
        Assert.Equal(1400, loaded.Placement?.Width);
        Assert.Equal("Maximized", loaded.Placement?.WindowState);
    }

    [Fact]
    public async Task PersistsDetachedWorkbenchWindowsToDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchPresetTests", Guid.NewGuid().ToString("N"), "workspace.presets.json");
        var service = new WorkspacePresetService(path);
        var preset = new WorkspacePreset(
            1,
            "Three monitor desk",
            MonitorRegion.Full,
            false,
            new WindowPlacement(0, 0, 1600, 900, "Normal"),
            [
                new WorkspaceDetachedWindowPlacement(
                    "preview",
                    new WindowPlacement(1600, 0, 960, 900, "Normal")),
                new WorkspaceDetachedWindowPlacement(
                    "relationship-map",
                    new WindowPlacement(2560, 0, 960, 900, "Normal"))
            ]);

        await service.SaveAsync(preset, CancellationToken.None);

        var loadedService = new WorkspacePresetService(path);
        await loadedService.LoadAsync(CancellationToken.None);
        var loaded = loadedService.Get(1);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.DetachedWindows?.Count);
        Assert.Equal("preview", loaded.DetachedWindows?[0].SurfaceId);
        Assert.Equal(1600, loaded.DetachedWindows?[0].Placement.Left);
        Assert.Equal("relationship-map", loaded.DetachedWindows?[1].SurfaceId);
    }

    [Fact]
    public async Task LoadsLegacyPresetWithoutDetachedWindowsAsEmptyList()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchPresetTests", Guid.NewGuid().ToString("N"), "workspace.presets.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(
            path,
            """
            [
              {
                "Slot": 1,
                "Name": "Legacy",
                "Region": 0,
                "AutoApplyOnStartup": false,
                "Placement": {
                  "Left": 10,
                  "Top": 20,
                  "Width": 1200,
                  "Height": 800,
                  "WindowState": "Normal"
                }
              }
            ]
            """);

        var service = new WorkspacePresetService(path);
        await service.LoadAsync(CancellationToken.None);

        var loaded = service.Get(1);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded.DetachedWindows);
        Assert.Empty(loaded.DetachedWindows);
    }

    [Fact]
    public async Task FindsStartupPresetByLowestEnabledSlot()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchPresetTests", Guid.NewGuid().ToString("N"), "workspace.presets.json");
        var service = new WorkspacePresetService(path);

        await service.SaveAsync(new WorkspacePreset(3, "Third", MonitorRegion.Full, true), CancellationToken.None);
        await service.SaveAsync(new WorkspacePreset(1, "First", MonitorRegion.LeftHalf, false), CancellationToken.None);
        await service.SaveAsync(new WorkspacePreset(2, "Second", MonitorRegion.RightHalf, true), CancellationToken.None);

        var startup = service.GetStartupPreset();

        Assert.NotNull(startup);
        Assert.Equal(2, startup.Slot);
    }

    [Fact]
    public async Task PersistsSingleStartupPresetSelection()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchPresetTests", Guid.NewGuid().ToString("N"), "workspace.presets.json");
        var service = new WorkspacePresetService(path);
        await service.SaveAsync(new WorkspacePreset(1, "First", MonitorRegion.Full, true), CancellationToken.None);
        await service.SaveAsync(new WorkspacePreset(2, "Second", MonitorRegion.LeftHalf, false), CancellationToken.None);

        await service.SetStartupPresetAsync(2, CancellationToken.None);

        var loadedService = new WorkspacePresetService(path);
        await loadedService.LoadAsync(CancellationToken.None);

        Assert.False(loadedService.Get(1)?.AutoApplyOnStartup);
        Assert.True(loadedService.Get(2)?.AutoApplyOnStartup);
    }
}
