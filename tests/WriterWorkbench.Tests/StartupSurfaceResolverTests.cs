using WriterWorkbench.Core.AppSettings;
using WriterWorkbench.Core.Application;

namespace WriterWorkbench.Tests;

public sealed class StartupSurfaceResolverTests
{
    [Fact]
    public void ProjectLastSurfaceOverridesLegacyGlobalEditorSession()
    {
        var session = new AppSessionState(
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            "scene-0001",
            AppSessionState.EditorSurface,
            null);
        var settings = ProjectAppSettings.Default with
        {
            LastSurface = AppSessionState.HtmlWorkbenchSurface,
            LastSceneId = "scene-0002"
        };

        var resolved = StartupSurfaceResolver.ApplyProjectSettings(session, settings);

        Assert.Equal(AppSessionState.HtmlWorkbenchSurface, resolved.Surface);
        Assert.Equal("scene-0002", resolved.DocumentId);
    }

    [Fact]
    public void ProjectLastSurfacePreservesUserChosenEditorAfterItIsSaved()
    {
        var session = new AppSessionState(
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            "scene-0001",
            AppSessionState.HtmlWorkbenchSurface,
            null);
        var settings = ProjectAppSettings.Default with
        {
            LastSurface = AppSessionState.EditorSurface,
            LastSceneId = "scene-0003"
        };

        var resolved = StartupSurfaceResolver.ApplyProjectSettings(session, settings);

        Assert.Equal(AppSessionState.EditorSurface, resolved.Surface);
        Assert.Equal("scene-0003", resolved.DocumentId);
    }
}
