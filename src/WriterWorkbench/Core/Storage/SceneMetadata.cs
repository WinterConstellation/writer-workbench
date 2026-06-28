namespace WriterWorkbench.Core.Storage;

public sealed record SceneMetadata(
    int SchemaVersion,
    string DocumentId,
    string Synopsis,
    SceneStatus Status,
    IReadOnlyList<string> Tags,
    int? TargetCharacterCount,
    DateTimeOffset UpdatedAt,
    int ContentLength = 0,
    int ContentLengthWithSpaces = 0,
    string SceneType = "Scene",
    bool ManualLineBreak = false,
    DateTimeOffset CreatedAt = default,
    string Summary = "",
    string FileCategory = "원고")
{
    public const int CurrentSchemaVersion = 3;

    public static SceneMetadata CreateDefault(string documentId)
    {
        var now = DateTimeOffset.UtcNow;
        return new SceneMetadata(
            CurrentSchemaVersion,
            documentId,
            "",
            SceneStatus.Draft,
            [],
            null,
            now,
            CreatedAt: now);
    }

    public SceneMetadata CopyForDocument(string documentId)
    {
        var now = DateTimeOffset.UtcNow;
        return this with
        {
            DocumentId = documentId,
            Tags = Tags.ToList(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public enum SceneStatus
{
    Draft,
    Revising,
    RevisionComplete,
    UploadPending,
    Uploaded,
    Final,
    Excluded
}
