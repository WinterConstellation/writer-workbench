using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class SceneMetadataStoreTests
{
    [Fact]
    public async Task LoadCreatesDefaultMetadataWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new SceneMetadataStore(paths);

        var metadata = await store.LoadAsync("scene-0001", CancellationToken.None);

        Assert.Equal(1, metadata.SchemaVersion);
        Assert.Equal("scene-0001", metadata.DocumentId);
        Assert.Equal("", metadata.Synopsis);
        Assert.Equal(SceneStatus.Draft, metadata.Status);
        Assert.Empty(metadata.Tags);
        Assert.Null(metadata.TargetCharacterCount);
        Assert.True(File.Exists(paths.SceneMetadataPath("scene-0001")));
    }

    [Fact]
    public async Task SavesAndLoadsSynopsisStatusTagsAndTargetCharacterCount()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new SceneMetadataStore(ProjectPaths.ForRoot(root));
        var metadata = new SceneMetadata(
            1,
            "scene-0002",
            "주인공이 첫 단서를 발견한다.",
            SceneStatus.Revising,
            ["단서", "1막"],
            3500,
            DateTimeOffset.UtcNow);

        await store.SaveAsync(metadata, CancellationToken.None);
        var loaded = await store.LoadAsync("scene-0002", CancellationToken.None);
        var rawJson = await File.ReadAllTextAsync(ProjectPaths.ForRoot(root).SceneMetadataPath("scene-0002"), CancellationToken.None);

        Assert.Equal("주인공이 첫 단서를 발견한다.", loaded.Synopsis);
        Assert.Equal(SceneStatus.Revising, loaded.Status);
        Assert.Equal(["단서", "1막"], loaded.Tags);
        Assert.Equal(3500, loaded.TargetCharacterCount);
        Assert.Contains("\"Status\": \"Revising\"", rawJson);
    }

    [Fact]
    public async Task MetadataSaveDoesNotRewriteDocumentBodyFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        Directory.CreateDirectory(paths.DocumentsPath);
        await File.WriteAllTextAsync(paths.DocumentJsonPath("scene-0003"), "{\"id\":\"scene-0003\"}");
        await File.WriteAllTextAsync(paths.DocumentTextPath("scene-0003"), "본문");
        var fixedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(paths.DocumentJsonPath("scene-0003"), fixedTime);
        File.SetLastWriteTimeUtc(paths.DocumentTextPath("scene-0003"), fixedTime);
        var store = new SceneMetadataStore(paths);

        await store.SaveAsync(
            new SceneMetadata(1, "scene-0003", "시놉시스", SceneStatus.Final, ["완료"], 1200, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(fixedTime, File.GetLastWriteTimeUtc(paths.DocumentJsonPath("scene-0003")));
        Assert.Equal(fixedTime, File.GetLastWriteTimeUtc(paths.DocumentTextPath("scene-0003")));
    }

    [Fact]
    public async Task DeleteRemovesMetadataFileWhenPresent()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new SceneMetadataStore(paths);
        await store.SaveAsync(SceneMetadata.CreateDefault("scene-0004"), CancellationToken.None);

        store.Delete("scene-0004");

        Assert.False(File.Exists(paths.SceneMetadataPath("scene-0004")));
    }
}
