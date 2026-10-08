namespace WriterWorkbench.Core.Export;

public sealed record ManuscriptExportResult(
    ManuscriptExportKind Kind,
    string OutputPath,
    int IncludedSceneCount,
    int ExcludedSceneCount,
    long CharacterCount)
{
    public int SceneCount => IncludedSceneCount;
}

public enum ManuscriptExportKind
{
    CurrentScene,
    FullManuscript
}
