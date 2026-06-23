namespace WriterWorkbench.Core.Export;

public sealed record ManuscriptExportResult(
    ManuscriptExportKind Kind,
    string OutputPath,
    int SceneCount);

public enum ManuscriptExportKind
{
    CurrentScene,
    FullManuscript
}
