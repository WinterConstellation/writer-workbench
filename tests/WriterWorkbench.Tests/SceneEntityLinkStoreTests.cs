using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;

namespace WriterWorkbench.Tests;

public sealed class SceneEntityLinkStoreTests
{
    [Fact]
    public async Task MissingSceneEntityLinkFileReturnsEmptyList()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new SceneEntityLinkStore(paths);

        var links = await store.LoadAsync(CancellationToken.None);

        Assert.Empty(links);
        Assert.False(File.Exists(paths.SceneEntityLinksPath));
    }

    [Fact]
    public async Task AddOrUpdateRejectsMissingEntityId()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new SceneEntityLinkStore(paths);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.AddOrUpdateAsync("scene-0001", "missing-entity", "appears", "", CancellationToken.None));

        Assert.Empty(await store.LoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AddOrUpdateRoundtripsKoreanText()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var storyStore = new StoryStructureStore(paths);
        var entity = await storyStore.AddEntityAsync(
            StoryEntityType.Character,
            "윤서",
            "주연",
            "첫 장면부터 등장",
            "#2563EB",
            ["주요"],
            CancellationToken.None);
        var store = new SceneEntityLinkStore(paths);

        await store.AddOrUpdateAsync("scene-0001", entity.Id, "등장", "감정선 확인", CancellationToken.None);

        var link = Assert.Single(await store.LoadForSceneAsync("scene-0001", CancellationToken.None));
        Assert.Equal("등장", link.Role);
        Assert.Equal("감정선 확인", link.Notes);
        Assert.Contains("윤서", await File.ReadAllTextAsync(paths.StoryEntitiesPath, CancellationToken.None));
        Assert.Contains("감정선 확인", await File.ReadAllTextAsync(paths.SceneEntityLinksPath, CancellationToken.None));
    }

    [Fact]
    public async Task AddOrUpdateUpsertsSceneEntityPair()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var storyStore = new StoryStructureStore(paths);
        var entity = await storyStore.AddEntityAsync(
            StoryEntityType.Character,
            "Character A",
            "Lead",
            "",
            "#2563EB",
            [],
            CancellationToken.None);
        var store = new SceneEntityLinkStore(paths);

        var first = await store.AddOrUpdateAsync("scene-0001", entity.Id, "appears", "first", CancellationToken.None);
        var second = await store.AddOrUpdateAsync("scene-0001", entity.Id, "mentioned", "second", CancellationToken.None);

        var link = Assert.Single(await store.LoadForSceneAsync("scene-0001", CancellationToken.None));
        Assert.Equal(first.CreatedAt, second.CreatedAt);
        Assert.Equal("mentioned", link.Role);
        Assert.Equal("second", link.Notes);
    }

    [Fact]
    public async Task DeleteEntityRemovesSceneEntityLinks()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var storyStore = new StoryStructureStore(paths);
        var first = await storyStore.AddEntityAsync(StoryEntityType.Character, "Character A", "Lead", "", "#2563EB", [], CancellationToken.None);
        var second = await storyStore.AddEntityAsync(StoryEntityType.Character, "Character B", "Support", "", "#DB2777", [], CancellationToken.None);
        var linkStore = new SceneEntityLinkStore(paths);
        await linkStore.AddOrUpdateAsync("scene-0001", first.Id, "appears", "", CancellationToken.None);
        await linkStore.AddOrUpdateAsync("scene-0001", second.Id, "appears", "", CancellationToken.None);

        await storyStore.DeleteEntityAsync(first.Id, CancellationToken.None);

        var link = Assert.Single(await linkStore.LoadAsync(CancellationToken.None));
        Assert.Equal(second.Id, link.EntityId);
    }

    [Fact]
    public async Task SavingSceneEntityLinksDoesNotRewriteDocumentBodies()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var projectStore = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "Body",
            [new WriterParagraph("p-0001", "본문은 그대로 둔다.", "body", [], [])]);
        await projectStore.SaveDocumentAsync(document, CancellationToken.None);
        var jsonTimestamp = File.GetLastWriteTimeUtc(paths.DocumentJsonPath(document.Id));
        var textTimestamp = File.GetLastWriteTimeUtc(paths.DocumentTextPath(document.Id));
        var entity = await new StoryStructureStore(paths).AddEntityAsync(
            StoryEntityType.Character,
            "Character A",
            "Lead",
            "",
            "#2563EB",
            [],
            CancellationToken.None);

        await new SceneEntityLinkStore(paths).AddOrUpdateAsync(document.Id, entity.Id, "appears", "", CancellationToken.None);

        Assert.Equal(jsonTimestamp, File.GetLastWriteTimeUtc(paths.DocumentJsonPath(document.Id)));
        Assert.Equal(textTimestamp, File.GetLastWriteTimeUtc(paths.DocumentTextPath(document.Id)));
    }

    private static string NewRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
