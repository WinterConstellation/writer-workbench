using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;

namespace WriterWorkbench.Tests;

public sealed class StoryStructureStoreTests
{
    [Fact]
    public async Task MissingStoryFilesReturnEmptyLists()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new StoryStructureStore(paths);

        var entities = await store.LoadEntitiesAsync(CancellationToken.None);
        var relationships = await store.LoadRelationshipsAsync(CancellationToken.None);
        var layout = await store.LoadRelationLayoutAsync(CancellationToken.None);

        Assert.Empty(entities);
        Assert.Empty(relationships);
        Assert.Empty(layout);
        Assert.False(File.Exists(paths.StoryEntitiesPath));
        Assert.False(File.Exists(paths.StoryRelationshipsPath));
        Assert.False(File.Exists(paths.StoryRelationLayoutPath));
    }

    [Fact]
    public async Task SaveAndLoadSplitStoryFilesRoundtripsKoreanText()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new StoryStructureStore(paths);
        var createdAt = DateTimeOffset.Parse("2026-06-24T01:02:03+09:00");
        var updatedAt = DateTimeOffset.Parse("2026-06-24T02:03:04+09:00");
        var entities = new[]
        {
            new StoryEntity("entity-0001", StoryEntityType.Character, "주인공", "화자", "말을 아끼는 인물", "#4F46E5", ["핵심", "1부"], createdAt, updatedAt),
            new StoryEntity("entity-0002", StoryEntityType.Faction, "동부 길드", "세력", "자금줄", "#0F766E", ["세력"], createdAt, updatedAt)
        };
        var relationships = new[]
        {
            new StoryRelationship("rel-0001", "entity-0001", "entity-0002", "소속", "표면적으로만 협력", true, createdAt, updatedAt)
        };
        var layout = new[]
        {
            new StoryMapNodeLayout("entity-0001", 120, 220),
            new StoryMapNodeLayout("entity-0002", 480, 260)
        };

        await store.SaveEntitiesAsync(entities, CancellationToken.None);
        await store.SaveRelationshipsAsync(relationships, CancellationToken.None);
        await store.SaveRelationLayoutAsync(layout, CancellationToken.None);

        Assert.Contains("주인공", await File.ReadAllTextAsync(paths.StoryEntitiesPath, CancellationToken.None));
        Assert.Contains("표면적으로만 협력", await File.ReadAllTextAsync(paths.StoryRelationshipsPath, CancellationToken.None));
        Assert.Equal(["entity-0001", "entity-0002"], (await store.LoadEntitiesAsync(CancellationToken.None)).Select(entity => entity.Id));
        Assert.Equal("소속", Assert.Single(await store.LoadRelationshipsAsync(CancellationToken.None)).Label);
        Assert.Equal(480, (await store.LoadRelationLayoutAsync(CancellationToken.None))[1].X);
    }

    [Fact]
    public async Task AddRelationshipRejectsMissingEntityId()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new StoryStructureStore(paths);
        var entity = await store.AddEntityAsync(StoryEntityType.Character, "인물 A", "주연", "", "#2563EB", [], CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.AddRelationshipAsync(entity.Id, "missing", "관계", "", false, CancellationToken.None));

        Assert.Empty(await store.LoadRelationshipsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DeleteEntityRemovesRelatedRelationshipsAndLayout()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new StoryStructureStore(paths);
        var first = await store.AddEntityAsync(StoryEntityType.Character, "인물 A", "주연", "", "#2563EB", [], CancellationToken.None);
        var second = await store.AddEntityAsync(StoryEntityType.Character, "인물 B", "조연", "", "#DB2777", [], CancellationToken.None);
        var third = await store.AddEntityAsync(StoryEntityType.Character, "인물 C", "조연", "", "#059669", [], CancellationToken.None);
        await store.AddRelationshipAsync(first.Id, second.Id, "협력", "", true, CancellationToken.None);
        await store.AddRelationshipAsync(second.Id, third.Id, "견제", "", true, CancellationToken.None);
        await store.SaveNodeLayoutAsync(second.Id, 300, 140, CancellationToken.None);
        await store.SaveNodeLayoutAsync(third.Id, 600, 240, CancellationToken.None);

        await store.DeleteEntityAsync(second.Id, CancellationToken.None);

        Assert.Equal([first.Id, third.Id], (await store.LoadEntitiesAsync(CancellationToken.None)).Select(entity => entity.Id));
        Assert.Empty(await store.LoadRelationshipsAsync(CancellationToken.None));
        Assert.Equal([third.Id], (await store.LoadRelationLayoutAsync(CancellationToken.None)).Select(node => node.EntityId));
    }

    [Fact]
    public async Task SaveNodeLayoutUpdatesSingleEntityPosition()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new StoryStructureStore(paths);
        var first = await store.AddEntityAsync(StoryEntityType.Character, "인물 A", "주연", "", "#2563EB", [], CancellationToken.None);
        var second = await store.AddEntityAsync(StoryEntityType.Character, "인물 B", "조연", "", "#DB2777", [], CancellationToken.None);

        await store.SaveNodeLayoutAsync(first.Id, 120, 160, CancellationToken.None);
        await store.SaveNodeLayoutAsync(second.Id, 420, 260, CancellationToken.None);
        await store.SaveNodeLayoutAsync(first.Id, 180, 220, CancellationToken.None);

        var layout = await store.LoadRelationLayoutAsync(CancellationToken.None);
        Assert.Equal(2, layout.Count);
        Assert.Equal(new StoryMapNodeLayout(first.Id, 180, 220), layout.Single(node => node.EntityId == first.Id));
    }

    [Fact]
    public async Task SavingStoryStructureDoesNotRewriteDocumentBodies()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var projectStore = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "Body",
            [new WriterParagraph("p-0001", "본문은 그대로", "body", [], [])]);
        await projectStore.SaveDocumentAsync(document, CancellationToken.None);
        var jsonTimestamp = File.GetLastWriteTimeUtc(paths.DocumentJsonPath(document.Id));
        var textTimestamp = File.GetLastWriteTimeUtc(paths.DocumentTextPath(document.Id));

        await new StoryStructureStore(paths).AddEntityAsync(
            StoryEntityType.Character,
            "본문과 분리된 인물",
            "주연",
            "원고 파일을 건드리지 않는다",
            "#2563EB",
            ["검증"],
            CancellationToken.None);

        Assert.Equal(jsonTimestamp, File.GetLastWriteTimeUtc(paths.DocumentJsonPath(document.Id)));
        Assert.Equal(textTimestamp, File.GetLastWriteTimeUtc(paths.DocumentTextPath(document.Id)));
    }

    private static string NewRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
