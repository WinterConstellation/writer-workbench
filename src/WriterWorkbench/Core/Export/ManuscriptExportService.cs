using System.IO;
using System.Text;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.Export;

public sealed class ManuscriptExportService(
    ProjectPaths paths,
    ProjectStore projectStore,
    SceneMetadataStore metadataStore)
{
    public async Task<ManuscriptExportResult> ExportCurrentSceneAsync(string documentId, CancellationToken token)
    {
        var document = await projectStore.LoadDocumentAsync(documentId, token);
        var outputPath = ExportPath($"{SanitizeFileName(document.Id)}-{SanitizeFileName(document.Title)}.txt");
        await WriteUtf8Async(outputPath, TextExportService.ToPlainText(document), token);
        return new ManuscriptExportResult(ManuscriptExportKind.CurrentScene, outputPath, 1);
    }

    public async Task<ManuscriptExportResult> ExportFullManuscriptAsync(CancellationToken token)
    {
        var manifest = await projectStore.LoadManifestAsync(token);
        var sections = new List<string>();

        foreach (var documentInfo in manifest.Documents)
        {
            var metadata = await metadataStore.LoadExistingOrDefaultAsync(documentInfo.Id, token);
            if (metadata.Status == SceneStatus.Excluded)
            {
                continue;
            }

            var document = await projectStore.LoadDocumentAsync(documentInfo.Id, token);
            sections.Add(TextExportService.ToPlainText(document));
        }

        if (sections.Count == 0)
        {
            throw new InvalidOperationException("No included scene is available for export.");
        }

        var outputPath = ExportPath($"{SanitizeFileName(manifest.Title)}-full.txt");
        await WriteUtf8Async(outputPath, string.Join(Environment.NewLine + Environment.NewLine, sections), token);
        return new ManuscriptExportResult(ManuscriptExportKind.FullManuscript, outputPath, sections.Count);
    }

    private string ExportPath(string fileName)
    {
        Directory.CreateDirectory(paths.ExportsPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        return Path.Combine(paths.ExportsPath, $"{timestamp}-{uniqueSuffix}-{fileName}");
    }

    private static async Task WriteUtf8Async(string path, string content, CancellationToken token)
    {
        var tempPath = path + ".tmp";
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

        File.Move(tempPath, path, overwrite: true);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalid.Contains(character) ? '_' : character)
            .ToArray())
            .Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "manuscript" : sanitized;
    }
}
