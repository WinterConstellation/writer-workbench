namespace WriterWorkbench.Core.AppSettings;

public sealed record ProjectAppSettings(
    string Schema,
    string LastWorkspaceLabel,
    bool AutosaveEnabled,
    string LastSurface,
    string? LastSceneId)
{
    public static ProjectAppSettings Default { get; } = new(
        "app",
        "마지막 작업",
        true,
        "html-workbench",
        null);
}
