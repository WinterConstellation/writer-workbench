using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Snapshots;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class SceneSnapshotServiceTests
{
    [Fact]
    public async Task CreateSnapshotWritesDocumentTextAndMetadataUnderSceneFolder()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = CreateDocument("scene-0001", "첫 장면", "한국어 원고 본문");
        await store.SaveDocumentAsync(document, CancellationToken.None);
        await new SceneMetadataStore(paths).SaveAsync(
            new SceneMetadata(1, document.Id, "스냅샷 시놉시스", SceneStatus.Revising, ["태그"], 1200, DateTimeOffset.UtcNow),
            CancellationToken.None);
        var service = new SceneSnapshotService(paths, store);

        var snapshot = await service.CreateSnapshotAsync(document.Id, "수동", CancellationToken.None);

        Assert.Equal(document.Id, snapshot.DocumentId);
        Assert.Equal(document.Title, snapshot.Title);
        Assert.Equal("수동", snapshot.Reason);
        Assert.StartsWith(paths.SceneSnapshotsPath(document.Id), snapshot.FolderPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(paths.SceneSnapshotDocumentJsonPath(document.Id, snapshot.SnapshotId)));
        Assert.True(File.Exists(paths.SceneSnapshotDocumentTextPath(document.Id, snapshot.SnapshotId)));
        Assert.True(File.Exists(paths.SceneSnapshotMetadataPath(document.Id, snapshot.SnapshotId)));
        Assert.Equal("한국어 원고 본문", await File.ReadAllTextAsync(paths.SceneSnapshotDocumentTextPath(document.Id, snapshot.SnapshotId), CancellationToken.None));
        Assert.Contains("스냅샷 시놉시스", await File.ReadAllTextAsync(paths.SceneSnapshotMetadataPath(document.Id, snapshot.SnapshotId), CancellationToken.None));
    }

    [Fact]
    public async Task ListSnapshotsReturnsActiveSceneSnapshotsNewestFirst()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = await store.CreateDocumentAsync("Second", CancellationToken.None);
        var service = new SceneSnapshotService(paths, store);

        var older = await service.CreateSnapshotAsync(first.Id, "older", CancellationToken.None);
        await Task.Delay(20);
        var newer = await service.CreateSnapshotAsync(first.Id, "newer", CancellationToken.None);
        await service.CreateSnapshotAsync(second.Id, "other scene", CancellationToken.None);

        var snapshots = await service.ListSnapshotsAsync(first.Id, CancellationToken.None);

        Assert.Equal([newer.SnapshotId, older.SnapshotId], snapshots.Select(snapshot => snapshot.SnapshotId));
    }

    [Fact]
    public async Task RestoreSnapshotRestoresDocumentMetadataAndSearchIndex()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var metadataStore = new SceneMetadataStore(paths);
        var original = CreateDocument("scene-0001", "원래 제목", "복원될 본문");
        await store.SaveDocumentAsync(original, CancellationToken.None);
        await metadataStore.SaveAsync(
            new SceneMetadata(1, original.Id, "복원될 메모", SceneStatus.RevisionComplete, ["복원"], 900, DateTimeOffset.UtcNow, FileCategory: "원고"),
            CancellationToken.None);
        var service = new SceneSnapshotService(paths, store);
        var snapshot = await service.CreateSnapshotAsync(original.Id, "restore source", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(original.Id, "바뀐 제목", "바뀐 본문"), CancellationToken.None);
        await metadataStore.SaveAsync(
            new SceneMetadata(1, original.Id, "바뀐 메모", SceneStatus.Draft, ["변경"], null, DateTimeOffset.UtcNow),
            CancellationToken.None);

        var restored = await service.RestoreSnapshotAsync(original.Id, snapshot.SnapshotId, CancellationToken.None);
        var loaded = await store.LoadDocumentAsync(original.Id, CancellationToken.None);
        var metadata = await metadataStore.LoadAsync(original.Id, CancellationToken.None);
        var hits = await store.SearchAsync("복원될", CancellationToken.None);

        Assert.Equal(snapshot.SnapshotId, restored.Snapshot.SnapshotId);
        Assert.Equal("원래 제목", loaded.Title);
        Assert.Equal("복원될 본문", loaded.Paragraphs[0].Text);
        Assert.Equal("복원될 메모", metadata.Synopsis);
        Assert.Equal(SceneStatus.RevisionComplete, metadata.Status);
        Assert.Equal("원고", metadata.FileCategory);
        Assert.Equal(["복원"], metadata.Tags);
        Assert.Single(hits);
        Assert.Equal(original.Id, hits[0].DocumentId);
    }

    [Fact]
    public async Task DeleteSnapshotRemovesSnapshotFolderOnly()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = CreateDocument("scene-0001", "장면", "삭제 테스트 본문");
        await store.SaveDocumentAsync(document, CancellationToken.None);
        var service = new SceneSnapshotService(paths, store);
        var snapshot = await service.CreateSnapshotAsync(document.Id, "delete", CancellationToken.None);

        await service.DeleteSnapshotAsync(document.Id, snapshot.SnapshotId, CancellationToken.None);

        Assert.False(Directory.Exists(paths.SceneSnapshotPath(document.Id, snapshot.SnapshotId)));
        Assert.True(File.Exists(paths.DocumentJsonPath(document.Id)));
        Assert.True(File.Exists(paths.DocumentTextPath(document.Id)));
    }

    [Fact]
    public async Task RestoreMissingSnapshotThrowsWithoutMutatingDocument()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = CreateDocument("scene-0001", "장면", "안전 본문");
        await store.SaveDocumentAsync(document, CancellationToken.None);
        var before = await File.ReadAllTextAsync(paths.DocumentJsonPath(document.Id), CancellationToken.None);
        var service = new SceneSnapshotService(paths, store);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            service.RestoreSnapshotAsync(document.Id, "missing", CancellationToken.None));

        Assert.Equal(before, await File.ReadAllTextAsync(paths.DocumentJsonPath(document.Id), CancellationToken.None));
    }

    private static string CreateRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }

    private static WriterDocument CreateDocument(string id, string title, string text)
    {
        return new WriterDocument(
            id,
            title,
            [new WriterParagraph("p-0001", text, "body", [], [])]);
    }
}
