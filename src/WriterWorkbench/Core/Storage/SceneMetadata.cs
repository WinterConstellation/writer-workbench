namespace WriterWorkbench.Core.Storage;

public sealed record SceneMetadata(
    int SchemaVersion,
    string DocumentId,
    string Synopsis,
    SceneStatus Status,
    IReadOnlyList<string> Tags,
    int? TargetCharacterCount,
    DateTimeOffset UpdatedAt)
{
    public static SceneMetadata CreateDefault(string documentId)
    {
        return new SceneMetadata(
            1,
            documentId,
            "",
            SceneStatus.Draft,
            [],
            null,
            DateTimeOffset.UtcNow);
    }

    public SceneMetadata CopyForDocument(string documentId)
    {
        return this with
        {
            DocumentId = documentId,
            Tags = Tags.ToList(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}

public enum SceneStatus
{
    Draft,
    Revising,
    Final,
    Excluded
}
