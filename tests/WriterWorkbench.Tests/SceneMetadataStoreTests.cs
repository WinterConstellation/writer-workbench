using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class SceneMetadataStoreTests
{
    [Fact]
    public async Task LoadCreatesDefaultMetadataWhenMissing()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new SceneMetadataStore(paths);

        var metadata = await store.LoadAsync("scene-0001", CancellationToken.None);

        Assert.Equal(SceneMetadata.CurrentSchemaVersion, metadata.SchemaVersion);
        Assert.Equal("scene-0001", metadata.DocumentId);
        Assert.Equal("", metadata.Synopsis);
        Assert.Equal("", metadata.Summary);
        Assert.Equal("원고", metadata.FileCategory);
        Assert.Equal(SceneStatus.Draft, metadata.Status);
        Assert.Empty(metadata.Tags);
        Assert.Null(metadata.TargetCharacterCount);
        Assert.Equal(0, metadata.ContentLength);
        Assert.Equal(0, metadata.ContentLengthWithSpaces);
        Assert.Equal("Scene", metadata.SceneType);
        Assert.False(metadata.ManualLineBreak);
        Assert.True(metadata.CreatedAt > DateTimeOffset.MinValue);
        Assert.True(metadata.UpdatedAt > DateTimeOffset.MinValue);
        Assert.True(File.Exists(paths.SceneMetadataPath("scene-0001")));
    }

    [Fact]
    public async Task LoadExistingOrDefaultDoesNotCreateMetadataWhenMissing()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new SceneMetadataStore(paths);

        var metadata = await store.LoadExistingOrDefaultAsync("scene-0001", CancellationToken.None);

        Assert.Equal("scene-0001", metadata.DocumentId);
        Assert.Equal(SceneStatus.Draft, metadata.Status);
        Assert.Equal("Scene", metadata.SceneType);
        Assert.Equal("원고", metadata.FileCategory);
        Assert.Equal(0, metadata.ContentLength);
        Assert.Equal(0, metadata.ContentLengthWithSpaces);
        Assert.False(File.Exists(paths.SceneMetadataPath("scene-0001")));
    }

    [Fact]
    public async Task LoadsExistingMetadataWithoutNewFieldsUsingDefaults()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        Directory.CreateDirectory(paths.DocumentsPath);
        await File.WriteAllTextAsync(
            paths.SceneMetadataPath("scene-old"),
            """
            {
              "SchemaVersion": 1,
              "DocumentId": "scene-old",
              "Synopsis": "legacy summary",
              "Status": "Revising",
              "Tags": ["old"],
              "TargetCharacterCount": 1200,
              "UpdatedAt": "2026-01-01T00:00:00+00:00"
            }
            """,
            CancellationToken.None);
        var store = new SceneMetadataStore(paths);

        var metadata = await store.LoadExistingOrDefaultAsync("scene-old", CancellationToken.None);

        Assert.Equal(SceneMetadata.CurrentSchemaVersion, metadata.SchemaVersion);
        Assert.Equal("scene-old", metadata.DocumentId);
        Assert.Equal("legacy summary", metadata.Synopsis);
        Assert.Equal("legacy summary", metadata.Summary);
        Assert.Equal(SceneStatus.Revising, metadata.Status);
        Assert.Equal(["old"], metadata.Tags);
        Assert.Equal(1200, metadata.TargetCharacterCount);
        Assert.Equal(0, metadata.ContentLength);
        Assert.Equal(0, metadata.ContentLengthWithSpaces);
        Assert.Equal("Scene", metadata.SceneType);
        Assert.Equal("원고", metadata.FileCategory);
        Assert.False(metadata.ManualLineBreak);
        Assert.True(metadata.CreatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task SavesAndLoadsStrengthenedSceneMetadataFields()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new SceneMetadataStore(paths);
        var createdAt = DateTimeOffset.Parse("2026-01-02T03:04:05+00:00");
        var updatedAt = DateTimeOffset.Parse("2026-01-03T04:05:06+00:00");
        var metadata = new SceneMetadata(
            SceneMetadata.CurrentSchemaVersion,
            "scene-0002",
            "",
            SceneStatus.UploadPending,
            ["단서", "1막"],
            3500,
            updatedAt,
            ContentLength: 2310,
            ContentLengthWithSpaces: 2501,
            SceneType: "Episode",
            ManualLineBreak: true,
            CreatedAt: createdAt,
            Summary: "주인공이 첫 단서를 발견한다.",
            FileCategory: "시놉시스");

        await store.SaveAsync(metadata, CancellationToken.None);
        var loaded = await store.LoadAsync("scene-0002", CancellationToken.None);
        var rawJson = await File.ReadAllTextAsync(paths.SceneMetadataPath("scene-0002"), CancellationToken.None);

        Assert.Equal("주인공이 첫 단서를 발견한다.", loaded.Synopsis);
        Assert.Equal("주인공이 첫 단서를 발견한다.", loaded.Summary);
        Assert.Equal(SceneStatus.UploadPending, loaded.Status);
        Assert.Equal("시놉시스", loaded.FileCategory);
        Assert.Equal(["단서", "1막"], loaded.Tags);
        Assert.Equal(3500, loaded.TargetCharacterCount);
        Assert.Equal(2310, loaded.ContentLength);
        Assert.Equal(2501, loaded.ContentLengthWithSpaces);
        Assert.Equal("Episode", loaded.SceneType);
        Assert.True(loaded.ManualLineBreak);
        Assert.Equal(createdAt, loaded.CreatedAt);
        Assert.Equal(updatedAt, loaded.UpdatedAt);
        Assert.Contains("\"Status\": \"UploadPending\"", rawJson);
        Assert.Contains("\"FileCategory\": \"시놉시스\"", rawJson);
        Assert.Contains("\"ContentLength\": 2310", rawJson);
        Assert.Contains("\"ContentLengthWithSpaces\": 2501", rawJson);
        Assert.Contains("\"ManualLineBreak\": true", rawJson);
    }

    [Fact]
    public async Task LegacyFinalStatusNormalizesToRevisionComplete()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        Directory.CreateDirectory(paths.DocumentsPath);
        await File.WriteAllTextAsync(
            paths.SceneMetadataPath("scene-final"),
            """
            {
              "SchemaVersion": 2,
              "DocumentId": "scene-final",
              "Synopsis": "legacy final",
              "Status": "Final",
              "Tags": [],
              "TargetCharacterCount": null,
              "UpdatedAt": "2026-01-01T00:00:00+00:00"
            }
            """,
            CancellationToken.None);
        var store = new SceneMetadataStore(paths);

        var metadata = await store.LoadExistingOrDefaultAsync("scene-final", CancellationToken.None);

        Assert.Equal(SceneStatus.RevisionComplete, metadata.Status);
        Assert.Equal("원고", metadata.FileCategory);
    }

    [Fact]
    public async Task MetadataSaveDoesNotRewriteDocumentBodyFiles()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        Directory.CreateDirectory(paths.DocumentsPath);
        await File.WriteAllTextAsync(paths.DocumentJsonPath("scene-0003"), "{\"id\":\"scene-0003\"}");
        await File.WriteAllTextAsync(paths.DocumentTextPath("scene-0003"), "본문");
        var fixedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(paths.DocumentJsonPath("scene-0003"), fixedTime);
        File.SetLastWriteTimeUtc(paths.DocumentTextPath("scene-0003"), fixedTime);
        var store = new SceneMetadataStore(paths);

        await store.SaveAsync(
            new SceneMetadata(
                SceneMetadata.CurrentSchemaVersion,
                "scene-0003",
                "",
                SceneStatus.RevisionComplete,
                ["완료"],
                1200,
                DateTimeOffset.UtcNow,
                ContentLength: 10,
                ContentLengthWithSpaces: 12,
                Summary: "본문 파일은 그대로 둔다."),
            CancellationToken.None);

        Assert.Equal(fixedTime, File.GetLastWriteTimeUtc(paths.DocumentJsonPath("scene-0003")));
        Assert.Equal(fixedTime, File.GetLastWriteTimeUtc(paths.DocumentTextPath("scene-0003")));
    }

    [Fact]
    public async Task DeleteRemovesMetadataFileWhenPresent()
    {
        var root = TestRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new SceneMetadataStore(paths);
        await store.SaveAsync(SceneMetadata.CreateDefault("scene-0004"), CancellationToken.None);

        store.Delete("scene-0004");

        Assert.False(File.Exists(paths.SceneMetadataPath("scene-0004")));
    }

    private static string TestRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
