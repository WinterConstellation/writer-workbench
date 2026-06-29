using WriterWorkbench.Core.Application;

namespace WriterWorkbench.Tests;

public sealed class AppSessionStateServiceTests
{
    [Fact]
    public async Task SavesAndLoadsLastWorkspaceState()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"),
            "app.session.json");
        var service = new AppSessionStateService(path);
        var state = new AppSessionState(
            @"C:\WriterWorkbench\Projects\Novel.writerproj",
            "scene-0042",
            AppSessionState.PreviewSurface,
            2,
            "lavender",
            55,
            new RemoteControlSessionState(
                false,
                false,
                RemoteControlSessionState.IconOnlyDisplayMode,
                -120,
                80,
                310,
                520));

        await service.SaveAsync(state, CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal(state, loaded);
    }

    [Fact]
    public async Task LoadsLegacyStateWithoutRemoteControlUsingDefaults()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "app.session.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "ProjectRoot": "C:\\WriterWorkbench\\Projects\\Novel.writerproj",
              "DocumentId": "scene-0002",
              "Surface": "html-workbench",
              "PresetSlot": 1,
              "GraphicPresetId": "default",
              "FocusDurationMinutes": 40
            }
            """,
            CancellationToken.None);
        var service = new AppSessionStateService(path);

        var loaded = await service.LoadAsync(CancellationToken.None);
        Assert.NotNull(loaded.RemoteControl);
        var remote = loaded.RemoteControl!;

        Assert.True(remote.IsVisible);
        Assert.True(remote.IsDockedToMemoRail);
        Assert.Equal(RemoteControlSessionState.IconAndTitleDisplayMode, remote.DisplayMode);
        Assert.Null(remote.Left);
        Assert.Null(remote.Top);
        Assert.Equal(RemoteControlSessionState.DefaultWidth, remote.Width);
        Assert.Equal(RemoteControlSessionState.DefaultHeight, remote.Height);
    }

    [Fact]
    public async Task SavesAndLoadsRemoteControlPlacementAndDisplayMode()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"),
            "app.session.json");
        var service = new AppSessionStateService(path);
        var state = new AppSessionState(
            null,
            null,
            AppSessionState.HtmlWorkbenchSurface,
            null,
            "default",
            40,
            new RemoteControlSessionState(
                true,
                false,
                RemoteControlSessionState.IconOnlyDisplayMode,
                -640,
                120,
                280,
                460));

        await service.SaveAsync(state, CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);
        Assert.NotNull(loaded.RemoteControl);
        var remote = loaded.RemoteControl!;

        Assert.True(remote.IsVisible);
        Assert.False(remote.IsDockedToMemoRail);
        Assert.Equal(RemoteControlSessionState.IconOnlyDisplayMode, remote.DisplayMode);
        Assert.Equal(-640, remote.Left);
        Assert.Equal(120, remote.Top);
        Assert.Equal(280, remote.Width);
        Assert.Equal(460, remote.Height);
    }

    [Fact]
    public async Task NormalizesRemoteControlState()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"),
            "app.session.json");
        var service = new AppSessionStateService(path);

        await service.SaveAsync(
            new AppSessionState(
                null,
                null,
                AppSessionState.HtmlWorkbenchSurface,
                null,
                "default",
                40,
                new RemoteControlSessionState(
                    true,
                    false,
                    "bad-mode",
                    10,
                    20,
                    -1,
                    5000)),
            CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);
        Assert.NotNull(loaded.RemoteControl);
        var remote = loaded.RemoteControl!;

        Assert.Equal(RemoteControlSessionState.IconAndTitleDisplayMode, remote.DisplayMode);
        Assert.Equal(10, remote.Left);
        Assert.Equal(20, remote.Top);
        Assert.Equal(RemoteControlSessionState.DefaultWidth, remote.Width);
        Assert.Equal(RemoteControlSessionState.MaxHeight, remote.Height);
    }

    [Fact]
    public async Task NormalizesInvalidGraphicPresetToDefault()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"),
            "app.session.json");
        var service = new AppSessionStateService(path);

        await service.SaveAsync(
            new AppSessionState(null, null, "unknown", 99, "missing"),
            CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSessionState.HtmlWorkbenchSurface, loaded.Surface);
        Assert.Null(loaded.PresetSlot);
        Assert.Equal(AppSessionState.DefaultGraphicPresetId, loaded.GraphicPresetId);
        Assert.Equal(AppSessionState.DefaultFocusDurationMinutes, loaded.FocusDurationMinutes);
    }

    [Fact]
    public async Task NormalizesFocusDurationIntoSupportedRange()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"),
            "app.session.json");
        var service = new AppSessionStateService(path);

        await service.SaveAsync(
            new AppSessionState(null, null, AppSessionState.EditorSurface, null, "default", 999),
            CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal(240, loaded.FocusDurationMinutes);
    }

    [Fact]
    public async Task FallsBackToEmptyStateWhenFileIsInvalid()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchSessionTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "app.session.json");
        await File.WriteAllTextAsync(path, "{ not json", CancellationToken.None);
        var service = new AppSessionStateService(path);

        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSessionState.Empty, loaded);
    }
}
