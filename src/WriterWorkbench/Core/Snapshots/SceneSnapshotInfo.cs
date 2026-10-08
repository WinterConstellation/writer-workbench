namespace WriterWorkbench.Core.Snapshots;

public sealed record SceneSnapshotInfo(
    int SchemaVersion,
    string SnapshotId,
    string DocumentId,
    string Title,
    DateTimeOffset CreatedAt,
    string Reason,
    string FolderPath,
    int CharacterCount);

public sealed record SceneSnapshotRestoreResult(
    SceneSnapshotInfo Snapshot,
    string DocumentId,
    string Title);
