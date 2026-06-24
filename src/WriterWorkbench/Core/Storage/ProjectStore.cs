using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Core.Storage;

public sealed class ProjectStore(ProjectPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<ProjectManifest> CreateProjectAsync(string title, CancellationToken token)
    {
        Directory.CreateDirectory(paths.RootPath);
        Directory.CreateDirectory(paths.DocumentsPath);

        if (File.Exists(paths.ManifestPath))
        {
            return await LoadManifestAsync(token);
        }

        var emptyManifest = new ProjectManifest(1, title, []);
        await SaveManifestAsync(emptyManifest, token);

        var initialDocument = new WriterDocument(
            "scene-0001",
            "첫 장면",
            [new WriterParagraph("p-0001", "여기에 원고를 씁니다.", "body", [], [])]);
        await SaveDocumentAsync(initialDocument, token);

        return await LoadManifestAsync(token);
    }

    public async Task<WriterDocument> CreateDocumentAsync(string title, CancellationToken token)
    {
        var manifest = await LoadManifestOrDefaultAsync(token);
        var nextNumber = manifest.Documents
            .Select(document => document.Id)
            .Select(ExtractSceneNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var id = $"scene-{nextNumber:0000}";
        var document = new WriterDocument(id, title, [new WriterParagraph("p-0001", "", "body", [], [])]);
        await SaveDocumentAsync(document, token);
        return document;
    }

    public async Task<WriterDocument> RenameDocumentAsync(string documentId, string title, CancellationToken token)
    {
        var normalizedTitle = title.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new ArgumentException("Scene title cannot be empty.", nameof(title));
        }

        var manifest = await LoadManifestAsync(token);
        if (FindDocumentIndex(manifest, documentId) < 0)
        {
            throw new KeyNotFoundException($"Document is not in the binder: {documentId}");
        }

        var document = await LoadDocumentAsync(documentId, token);
        var renamed = document with { Title = normalizedTitle };
        await SaveDocumentAsync(renamed, token);
        return renamed;
    }

    public async Task SaveDocumentAsync(WriterDocument document, CancellationToken token)
    {
        Directory.CreateDirectory(paths.RootPath);
        Directory.CreateDirectory(paths.DocumentsPath);

        var manifest = await LoadManifestOrDefaultAsync(token);
        var documentInfo = await WriteDocumentAsync(document, token);
        await UpdateMetadataForDocumentAsync(document, documentInfo.UpdatedAt, token);
        var existingIndex = FindDocumentIndex(manifest, document.Id);
        var documents = manifest.Documents.ToList();
        if (existingIndex >= 0)
        {
            documents[existingIndex] = documentInfo;
        }
        else
        {
            documents.Add(documentInfo);
        }

        await SaveManifestAsync(manifest with { Documents = documents }, token);
    }

    public async Task<WriterDocument> DuplicateDocumentAsync(string documentId, CancellationToken token)
    {
        Directory.CreateDirectory(paths.RootPath);
        Directory.CreateDirectory(paths.DocumentsPath);

        var manifest = await LoadManifestAsync(token);
        var sourceIndex = FindDocumentIndex(manifest, documentId);
        if (sourceIndex < 0)
        {
            throw new KeyNotFoundException($"Document is not in the binder: {documentId}");
        }

        var source = await LoadDocumentAsync(documentId, token);
        var duplicate = source with
        {
            Id = NextSceneId(manifest),
            Title = $"{source.Title} 복사",
            Paragraphs = source.Paragraphs
                .Select((paragraph, index) => paragraph with { Id = $"p-{index + 1:0000}" })
                .ToList()
        };
        var duplicateInfo = await WriteDocumentAsync(duplicate, token);
        await CopyMetadataAsync(source.Id, duplicate.Id, token);
        await UpdateMetadataForDocumentAsync(duplicate, duplicateInfo.UpdatedAt, token);
        var documents = manifest.Documents.ToList();
        documents.Insert(sourceIndex + 1, duplicateInfo);

        await SaveManifestAsync(manifest with { Documents = documents }, token);
        return duplicate;
    }

    public async Task<ProjectManifest> DeleteDocumentAsync(string documentId, CancellationToken token)
    {
        var manifest = await LoadManifestAsync(token);
        var index = FindDocumentIndex(manifest, documentId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Document is not in the binder: {documentId}");
        }

        if (manifest.Documents.Count <= 1)
        {
            throw new InvalidOperationException("A project must keep at least one scene.");
        }

        var documents = manifest.Documents.ToList();
        documents.RemoveAt(index);
        var updatedManifest = manifest with { Documents = documents };
        await SaveManifestAsync(updatedManifest, token);

        DeleteFileIfExists(paths.DocumentJsonPath(documentId));
        DeleteFileIfExists(paths.DocumentTextPath(documentId));
        new SceneMetadataStore(paths).Delete(documentId);
        var indexStore = new SqliteProjectIndex(paths.ProjectDatabasePath);
        await indexStore.DeleteDocumentAsync(documentId, token);

        return updatedManifest;
    }

    public async Task<ProjectManifest> MoveDocumentAsync(string documentId, int offset, CancellationToken token)
    {
        if (offset == 0)
        {
            return await LoadManifestAsync(token);
        }

        var manifest = await LoadManifestAsync(token);
        var currentIndex = FindDocumentIndex(manifest, documentId);
        if (currentIndex < 0)
        {
            throw new KeyNotFoundException($"Document is not in the binder: {documentId}");
        }

        var targetIndex = Math.Clamp(currentIndex + offset, 0, manifest.Documents.Count - 1);
        if (targetIndex == currentIndex)
        {
            return manifest;
        }

        var documents = manifest.Documents.ToList();
        var document = documents[currentIndex];
        documents.RemoveAt(currentIndex);
        documents.Insert(targetIndex, document);

        var updatedManifest = manifest with { Documents = documents };
        await SaveManifestAsync(updatedManifest, token);
        return updatedManifest;
    }

    public async Task<ProjectManifest> LoadManifestAsync(CancellationToken token)
    {
        await using var stream = File.OpenRead(paths.ManifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<ProjectManifest>(stream, JsonOptions, token);
        return manifest ?? throw new InvalidDataException("Project manifest is empty.");
    }

    public async Task<WriterDocument> LoadDocumentAsync(string documentId, CancellationToken token)
    {
        await using var stream = File.OpenRead(paths.DocumentJsonPath(documentId));
        var document = await JsonSerializer.DeserializeAsync<WriterDocument>(stream, JsonOptions, token);
        return StarterDocumentRepairService.RepairIfEmpty(
            document ?? throw new InvalidDataException($"Document is empty: {documentId}"));
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken token)
    {
        var index = new SqliteProjectIndex(paths.ProjectDatabasePath);
        return await index.SearchAsync(query, token);
    }

    private async Task<ProjectManifest> LoadManifestOrDefaultAsync(CancellationToken token)
    {
        if (File.Exists(paths.ManifestPath))
        {
            return await LoadManifestAsync(token);
        }

        return new ProjectManifest(1, "Untitled", []);
    }

    private async Task SaveManifestAsync(ProjectManifest manifest, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await WriteUtf8AtomicAsync(paths.ManifestPath, json, token);
    }

    private async Task<ProjectDocumentInfo> WriteDocumentAsync(WriterDocument document, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var plainText = TextExportService.ToPlainText(document);
        var updatedAt = DateTimeOffset.UtcNow;

        await WriteUtf8AtomicAsync(paths.DocumentJsonPath(document.Id), json, token);
        await WriteUtf8AtomicAsync(paths.DocumentTextPath(document.Id), plainText, token);

        var index = new SqliteProjectIndex(paths.ProjectDatabasePath);
        await index.UpsertDocumentAsync(document, plainText, updatedAt, token);

        return new ProjectDocumentInfo(
            document.Id,
            document.Title,
            Path.GetRelativePath(paths.RootPath, paths.DocumentJsonPath(document.Id)),
            Path.GetRelativePath(paths.RootPath, paths.DocumentTextPath(document.Id)),
            updatedAt);
    }

    private static async Task WriteUtf8AtomicAsync(string targetPath, string content, CancellationToken token)
    {
        var tempPath = targetPath + ".tmp";

        await using (var stream = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 64 * 1024,
                         useAsync: true))
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            await stream.WriteAsync(bytes, token);
            await stream.FlushAsync(token);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }

    private static int ExtractSceneNumber(string id)
    {
        var suffix = id.StartsWith("scene-", StringComparison.OrdinalIgnoreCase)
            ? id["scene-".Length..]
            : id;
        return int.TryParse(suffix, out var number) ? number : 0;
    }

    private static int FindDocumentIndex(ProjectManifest manifest, string documentId)
    {
        for (var index = 0; index < manifest.Documents.Count; index++)
        {
            if (string.Equals(manifest.Documents[index].Id, documentId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static string NextSceneId(ProjectManifest manifest)
    {
        var nextNumber = manifest.Documents
            .Select(document => document.Id)
            .Select(ExtractSceneNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return $"scene-{nextNumber:0000}";
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private async Task UpdateMetadataForDocumentAsync(WriterDocument document, DateTimeOffset updatedAt, CancellationToken token)
    {
        var metadataStore = new SceneMetadataStore(paths);
        await metadataStore.UpdateDerivedAsync(document, updatedAt, token);
    }

    private async Task CopyMetadataAsync(string sourceDocumentId, string targetDocumentId, CancellationToken token)
    {
        var metadataStore = new SceneMetadataStore(paths);
        var sourceMetadata = await metadataStore.LoadAsync(sourceDocumentId, token);
        await metadataStore.SaveAsync(sourceMetadata.CopyForDocument(targetDocumentId), token);
    }
}
