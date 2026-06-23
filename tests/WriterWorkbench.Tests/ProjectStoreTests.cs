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
        var store = new ProjectStore(ProjectPaths.ForRoot(root));

        var first = await store.CreateDocumentAsync("새 장면", CancellationToken.None);
        var second = await store.CreateDocumentAsync("두 번째 장면", CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);
        var loadedSecond = await store.LoadDocumentAsync(second.Id, CancellationToken.None);

        Assert.Equal("scene-0001", first.Id);
        Assert.Equal("scene-0002", second.Id);
        Assert.Equal("두 번째 장면", second.Title);
        Assert.Equal("두 번째 장면", loadedSecond.Title);
        Assert.Equal([first.Id, second.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal("두 번째 장면", manifest.Documents.Single(document => document.Id == second.Id).Title);
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
        var after = await store.CreateDocumentAsync("After", CancellationToken.None);

        var duplicate = await store.DuplicateDocumentAsync(source.Id, CancellationToken.None);
        var manifest = await store.LoadManifestAsync(CancellationToken.None);
        var reloadedManifest = await new ProjectStore(ProjectPaths.ForRoot(root)).LoadManifestAsync(CancellationToken.None);
        var loadedDuplicate = await store.LoadDocumentAsync(duplicate.Id, CancellationToken.None);

        Assert.Equal([source.Id, duplicate.Id, after.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal([source.Id, duplicate.Id, after.Id], reloadedManifest.Documents.Select(document => document.Id));
        Assert.Equal("Source 복사", duplicate.Title);
        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal(source.Paragraphs.Select(paragraph => paragraph.Text), loadedDuplicate.Paragraphs.Select(paragraph => paragraph.Text));
        Assert.Equal(["p-0001", "p-0002"], loadedDuplicate.Paragraphs.Select(paragraph => paragraph.Id));
    }

    [Fact]
    public async Task DeleteDocumentRemovesFilesManifestEntryAndSearchIndex()
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

        var manifest = await store.DeleteDocumentAsync(second.Id, CancellationToken.None);
        var reloadedManifest = await new ProjectStore(paths).LoadManifestAsync(CancellationToken.None);
        var results = await store.SearchAsync("needle", CancellationToken.None);

        Assert.Equal([first.Id], manifest.Documents.Select(document => document.Id));
        Assert.Equal([first.Id], reloadedManifest.Documents.Select(document => document.Id));
        Assert.False(File.Exists(paths.DocumentJsonPath(second.Id)));
        Assert.False(File.Exists(paths.DocumentTextPath(second.Id)));
        Assert.Empty(results);
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
