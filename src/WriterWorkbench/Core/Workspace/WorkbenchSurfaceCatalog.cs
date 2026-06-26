using WriterWorkbench.Core.Application;

namespace WriterWorkbench.Core.Workspace;

public sealed record WorkbenchSurfaceOption(
    string SurfaceId,
    string Name,
    string Description);

public sealed record WorkbenchSurfaceAvailability(
    string SurfaceId,
    string Name,
    string Description,
    bool IsAvailable,
    string? OccupiedBy);

public static class WorkbenchSurfaceCatalog
{
    public static IReadOnlyList<WorkbenchSurfaceOption> All { get; } =
    [
        new(AppSessionState.EditorSurface, "작품 수정", "현재 원고를 쓰고 고치는 화면"),
        new(AppSessionState.HtmlWorkbenchSurface, "HTML 작업대", "HTML/CSS로 구성한 프로젝트 작업대"),
        new(AppSessionState.MainSurface, "메인", "프로젝트 접근과 빠른 실행 화면"),
        new(AppSessionState.PreviewSurface, "미리보기", "현재 원고를 읽기 화면으로 확인"),
        new(AppSessionState.RelationshipMapSurface, "관계도", "인물과 관계를 배치하는 화면")
    ];

    public static string GetName(string surfaceId)
    {
        return All.FirstOrDefault(option =>
            string.Equals(option.SurfaceId, surfaceId, StringComparison.OrdinalIgnoreCase))?.Name
            ?? surfaceId;
    }
}
