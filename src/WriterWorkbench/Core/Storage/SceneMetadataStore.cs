using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Core.Storage;

public sealed class SceneMetadataStore(ProjectPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<SceneMetadata> LoadAsync(string documentId, CancellationToken token)
    {
        Directory.CreateDirectory(paths.DocumentsPath);
        var path = paths.SceneMetadataPath(documentId);
        if (!File.Exists(path))
        {
            var metadata = SceneMetadata.CreateDefault(documentId);
            await SaveAsync(metadata, token);
            return metadata;
        }

        return await LoadExistingOrDefaultAsync(documentId, token);
    }

    public async Task<SceneMetadata> LoadExistingOrDefaultAsync(string documentId, CancellationToken token)
    {
        var path = paths.SceneMetadataPath(documentId);
        if (!File.Exists(path))
        {
            return SceneMetadata.CreateDefault(documentId);
        }

        await using var stream = File.OpenRead(path);
        var loaded = await JsonSerializer.DeserializeAsync<SceneMetadata>(stream, JsonOptions, token);
        return Normalize(loaded ?? SceneMetadata.CreateDefault(documentId), documentId);
    }

    public async Task SaveAsync(SceneMetadata metadata, CancellationToken token)
    {
        Directory.CreateDirectory(paths.DocumentsPath);
        var normalized = Normalize(metadata, metadata.DocumentId) with
        {
            UpdatedAt = metadata.UpdatedAt == default ? DateTimeOffset.UtcNow : metadata.UpdatedAt,
            CreatedAt = metadata.CreatedAt == default ? DateTimeOffset.UtcNow : metadata.CreatedAt
        };
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        await WriteUtf8AtomicAsync(paths.SceneMetadataPath(normalized.DocumentId), json, token);
    }

    public async Task<SceneMetadata> UpdateDerivedAsync(WriterDocument document, DateTimeOffset updatedAt, CancellationToken token)
    {
        var metadata = await LoadExistingOrDefaultAsync(document.Id, token);
        var derived = metadata with
        {
            ContentLength = CountCharacters(document, includeWhitespace: false),
            ContentLengthWithSpaces = CountCharacters(document, includeWhitespace: true),
            UpdatedAt = updatedAt == default ? DateTimeOffset.UtcNow : updatedAt
        };

        await SaveAsync(derived, token);
        return derived;
    }

    public void Delete(string documentId)
    {
        var path = paths.SceneMetadataPath(documentId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static SceneMetadata Normalize(SceneMetadata metadata, string documentId)
    {
        var summary = string.IsNullOrWhiteSpace(metadata.Summary)
            ? metadata.Synopsis ?? ""
            : metadata.Summary.Trim();
        var now = DateTimeOffset.UtcNow;

        return metadata with
        {
            SchemaVersion = Math.Max(metadata.SchemaVersion, SceneMetadata.CurrentSchemaVersion),
            DocumentId = string.IsNullOrWhiteSpace(metadata.DocumentId) ? documentId : metadata.DocumentId,
            Synopsis = summary,
            Summary = summary,
            Status = NormalizeStatus(metadata.Status),
            Tags = (metadata.Tags ?? [])
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ContentLength = Math.Max(0, metadata.ContentLength),
            ContentLengthWithSpaces = Math.Max(0, metadata.ContentLengthWithSpaces),
            SceneType = string.IsNullOrWhiteSpace(metadata.SceneType) ? "Scene" : metadata.SceneType.Trim(),
            FileCategory = string.IsNullOrWhiteSpace(metadata.FileCategory) ? "원고" : metadata.FileCategory.Trim(),
            CreatedAt = metadata.CreatedAt == default ? now : metadata.CreatedAt,
            UpdatedAt = metadata.UpdatedAt == default ? now : metadata.UpdatedAt
        };
    }

    private static SceneStatus NormalizeStatus(SceneStatus status)
    {
        return status == SceneStatus.Final ? SceneStatus.RevisionComplete : status;
    }

    private static int CountCharacters(WriterDocument document, bool includeWhitespace)
    {
        var count = 0;
        foreach (var paragraph in document.Paragraphs)
        {
            foreach (var character in paragraph.Text)
            {
                if (includeWhitespace || !char.IsWhiteSpace(character))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static async Task WriteUtf8AtomicAsync(string targetPath, string content, CancellationToken token)
    {
        var tempPath = targetPath + ".tmp";
        await using (var stream = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 16 * 1024,
                         useAsync: true))
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            await stream.WriteAsync(bytes, token);
            await stream.FlushAsync(token);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }
}
