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
            55);

        await service.SaveAsync(state, CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal(state, loaded);
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
