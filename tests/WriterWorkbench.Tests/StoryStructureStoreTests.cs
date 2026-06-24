using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;

namespace WriterWorkbench.Tests;

public sealed class StoryStructureStoreTests
{
    [Fact]
    public async Task LoadOrCreateCreatesEmptyStoryStructureFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new StoryStructureStore(paths);

        var document = await store.LoadOrCreateAsync(CancellationToken.None);

        Assert.Equal(1, document.SchemaVersion);
        Assert.Empty(document.Nodes);
        Assert.Empty(document.Relationships);
        Assert.True(File.Exists(paths.StoryStructurePath));
    }

    [Fact]
    public async Task SaveAndLoadRoundtripsNodesAndRelationships()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new StoryStructureStore(paths);
        var createdAt = DateTimeOffset.Parse("2026-06-24T01:02:03+09:00");
        var updatedAt = DateTimeOffset.Parse("2026-06-24T02:03:04+09:00");
        var document = new StoryStructureDocument(
            1,
            [
                new StoryStructureNode("arc-main", "중심 줄기", "Arc", "주인공 선택의 압력", ["핵심", "1부"], ["scene-0001"], 10, createdAt, updatedAt),
                new StoryStructureNode("char-lead", "주인공", "Character", "말을 아끼는 인물", ["인물"], ["scene-0001", "scene-0002"], 20, createdAt, updatedAt)
            ],
            [
                new RelationshipLink("rel-arc-lead", "arc-main", "char-lead", "drives", "구조가 인물을 밀어붙임", 4, ["갈등"], createdAt, updatedAt)
            ],
            updatedAt);

        await store.SaveAsync(document, CancellationToken.None);
        var loaded = await store.LoadOrCreateAsync(CancellationToken.None);

        Assert.Equal(["arc-main", "char-lead"], loaded.Nodes.Select(node => node.Id));
        Assert.Equal("중심 줄기", loaded.Nodes[0].Name);
        Assert.Equal(["scene-0001", "scene-0002"], loaded.Nodes[1].LinkedSceneIds);
        var relationship = Assert.Single(loaded.Relationships);
        Assert.Equal("rel-arc-lead", relationship.Id);
        Assert.Equal("arc-main", relationship.SourceNodeId);
        Assert.Equal("char-lead", relationship.TargetNodeId);
        Assert.Equal(4, relationship.Strength);
    }

    [Fact]
    public async Task SaveDropsRelationshipsWithMissingEndpoints()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new StoryStructureStore(paths);
        var now = DateTimeOffset.UtcNow;
        var document = new StoryStructureDocument(
            1,
            [new StoryStructureNode("node-a", "A", "Theme", "", [], [], 0, now, now)],
            [
                new RelationshipLink("rel-valid", "node-a", "node-a", "echo", "", 1, [], now, now),
                new RelationshipLink("rel-broken", "node-a", "missing", "broken", "", 1, [], now, now)
            ],
            now);

        await store.SaveAsync(document, CancellationToken.None);
        var loaded = await store.LoadOrCreateAsync(CancellationToken.None);

        var relationship = Assert.Single(loaded.Relationships);
        Assert.Equal("rel-valid", relationship.Id);
    }

    [Fact]
    public async Task SavingStoryStructureDoesNotRewriteDocumentBodies()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var projectStore = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "Body",
            [new WriterParagraph("p-0001", "본문은 그대로", "body", [], [])]);
        await projectStore.SaveDocumentAsync(document, CancellationToken.None);
        var jsonTimestamp = File.GetLastWriteTimeUtc(paths.DocumentJsonPath(document.Id));
        var textTimestamp = File.GetLastWriteTimeUtc(paths.DocumentTextPath(document.Id));
        var now = DateTimeOffset.UtcNow;
        var structure = new StoryStructureDocument(
            1,
            [new StoryStructureNode("node-body", "본문 연결", "PlotPoint", "", [], [document.Id], 0, now, now)],
            [],
            now);

        await new StoryStructureStore(paths).SaveAsync(structure, CancellationToken.None);

        Assert.Equal(jsonTimestamp, File.GetLastWriteTimeUtc(paths.DocumentJsonPath(document.Id)));
        Assert.Equal(textTimestamp, File.GetLastWriteTimeUtc(paths.DocumentTextPath(document.Id)));
    }

    [Fact]
    public async Task PreservesKoreanTextAsUtf8()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new StoryStructureStore(paths);
        var now = DateTimeOffset.UtcNow;
        var structure = new StoryStructureDocument(
            1,
            [new StoryStructureNode("node-korean", "감정선", "Theme", "후반부에서 의미가 뒤집히는 관계", ["복선"], ["scene-0001"], 0, now, now)],
            [new RelationshipLink("rel-korean", "node-korean", "node-korean", "mirror", "같은 사건을 다르게 해석", 2, ["반복"], now, now)],
            now);

        await store.SaveAsync(structure, CancellationToken.None);

        var raw = await File.ReadAllTextAsync(paths.StoryStructurePath, CancellationToken.None);
        var loaded = await store.LoadOrCreateAsync(CancellationToken.None);

        Assert.Contains("감정선", raw);
        Assert.Contains("같은 사건을 다르게 해석", raw);
        Assert.Equal("감정선", loaded.Nodes[0].Name);
        Assert.Equal("같은 사건을 다르게 해석", loaded.Relationships[0].Summary);
    }
}
