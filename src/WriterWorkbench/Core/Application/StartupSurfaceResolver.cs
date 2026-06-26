using WriterWorkbench.Core.AppSettings;

namespace WriterWorkbench.Core.Application;

public static class StartupSurfaceResolver
{
    public static AppSessionState ApplyProjectSettings(
        AppSessionState sessionState,
        ProjectAppSettings projectSettings)
    {
        var surface = NormalizeSurface(projectSettings.LastSurface, sessionState.Surface);
        var documentId = string.IsNullOrWhiteSpace(projectSettings.LastSceneId)
            ? sessionState.DocumentId
            : projectSettings.LastSceneId.Trim();
        return sessionState with
        {
            Surface = surface,
            DocumentId = documentId
        };
    }

    public static string NormalizeSurface(string? surface, string? fallback = null)
    {
        var normalized = surface?.Trim() ?? "";
        return normalized is AppSessionState.EditorSurface
            or AppSessionState.PreviewSurface
            or AppSessionState.MainSurface
            or AppSessionState.HtmlWorkbenchSurface
            or AppSessionState.RelationshipMapSurface
            ? normalized
            : NormalizeFallback(fallback);
    }

    private static string NormalizeFallback(string? fallback)
    {
        var normalized = fallback?.Trim() ?? "";
        return normalized is AppSessionState.EditorSurface
            or AppSessionState.PreviewSurface
            or AppSessionState.MainSurface
            or AppSessionState.HtmlWorkbenchSurface
            or AppSessionState.RelationshipMapSurface
            ? normalized
            : AppSessionState.HtmlWorkbenchSurface;
    }
}
