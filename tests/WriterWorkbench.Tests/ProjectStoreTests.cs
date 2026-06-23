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
}
