using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            UpdatedAt = metadata.UpdatedAt == default ? DateTimeOffset.UtcNow : metadata.UpdatedAt
        };
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        await WriteUtf8AtomicAsync(paths.SceneMetadataPath(normalized.DocumentId), json, token);
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
        return metadata with
        {
            SchemaVersion = metadata.SchemaVersion <= 0 ? 1 : metadata.SchemaVersion,
            DocumentId = string.IsNullOrWhiteSpace(metadata.DocumentId) ? documentId : metadata.DocumentId,
            Synopsis = metadata.Synopsis ?? "",
            Tags = metadata.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
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
