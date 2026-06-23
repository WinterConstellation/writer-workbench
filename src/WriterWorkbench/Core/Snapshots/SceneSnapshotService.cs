using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.Snapshots;

public sealed class SceneSnapshotService(ProjectPaths paths, ProjectStore projectStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<SceneSnapshotInfo> CreateSnapshotAsync(
        string documentId,
        string reason,
        CancellationToken token)
    {
        var document = await projectStore.LoadDocumentAsync(documentId, token);
        var snapshotId = CreateSnapshotId();
        var snapshotPath = paths.SceneSnapshotPath(documentId, snapshotId);
        Directory.CreateDirectory(snapshotPath);

        File.Copy(paths.DocumentJsonPath(documentId), paths.SceneSnapshotDocumentJsonPath(documentId, snapshotId), overwrite: true);
        File.Copy(paths.DocumentTextPath(documentId), paths.SceneSnapshotDocumentTextPath(documentId, snapshotId), overwrite: true);
        if (File.Exists(paths.SceneMetadataPath(documentId)))
        {
            File.Copy(paths.SceneMetadataPath(documentId), paths.SceneSnapshotMetadataPath(documentId, snapshotId), overwrite: true);
        }

        var info = new SceneSnapshotInfo(
            1,
            snapshotId,
            documentId,
            document.Title,
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(reason) ? "manual" : reason.Trim(),
            snapshotPath,
            TextExportService.ToPlainText(document).Length);
        await WriteJsonAtomicAsync(paths.SceneSnapshotInfoPath(documentId, snapshotId), info, token);
        return info;
    }

    public async Task<IReadOnlyList<SceneSnapshotInfo>> ListSnapshotsAsync(string documentId, CancellationToken token)
    {
        var sceneSnapshotsPath = paths.SceneSnapshotsPath(documentId);
        if (!Directory.Exists(sceneSnapshotsPath))
        {
            return [];
        }

        var snapshots = new List<SceneSnapshotInfo>();
        foreach (var snapshotPath in Directory.EnumerateDirectories(sceneSnapshotsPath))
        {
            var snapshotId = Path.GetFileName(snapshotPath);
            var infoPath = paths.SceneSnapshotInfoPath(documentId, snapshotId);
            if (!File.Exists(infoPath))
            {
                continue;
            }

            await using var stream = File.OpenRead(infoPath);
            var info = await JsonSerializer.DeserializeAsync<SceneSnapshotInfo>(stream, JsonOptions, token);
            if (info is not null)
            {
                snapshots.Add(info with { FolderPath = snapshotPath });
            }
        }

        return snapshots
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ThenByDescending(snapshot => snapshot.SnapshotId, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<SceneSnapshotRestoreResult> RestoreSnapshotAsync(
        string documentId,
        string snapshotId,
        CancellationToken token)
    {
        var snapshotPath = paths.SceneSnapshotPath(documentId, snapshotId);
        if (!Directory.Exists(snapshotPath))
        {
            throw new DirectoryNotFoundException($"Snapshot was not found: {documentId}/{snapshotId}");
        }

        var snapshot = await LoadSnapshotInfoAsync(documentId, snapshotId, token);
        var documentJsonPath = paths.SceneSnapshotDocumentJsonPath(documentId, snapshotId);
        if (!File.Exists(documentJsonPath))
        {
            throw new FileNotFoundException("Snapshot document JSON is missing.", documentJsonPath);
        }

        await using var stream = File.OpenRead(documentJsonPath);
        var document = await JsonSerializer.DeserializeAsync<WriterDocument>(stream, JsonOptions, token)
            ?? throw new InvalidDataException($"Snapshot document is empty: {documentId}/{snapshotId}");
        if (!string.Equals(document.Id, documentId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Snapshot document id mismatch: {document.Id} != {documentId}");
        }

        await projectStore.SaveDocumentAsync(document, token);
        var snapshotMetadataPath = paths.SceneSnapshotMetadataPath(documentId, snapshotId);
        if (File.Exists(snapshotMetadataPath))
        {
            File.Copy(snapshotMetadataPath, paths.SceneMetadataPath(documentId), overwrite: true);
        }
        else if (File.Exists(paths.SceneMetadataPath(documentId)))
        {
            File.Delete(paths.SceneMetadataPath(documentId));
        }

        return new SceneSnapshotRestoreResult(snapshot, document.Id, document.Title);
    }

    public Task DeleteSnapshotAsync(string documentId, string snapshotId, CancellationToken token)
    {
        var snapshotPath = paths.SceneSnapshotPath(documentId, snapshotId);
        if (!Directory.Exists(snapshotPath))
        {
            throw new DirectoryNotFoundException($"Snapshot was not found: {documentId}/{snapshotId}");
        }

        Directory.Delete(snapshotPath, recursive: true);
        return Task.CompletedTask;
    }

    private async Task<SceneSnapshotInfo> LoadSnapshotInfoAsync(string documentId, string snapshotId, CancellationToken token)
    {
        var infoPath = paths.SceneSnapshotInfoPath(documentId, snapshotId);
        if (!File.Exists(infoPath))
        {
            throw new FileNotFoundException("Snapshot info is missing.", infoPath);
        }

        await using var stream = File.OpenRead(infoPath);
        return await JsonSerializer.DeserializeAsync<SceneSnapshotInfo>(stream, JsonOptions, token)
            ?? throw new InvalidDataException($"Snapshot info is empty: {documentId}/{snapshotId}");
    }

    private static string CreateSnapshotId()
    {
        return $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..26];
    }

    private static async Task WriteJsonAtomicAsync<T>(string targetPath, T value, CancellationToken token)
    {
        var tempPath = targetPath + ".tmp";
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await using (var stream = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 16 * 1024,
                         useAsync: true))
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
            await stream.WriteAsync(bytes, token);
            await stream.FlushAsync(token);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }
}
