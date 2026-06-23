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

    public async Task SaveDocumentAsync(WriterDocument document, CancellationToken token)
    {
        Directory.CreateDirectory(paths.RootPath);
        Directory.CreateDirectory(paths.DocumentsPath);

        var json = JsonSerializer.Serialize(document, JsonOptions);
        var plainText = TextExportService.ToPlainText(document);
        var updatedAt = DateTimeOffset.UtcNow;

        await WriteUtf8AtomicAsync(paths.DocumentJsonPath(document.Id), json, token);
        await WriteUtf8AtomicAsync(paths.DocumentTextPath(document.Id), plainText, token);

        var manifest = await LoadManifestOrDefaultAsync(token);
        var documentInfo = new ProjectDocumentInfo(
            document.Id,
            document.Title,
            Path.GetRelativePath(paths.RootPath, paths.DocumentJsonPath(document.Id)),
            Path.GetRelativePath(paths.RootPath, paths.DocumentTextPath(document.Id)),
            updatedAt);
        var documents = manifest.Documents
            .Where(existing => !string.Equals(existing.Id, document.Id, StringComparison.OrdinalIgnoreCase))
            .Append(documentInfo)
            .OrderBy(existing => existing.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        await SaveManifestAsync(manifest with { Documents = documents }, token);

        var index = new SqliteProjectIndex(paths.ProjectDatabasePath);
        await index.UpsertDocumentAsync(document, plainText, updatedAt, token);
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
}
