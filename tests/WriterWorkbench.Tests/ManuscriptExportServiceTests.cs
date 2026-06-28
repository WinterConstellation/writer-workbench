using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Export;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class ManuscriptExportServiceTests
{
    [Fact]
    public async Task ExportCurrentSceneWritesUtf8Txt()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "현재 장면",
            [new WriterParagraph("p-0001", "한국어 본문입니다.", "body", [], [])]);
        await store.SaveDocumentAsync(document, CancellationToken.None);
        var service = new ManuscriptExportService(paths, store, new SceneMetadataStore(paths));

        var result = await service.ExportCurrentSceneAsync(document.Id, CancellationToken.None);
        var text = await File.ReadAllTextAsync(result.OutputPath, CancellationToken.None);
        var bytes = await File.ReadAllBytesAsync(result.OutputPath, CancellationToken.None);

        Assert.Equal(ManuscriptExportKind.CurrentScene, result.Kind);
        Assert.True(File.Exists(result.OutputPath));
        Assert.StartsWith(paths.ExportsPath, result.OutputPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("한국어 본문입니다.", text);
        Assert.Equal("한국어 본문입니다.", System.Text.Encoding.UTF8.GetString(bytes));
        Assert.Equal(1, result.IncludedSceneCount);
        Assert.Equal(0, result.ExcludedSceneCount);
        Assert.Equal(text.Length, result.CharacterCount);
    }

    [Fact]
    public async Task ExportFullManuscriptUsesBinderOrderAndSkipsExcludedScenes()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var first = await store.CreateDocumentAsync("First", CancellationToken.None);
        var second = await store.CreateDocumentAsync("Second", CancellationToken.None);
        var third = await store.CreateDocumentAsync("Third", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(first.Id, "첫 장면", "첫 본문"), CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(second.Id, "둘째 장면", "둘째 본문"), CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(third.Id, "셋째 장면", "셋째 본문"), CancellationToken.None);
        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);
        await store.MoveDocumentAsync(third.Id, -1, CancellationToken.None);
        var metadataStore = new SceneMetadataStore(paths);
        await metadataStore.SaveAsync(
            new SceneMetadata(1, second.Id, "", SceneStatus.Excluded, [], null, DateTimeOffset.UtcNow),
            CancellationToken.None);
        var service = new ManuscriptExportService(paths, store, metadataStore);

        var result = await service.ExportFullManuscriptAsync(CancellationToken.None);
        var text = await File.ReadAllTextAsync(result.OutputPath, CancellationToken.None);

        Assert.Equal(ManuscriptExportKind.FullManuscript, result.Kind);
        Assert.Equal(2, result.IncludedSceneCount);
        Assert.Equal(1, result.ExcludedSceneCount);
        Assert.Equal(text.Length, result.CharacterCount);
        Assert.Contains("# 셋째 장면", text);
        Assert.Contains("셋째 본문", text);
        Assert.Contains("# 첫 장면", text);
        Assert.Contains("첫 본문", text);
        Assert.DoesNotContain("둘째 본문", text);
        Assert.True(text.IndexOf("# 셋째 장면", StringComparison.Ordinal) < text.IndexOf("# 첫 장면", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportCreatesExportsDirectoryWhenMissing()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = await store.CreateDocumentAsync("장면", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(document.Id, "본문"), CancellationToken.None);
        if (Directory.Exists(paths.ExportsPath))
        {
            Directory.Delete(paths.ExportsPath, recursive: true);
        }
        var service = new ManuscriptExportService(paths, store, new SceneMetadataStore(paths));

        var result = await service.ExportCurrentSceneAsync(document.Id, CancellationToken.None);

        Assert.True(Directory.Exists(paths.ExportsPath));
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public async Task ExportCurrentSceneDoesNotOverwriteRepeatedExports()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = await store.CreateDocumentAsync("Repeat", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(document.Id, "반복 내보내기"), CancellationToken.None);
        var service = new ManuscriptExportService(paths, store, new SceneMetadataStore(paths));

        var first = await service.ExportCurrentSceneAsync(document.Id, CancellationToken.None);
        var second = await service.ExportCurrentSceneAsync(document.Id, CancellationToken.None);
        var third = await service.ExportCurrentSceneAsync(document.Id, CancellationToken.None);
        var exportedFiles = Directory.GetFiles(paths.ExportsPath, "*.txt");

        Assert.NotEqual(first.OutputPath, second.OutputPath);
        Assert.NotEqual(first.OutputPath, third.OutputPath);
        Assert.NotEqual(second.OutputPath, third.OutputPath);
        Assert.Equal(3, exportedFiles.Length);
        Assert.True(File.Exists(first.OutputPath));
        Assert.True(File.Exists(second.OutputPath));
        Assert.True(File.Exists(third.OutputPath));
    }

    [Fact]
    public async Task ExportFullManuscriptThrowsWhenNoIncludedScenesExist()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = await store.CreateDocumentAsync("Excluded", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(document.Id, "빠질 본문"), CancellationToken.None);
        var metadataStore = new SceneMetadataStore(paths);
        await metadataStore.SaveAsync(
            new SceneMetadata(1, document.Id, "", SceneStatus.Excluded, [], null, DateTimeOffset.UtcNow),
            CancellationToken.None);
        var service = new ManuscriptExportService(paths, store, metadataStore);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportFullManuscriptAsync(CancellationToken.None));

        Assert.Contains("included scene", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportFullManuscriptThrowsWhenProjectManifestHasNoScenes()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        Directory.CreateDirectory(paths.RootPath);
        await File.WriteAllTextAsync(
            paths.ManifestPath,
            """
            {
              "SchemaVersion": 1,
              "Title": "Empty",
              "Documents": []
            }
            """,
            CancellationToken.None);
        var service = new ManuscriptExportService(paths, new ProjectStore(paths), new SceneMetadataStore(paths));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportFullManuscriptAsync(CancellationToken.None));

        Assert.Contains("included scene", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportDoesNotMutateDocumentBodyOrMetadata()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = await store.CreateDocumentAsync("Stable", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(document.Id, "안정 본문"), CancellationToken.None);
        var metadataStore = new SceneMetadataStore(paths);
        await metadataStore.SaveAsync(
            new SceneMetadata(1, document.Id, "시놉시스", SceneStatus.RevisionComplete, ["보존"], 1200, DateTimeOffset.UtcNow),
            CancellationToken.None);
        var jsonBefore = await File.ReadAllTextAsync(paths.DocumentJsonPath(document.Id), CancellationToken.None);
        var txtBefore = await File.ReadAllTextAsync(paths.DocumentTextPath(document.Id), CancellationToken.None);
        var metaBefore = await File.ReadAllTextAsync(paths.SceneMetadataPath(document.Id), CancellationToken.None);
        var service = new ManuscriptExportService(paths, store, metadataStore);

        await service.ExportFullManuscriptAsync(CancellationToken.None);

        Assert.Equal(jsonBefore, await File.ReadAllTextAsync(paths.DocumentJsonPath(document.Id), CancellationToken.None));
        Assert.Equal(txtBefore, await File.ReadAllTextAsync(paths.DocumentTextPath(document.Id), CancellationToken.None));
        Assert.Equal(metaBefore, await File.ReadAllTextAsync(paths.SceneMetadataPath(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task ExportFullManuscriptDoesNotCreateMissingMetadataFile()
    {
        var root = CreateRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = await store.CreateDocumentAsync("No Metadata", CancellationToken.None);
        await store.SaveDocumentAsync(CreateDocument(document.Id, "메타 없음 본문"), CancellationToken.None);
        File.Delete(paths.SceneMetadataPath(document.Id));
        var service = new ManuscriptExportService(paths, store, new SceneMetadataStore(paths));

        var result = await service.ExportFullManuscriptAsync(CancellationToken.None);
        var text = await File.ReadAllTextAsync(result.OutputPath, CancellationToken.None);

        Assert.Contains("메타 없음 본문", text);
        Assert.False(File.Exists(paths.SceneMetadataPath(document.Id)));
    }

    private static string CreateRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }

    private static WriterDocument CreateDocument(string id, string text)
    {
        return CreateDocument(id, id, text);
    }

    private static WriterDocument CreateDocument(string id, string title, string text)
    {
        return new WriterDocument(
            id,
            title,
            [new WriterParagraph("p-0001", text, "body", [], [])]);
    }
}
