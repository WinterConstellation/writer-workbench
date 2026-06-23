namespace WriterWorkbench.Core.Storage;

public sealed record ProjectManifest(
    int SchemaVersion,
    string Title,
    IReadOnlyList<ProjectDocumentInfo> Documents
);

public sealed record ProjectDocumentInfo(
    string Id,
    string Title,
    string JsonPath,
    string TextPath,
    DateTimeOffset UpdatedAt
);

public sealed record SearchHit(
    string DocumentId,
    string Title,
    string Snippet
);
