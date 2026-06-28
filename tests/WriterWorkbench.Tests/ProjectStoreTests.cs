using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class ProjectStoreTests
{
    [Fact]
    public async Task SaveDocumentWritesCanonicalJsonAndDerivedTxt()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "Opening",
            [new WriterParagraph("p-1", "첫 문단", "body", [], [])]);

        await store.SaveDocumentAsync(document, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(root, "documents", "scene-0001.wwdoc.json")));
        Assert.Equal("첫 문단", await File.ReadAllTextAsync(Path.Combine(root, "documents", "scene-0001.txt")));
    }

    [Fact]
    public async Task SaveAutosaveCopyWritesSeparateJsonAndTxtWithoutMutatingManifest()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-autosave",
            "Autosave",
            [new WriterParagraph("p-1", "자동저장 한글 본문", "body", [], [])]);

        await store.SaveAutosaveCopyAsync(document, CancellationToken.None);

        Assert.True(File.Exists(paths.AutosaveDocumentJsonPath(document.Id)));
        Assert.True(File.Exists(paths.AutosaveDocumentTextPath(document.Id)));
        Assert.Equal("자동저장 한글 본문", await File.ReadAllTextAsync(paths.AutosaveDocumentTextPath(document.Id)));
        Assert.False(File.Exists(paths.ManifestPath));
    }

    [Fact]
    public async Task SaveDocumentUpdatesDerivedSceneMetadataCountsForKoreanBody()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-korean",
            "Korean",
            [
                new WriterParagraph("p-1", "가 나", "body", [], []),
                new WriterParagraph("p-2", "라마바사", "body", [], [])
            ]);

        await store.SaveDocumentAsync(document, CancellationToken.None);
        var metadata = await new SceneMetadataStore(paths).LoadAsync(document.Id, CancellationToken.None);

        Assert.Equal(6, metadata.ContentLength);
        Assert.Equal(7, metadata.ContentLengthWithSpaces);
        Assert.Equal("Scene", metadata.SceneType);
        Assert.False(metadata.ManualLineBreak);
        Assert.True(metadata.UpdatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task SaveDocumentPreservesManualSceneMetadataAndRefreshesDerivedCounts()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var metadataStore = new SceneMetadataStore(paths);
        await metadataStore.SaveAsync(
            new SceneMetadata(
                SceneMetadata.CurrentSchemaVersion,
                "scene-keep",
                "",
                SceneStatus.RevisionComplete,
                ["태그"],
                1500,
                DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"),
                ContentLength: 1,
                ContentLengthWithSpaces: 1,
                SceneType: "Cover",
                ManualLineBreak: true,
                CreatedAt: DateTimeOffset.Parse("2025-12-31T00:00:00+00:00"),
                Summary: "수동 요약",
                FileCategory: "표지"),
            CancellationToken.None);
        var document = new WriterDocument(
            "scene-keep",
            "Keep",
            [new WriterParagraph("p-1", "새 본문", "body", [], [])]);

        await store.SaveDocumentAsync(document, CancellationToken.None);
        var metadata = await metadataStore.LoadAsync(document.Id, CancellationToken.None);

        Assert.Equal("수동 요약", metadata.Summary);
        Assert.Equal(SceneStatus.RevisionComplete, metadata.Status);
        Assert.Equal("표지", metadata.FileCategory);
        Assert.Equal(["태그"], metadata.Tags);
        Assert.Equal(1500, metadata.TargetCharacterCount);
        Assert.Equal("Cover", metadata.SceneType);
        Assert.True(metadata.ManualLineBreak);
        Assert.Equal(DateTimeOffset.Parse("2025-12-31T00:00:00+00:00"), metadata.CreatedAt);
        Assert.Equal(3, metadata.ContentLength);
        Assert.Equal(4, metadata.ContentLengthWithSpaces);
        Assert.True(metadata.UpdatedAt > DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"));
    }

    [Fact]
    public async Task CreateProjectCreatesManifestAndInitialDocument()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));

        var manifest = await store.CreateProjectAsync("테스트 작품", CancellationToken.None);

        Assert.Equal("테스트 작품", manifest.Title);
        Assert.Single(manifest.Documents);
        Assert.Equal("scene-0001", manifest.Documents[0].Id);
        Assert.True(File.Exists(Path.Combine(root, "project.manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "documents", "scene-0001.wwdoc.json")));
    }

    [Fact]
    public async Task CreateDocumentGeneratesSequentialSceneIdsAndStoresEditableTitle()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);

        var first = await store.CreateDocumentAsync("새 장면", CancellationToken.None);
        var second = await store.CreateDocumentAsync("두 번째 장면", CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);
        var loadedSecond = await store.LoadDocumentAsync(second.Id, CancellationToken.None);
        var metadata = await new SceneMetadataStore(paths).LoadAsync(second.Id, CancellationToken.None);

        Assert.Equal("scene-0001", first.Id);
        Assert.Equal("scene-0002", second.Id);
        Assert.Equal("두 번째 장면", second.Title);
        Assert.Equal("두 번째 장면", loadedSecond.Title);
        Assert.Equal([first.Id, second.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal("두 번째 장면", manifest.Documents.Single(document => document.Id == second.Id).Title);
        Assert.Equal(second.Id, metadata.DocumentId);
        Assert.Equal(SceneStatus.Draft, metadata.Status);
    }

    [Fact]
    public async Task LoadDocumentReturnsSavedDocument()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));
        var document = new WriterDocument(
            "scene-0007",
            "Loaded",
            [new WriterParagraph("p-1", "불러올 문장", "body", [], [])]);

        await store.SaveDocumentAsync(document, CancellationToken.None);
        var loaded = await store.LoadDocumentAsync("scene-0007", CancellationToken.None);

        Assert.Equal("Loaded", loaded.Title);
        Assert.Equal("불러올 문장", loaded.Paragraphs[0].Text);
    }

    [Fact]
    public async Task RenameDocumentUpdatesManifestDocumentMetadataAndSearchIndex()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "Before",
            [new WriterParagraph("p-0001", "body stays untouched", "body", [], [])]);
        await store.SaveDocumentAsync(document, CancellationToken.None);

        var renamed = await store.RenameDocumentAsync(document.Id, "After", CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);
        var loaded = await store.LoadDocumentAsync(document.Id, CancellationToken.None);
        var titleHits = await store.SearchAsync("After", CancellationToken.None);
        var text = await File.ReadAllTextAsync(paths.DocumentTextPath(document.Id), CancellationToken.None);

        Assert.Equal(document.Id, renamed.Id);
        Assert.Equal("After", renamed.Title);
        Assert.Equal("After", loaded.Title);
        Assert.Equal("After", manifest.Documents.Single(info => info.Id == document.Id).Title);
        Assert.Equal("body stays untouched", text);
        Assert.Single(titleHits);
        Assert.Equal(document.Id, titleHits[0].DocumentId);
        Assert.Equal("After", titleHits[0].Title);
    }

    [Fact]
    public async Task LoadManifestRestoresBinderMetadataWithoutReadingDocumentBodies()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = await store.CreateDocumentAsync("Second", CancellationToken.None);
        File.Delete(paths.DocumentJsonPath(second.Id));

        var manifest = await new ProjectStore(paths).LoadManifestAsync(CancellationToken.None);

        Assert.Equal([first.Id, second.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal("Second", manifest.Documents.Single(document => document.Id == second.Id).Title);
    }

    [Fact]
    public async Task SearchUsesSqliteIndex()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));
        var document = new WriterDocument(
            "scene-0003",
            "Searchable",
            [new WriterParagraph("p-1", "검은 탑 아래에서 약속했다.", "body", [], [])]);

        await store.SaveDocumentAsync(document, CancellationToken.None);
        var results = await store.SearchAsync("약속", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("scene-0003", results[0].DocumentId);
        Assert.Contains("약속", results[0].Snippet);
    }

    [Fact]
    public async Task MoveDocumentChangesManifestOrderWithoutRenumberingScenes()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = await store.CreateDocumentAsync("Second", CancellationToken.None);
        var third = await store.CreateDocumentAsync("Third", CancellationToken.None);

        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);
        var manifest = await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);

        Assert.Equal([third.Id, first.Id, second.Id], manifest.Documents.Select(document => document.Id));
    }

    [Fact]
    public async Task MoveDocumentOrderPersistsAfterStoreReload()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = await store.CreateDocumentAsync("Second", CancellationToken.None);
        var third = await store.CreateDocumentAsync("Third", CancellationToken.None);

        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);
        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);
        var reloadedManifest = await new ProjectStore(paths).LoadManifestAsync(CancellationToken.None);

        Assert.Equal([third.Id, first.Id, second.Id], reloadedManifest.Documents.Select(document => document.Id));
    }

    [Fact]
    public async Task SaveDocumentPreservesExistingBinderOrder()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = await store.CreateDocumentAsync("Second", CancellationToken.None);
        var third = await store.CreateDocumentAsync("Third", CancellationToken.None);
        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);
        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);

        await store.SaveDocumentAsync(first with { Title = "First renamed" }, CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);

        Assert.Equal([third.Id, first.Id, second.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal("First renamed", manifest.Documents.Single(document => document.Id == first.Id).Title);
    }

    [Fact]
    public async Task DuplicateDocumentCopiesSceneAfterSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));
        var source = new WriterDocument(
            "scene-0001",
            "Source",
            [
                new WriterParagraph("p-0001", "First paragraph", "body", ["draft"], ["note"]),
                new WriterParagraph("p-0002", "Second paragraph", "body", [], [])
            ]);
        await store.SaveDocumentAsync(source, CancellationToken.None);
        await new SceneMetadataStore(ProjectPaths.ForRoot(root)).SaveAsync(
            new SceneMetadata(1, source.Id, "복제할 시놉시스", SceneStatus.Revising, ["태그A", "태그B"], 4200, DateTimeOffset.UtcNow, FileCategory: "자료"),
            CancellationToken.None);
        var after = await store.CreateDocumentAsync("After", CancellationToken.None);

        var duplicate = await store.DuplicateDocumentAsync(source.Id, CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);
        var reloadedManifest = await new ProjectStore(ProjectPaths.ForRoot(root)).LoadManifestAsync(CancellationToken.None);
        var loadedDuplicate = await store.LoadDocumentAsync(duplicate.Id, CancellationToken.None);
        var duplicateMetadata = await new SceneMetadataStore(ProjectPaths.ForRoot(root)).LoadAsync(duplicate.Id, CancellationToken.None);

        Assert.Equal([source.Id, duplicate.Id, after.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal([source.Id, duplicate.Id, after.Id], reloadedManifest.Documents.Select(document => document.Id));
        Assert.Equal("Source 복사", duplicate.Title);
        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal(source.Paragraphs.Select(paragraph => paragraph.Text), loadedDuplicate.Paragraphs.Select(paragraph => paragraph.Text));
        Assert.Equal(["p-0001", "p-0002"], loadedDuplicate.Paragraphs.Select(paragraph => paragraph.Id));
        Assert.Equal(duplicate.Id, duplicateMetadata.DocumentId);
        Assert.Equal("복제할 시놉시스", duplicateMetadata.Synopsis);
        Assert.Equal(SceneStatus.Revising, duplicateMetadata.Status);
        Assert.Equal("자료", duplicateMetadata.FileCategory);
        Assert.Equal(["태그A", "태그B"], duplicateMetadata.Tags);
        Assert.Equal(4200, duplicateMetadata.TargetCharacterCount);
    }

    [Fact]
    public async Task DeleteDocumentMovesFilesToTrashAndRemovesManifestEntryAndSearchIndex()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = new WriterDocument(
            "scene-0002",
            "Second",
            [new WriterParagraph("p-0001", "needle text", "body", [], [])]);
        await store.SaveDocumentAsync(second, CancellationToken.None);
        await new SceneMetadataStore(paths).SaveAsync(
            new SceneMetadata(1, second.Id, "삭제될 메타", SceneStatus.Excluded, ["삭제"], 900, DateTimeOffset.UtcNow),
            CancellationToken.None);

        var manifest = await store.DeleteDocumentAsync(second.Id, CancellationToken.None);
        var reloadedManifest = await new ProjectStore(paths).LoadManifestAsync(CancellationToken.None);
        var results = await store.SearchAsync("needle", CancellationToken.None);

        Assert.Equal([first.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal([first.Id], reloadedManifest.Documents.Select(document => document.Id));
        var trashFolders = Directory.GetDirectories(paths.TrashPath, $"{second.Id}-*");
        var trashFolder = Assert.Single(trashFolders);
        Assert.False(File.Exists(paths.DocumentJsonPath(second.Id)));
        Assert.False(File.Exists(paths.DocumentTextPath(second.Id)));
        Assert.False(File.Exists(paths.SceneMetadataPath(second.Id)));
        Assert.True(File.Exists(Path.Combine(trashFolder, $"{second.Id}.wwdoc.json")));
        Assert.True(File.Exists(Path.Combine(trashFolder, $"{second.Id}.txt")));
        Assert.True(File.Exists(Path.Combine(trashFolder, $"{second.Id}.meta.json")));
        Assert.True(File.Exists(Path.Combine(trashFolder, "trash.info.json")));
        Assert.Empty(results);
    }

    [Fact]
    public async Task RestoreTrashedDocumentRecreatesFilesManifestMetadataAndSearchIndex()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = new WriterDocument(
            "scene-0002",
            "Second",
            [new WriterParagraph("p-0001", "복원할 한글 본문 needle", "body", [], [])]);
        await store.SaveDocumentAsync(second, CancellationToken.None);
        await new SceneMetadataStore(paths).SaveAsync(
            new SceneMetadata(1, second.Id, "휴지통 복원 메타", SceneStatus.Revising, ["복원"], 900, DateTimeOffset.UtcNow),
            CancellationToken.None);
        await store.DeleteDocumentAsync(second.Id, CancellationToken.None);

        var trashItem = Assert.Single(await store.ListTrashedDocumentsAsync(CancellationToken.None));
        var restored = await store.RestoreTrashedDocumentAsync(trashItem.TrashId, CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);
        var loaded = await store.LoadDocumentAsync(second.Id, CancellationToken.None);
        var metadata = await new SceneMetadataStore(paths).LoadAsync(second.Id, CancellationToken.None);
        var results = await store.SearchAsync("needle", CancellationToken.None);

        Assert.Equal(second.Id, restored.Id);
        Assert.Equal([first.Id, second.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal("복원할 한글 본문 needle", loaded.Paragraphs[0].Text);
        Assert.Equal("휴지통 복원 메타", metadata.Synopsis);
        Assert.Equal(SceneStatus.Revising, metadata.Status);
        Assert.Equal(["복원"], metadata.Tags);
        Assert.True(File.Exists(paths.DocumentJsonPath(second.Id)));
        Assert.True(File.Exists(paths.DocumentTextPath(second.Id)));
        Assert.True(File.Exists(paths.SceneMetadataPath(second.Id)));
        Assert.Empty(await store.ListTrashedDocumentsAsync(CancellationToken.None));
        Assert.Single(results);
        Assert.Equal(second.Id, results[0].DocumentId);
    }

    [Fact]
    public async Task DeleteDocumentKeepsAtLeastOneScene()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new ProjectStore(ProjectPaths.ForRoot(root));
        var document = await store.CreateDocumentAsync("Only", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteDocumentAsync(document.Id, CancellationToken.None));
    }
}
