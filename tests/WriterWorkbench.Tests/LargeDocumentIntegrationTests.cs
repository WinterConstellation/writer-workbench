using System.Diagnostics;
using Microsoft.Data.Sqlite;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class LargeDocumentIntegrationTests
{
    [Fact]
    public async Task SavesLoadsAndSearchesLargeDocument()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchLargeTests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new ProjectStore(ProjectPaths.ForRoot(root));
            var paragraphs = Enumerable.Range(1, 15_000)
                .Select(index => new WriterParagraph(
                    $"p-{index:00000}",
                    $"대용량 원고 문단 {index:00000}. 반복 저장과 검색 검증을 위한 문장입니다.",
                    "body",
                    [],
                    []))
                .ToList();
            paragraphs[12_345] = paragraphs[12_345] with
            {
                Text = "대용량 원고 안에서 바늘서식을 찾는 문장입니다."
            };
            var document = new WriterDocument("scene-large", "Large", paragraphs);

            var stopwatch = Stopwatch.StartNew();
            await store.SaveDocumentAsync(document, CancellationToken.None);
            var loaded = await store.LoadDocumentAsync("scene-large", CancellationToken.None);
            var hits = await store.SearchAsync("바늘서식", CancellationToken.None);
            stopwatch.Stop();

            Assert.Equal(15_000, loaded.Paragraphs.Count);
            Assert.Contains("바늘서식", loaded.Paragraphs[12_345].Text);
            Assert.Single(hits);
            Assert.Equal("scene-large", hits[0].DocumentId);
            Assert.True(new FileInfo(Path.Combine(root, "documents", "scene-large.wwdoc.json")).Length > 1_000_000);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Large document flow took {stopwatch.Elapsed}.");
        }
        finally
        {
            await DeleteDirectoryWithRetryAsync(root);
        }
    }

    private static async Task DeleteDirectoryWithRetryAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 5)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
            }
        }

        SqliteConnection.ClearAllPools();
        Directory.Delete(path, recursive: true);
    }
}
