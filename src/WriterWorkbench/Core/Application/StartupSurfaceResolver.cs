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
        if (string.Equals(normalized, AppSessionState.MainSurface, StringComparison.OrdinalIgnoreCase))
        {
            return AppSessionState.HtmlWorkbenchSurface;
        }

        return normalized is AppSessionState.EditorSurface
            or AppSessionState.PreviewSurface
            or AppSessionState.HtmlWorkbenchSurface
            or AppSessionState.RelationshipMapSurface
            ? normalized
            : NormalizeFallback(fallback);
    }

    public static string ToPersistedStartupSurface(string? surface)
    {
        var normalized = NormalizeSurface(surface);
        return normalized == AppSessionState.EditorSurface
            ? AppSessionState.HtmlWorkbenchSurface
            : normalized;
    }

    private static string NormalizeFallback(string? fallback)
    {
        var normalized = fallback?.Trim() ?? "";
        if (string.Equals(normalized, AppSessionState.MainSurface, StringComparison.OrdinalIgnoreCase))
        {
            return AppSessionState.HtmlWorkbenchSurface;
        }

        return normalized is AppSessionState.EditorSurface
            or AppSessionState.PreviewSurface
            or AppSessionState.HtmlWorkbenchSurface
            or AppSessionState.RelationshipMapSurface
            ? normalized
            : AppSessionState.HtmlWorkbenchSurface;
    }
}
