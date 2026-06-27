using System.Text.Json;
using System.Text.Encodings.Web;
using WriterWorkbench.Core.AppSettings;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.WebWorkbench;

namespace WriterWorkbench.Tests;

public sealed class WebWorkbenchPayloadFactoryTests
{
    [Fact]
    public void CreatesDashboardPayloadWithActiveEditorTextOnly()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-html", "메인", registry);
        var manifest = new ProjectManifest(
            1,
            "한국어 장편",
            [
                new ProjectDocumentInfo("scene-0001", "첫 장면", "scene-0001.wwdoc.json", "scene-0001.txt", DateTimeOffset.Parse("2026-06-26T01:00:00+09:00")),
                new ProjectDocumentInfo("scene-0002", "두 번째 장면", "scene-0002.wwdoc.json", "scene-0002.txt", DateTimeOffset.Parse("2026-06-26T02:00:00+09:00"))
            ]);
        var metadata = new Dictionary<string, SceneMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["scene-0001"] = SceneMetadata.CreateDefault("scene-0001") with
            {
                Summary = "도입부",
                Tags = ["주인공", "비밀"],
                ContentLength = 120,
                ContentLengthWithSpaces = 140,
                SceneType = "Scene",
                Status = SceneStatus.Revising
            },
            ["scene-0002"] = SceneMetadata.CreateDefault("scene-0002") with
            {
                Summary = "추격",
                Tags = ["액션"],
                ContentLength = 90,
                ContentLengthWithSpaces = 100,
                SceneType = "Action",
                Status = SceneStatus.Draft
            }
        };
        var activeDocument = new WriterDocument(
            "scene-0001",
            "첫 장면",
            [
                new WriterParagraph("p1", "본문은 메인에서 바로 수정할 수 있어야 한다", "Body", [], [])
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            activeDocument,
            metadata["scene-0001"],
            metadata,
            profile,
            registry,
            "프로젝트 준비됨",
            "기본",
            autosaveEnabled: true,
            activeView: "editor",
            previewText: "미리보기 본문",
            shortcutBindings:
            [
                new ShortcutBinding(AppCommandIds.ProjectSave, "Ctrl+S", CommandScope.Global),
                new ShortcutBinding(AppCommandIds.ViewPreviewToggle, "Ctrl+Alt+P", CommandScope.Workbench)
            ]);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        Assert.Equal("한국어 장편", payload.Project.Title);
        Assert.Equal("scene-0001", payload.ActiveScene!.Id);
        Assert.Equal("editor", payload.ActiveView);
        Assert.Equal("미리보기 본문", payload.PreviewText);
        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.ProjectSave && command.Area == "catalog");
        Assert.Contains(payload.ShortcutBindings, shortcut =>
            shortcut.CommandId == AppCommandIds.ProjectSave &&
            shortcut.Gesture == "Ctrl+S" &&
            shortcut.Scope == CommandScope.Global.ToString());
        Assert.Equal("도입부", payload.ActiveScene.Summary);
        Assert.Equal("본문은 메인에서 바로 수정할 수 있어야 한다", payload.ActiveScene.EditorText);
        Assert.False(payload.ActiveScene.IsSegmentMode);
        Assert.Equal(["scene-0001", "scene-0002"], payload.Binder.Select(item => item.Id));
        Assert.Contains(payload.Commands, command => command.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(payload.Commands, command => command.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains("한국어 장편", json);
        Assert.Contains("본문은 메인에서 바로 수정할 수 있어야 한다", json);
        Assert.DoesNotContain("두 번째 장면 본문은 로드하지 않는다", json);
    }

    [Fact]
    public void PayloadCarriesRemainingLargeDocumentTextForOnScreenReading()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-html", "메인", registry);
        var now = DateTimeOffset.Parse("2026-06-26T01:00:00+09:00");
        var activeDocument = LargeDocumentFactory.Create("scene-large", "대형 장면", 15_000);
        var manifest = new ProjectManifest(
            1,
            "대형 원고",
            [
                new ProjectDocumentInfo(activeDocument.Id, activeDocument.Title, "scene-large.wwdoc.json", "scene-large.txt", now)
            ]);
        var metadata = SceneMetadata.CreateDefault(activeDocument.Id) with
        {
            ContentLength = 1_530_007,
            ContentLengthWithSpaces = 1_755_009
        };

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            activeDocument,
            metadata,
            new Dictionary<string, SceneMetadata> { [activeDocument.Id] = metadata },
            profile,
            registry,
            "대기",
            "기본",
            autosaveEnabled: true);

        Assert.True(payload.ActiveScene!.IsSegmentMode);
        Assert.NotEmpty(payload.ActiveScene.RemainderText);
        Assert.Equal(activeDocument.Paragraphs.Count, payload.ActiveScene.TotalParagraphCount);
        Assert.Contains(activeDocument.Paragraphs[payload.ActiveScene.VisibleParagraphCount].Text, payload.ActiveScene.RemainderText);
        Assert.Contains(activeDocument.Paragraphs[^1].Text, payload.ActiveScene.RemainderText);
    }

    [Fact]
    public void PayloadSeparatesTopMenuAndRemoteCommandsFromProfilePlacements()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var now = DateTimeOffset.Parse("2026-06-26T01:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-web-shell",
            "웹 작업대",
            [
                new CommandPlacement("menu", "top.project", "save", AppCommandIds.ProjectSave, "저장", 10, new Dictionary<string, string>()),
                new CommandPlacement("menu", "top.story", "relationship", AppCommandIds.StoryRelationshipMapOpen, "관계도", 20, new Dictionary<string, string>()),
                new CommandPlacement("remote", "floating", "snapshot", AppCommandIds.SnapshotCreateCurrent, "스냅샷", 30, new Dictionary<string, string>()),
                new CommandPlacement("toolbar", "main", "legacy", AppCommandIds.HelpOpen, "도움말", 40, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var manifest = new ProjectManifest(
            1,
            "메뉴 테스트",
            [
                new ProjectDocumentInfo("scene-0001", "첫 장면", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "대기",
            "기본",
            autosaveEnabled: true);

        Assert.Equal(["save", "relationship", "view.editor"], payload.MenuCommands.Select(command => command.SlotKey));
        Assert.Equal("top.project", payload.MenuCommands[0].Area);
        Assert.Equal("floating", Assert.Single(payload.RemoteCommands).Area);
        Assert.Equal("snapshot", Assert.Single(payload.RemoteCommands).SlotKey);
        Assert.Equal("legacy", Assert.Single(payload.Commands).SlotKey);
    }

    [Fact]
    public void PayloadUsesWidgetRegistryForMenuAndRemoteWhenProvided()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var now = DateTimeOffset.Parse("2026-06-26T01:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-web-shell",
            "웹 작업대",
            [
                new CommandPlacement("toolbar", "main", "legacy", AppCommandIds.HelpOpen, "도움말", 40, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var widgetRegistry = new WorkbenchWidgetRegistry(
            "widget-registry",
            [
                new WidgetInstance("w-menu", "command-button", "menu", "top.project", "registry-save", 1, AppCommandIds.ProjectSave, "저장", new Dictionary<string, string>()),
                new WidgetInstance("w-main", "command-button", "menu", "top.view", "registry-main", 2, AppCommandIds.ViewHtmlWorkbenchOpen, "메인", new Dictionary<string, string>()),
                new WidgetInstance("w-remote", "command-button", "remote", "floating", "registry-focus", 2, AppCommandIds.WritingFocusToggle, "집중", new Dictionary<string, string>())
            ]);
        var manifest = new ProjectManifest(
            1,
            "위젯 테스트",
            [
                new ProjectDocumentInfo("scene-0001", "첫 장면", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "대기",
            "기본",
            autosaveEnabled: true,
            widgetRegistry);

        Assert.Contains(payload.MenuCommands, command => command.SlotKey == "registry-save");
        Assert.Contains(payload.MenuCommands, command => command.CommandId == AppCommandIds.ViewMainOpen && command.SlotKey == "registry-main");
        Assert.Contains(payload.MenuCommands, command => command.CommandId == AppCommandIds.ViewEditorOpen);
        Assert.Equal("registry-focus", Assert.Single(payload.RemoteCommands).SlotKey);
        Assert.Equal("legacy", Assert.Single(payload.Commands).SlotKey);
    }

    [Fact]
    public void PayloadUsesProfileRemotePlacementsBeforeWidgetFallback()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var now = DateTimeOffset.Parse("2026-06-27T01:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-remote-edited",
            "리모컨 편집됨",
            [
                new CommandPlacement("remote", "main", "remote-01", AppCommandIds.HelpOpen, "도움말", 1, new Dictionary<string, string>()),
                new CommandPlacement("remote", "main", "remote-02", AppCommandIds.ProjectSave, "저장", 2, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var widgetRegistry = new WorkbenchWidgetRegistry(
            "widget-registry",
            [
                new WidgetInstance("w-remote", "command-button", "remote", "floating", "registry-focus", 1, AppCommandIds.WritingFocusToggle, "집중", new Dictionary<string, string>())
            ]);
        var manifest = new ProjectManifest(1, "리모컨", []);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "대기",
            "기본",
            autosaveEnabled: true,
            widgetRegistry);

        Assert.Equal([AppCommandIds.HelpOpen, AppCommandIds.ProjectSave], payload.RemoteCommands.Select(command => command.CommandId));
        Assert.Equal(["remote-01", "remote-02"], payload.RemoteCommands.Select(command => command.SlotKey));
    }

    [Fact]
    public void PayloadExposesCatalogCommandsForHtmlRemoteSettingsEvenWhenToolbarProfileIsSparse()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var now = DateTimeOffset.Parse("2026-06-27T01:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-sparse",
            "희소 프로필",
            [
                new CommandPlacement("remote", "main", "remote-01", AppCommandIds.ProjectSave, "저장", 1, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var manifest = new ProjectManifest(1, "카탈로그", []);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "대기",
            "기본",
            autosaveEnabled: true);

        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.HelpOpen);
        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Equal(AppCommandIds.ProjectSave, Assert.Single(payload.RemoteCommands).CommandId);
    }

    [Fact]
    public void PayloadCarriesStoryStructureForHtmlRelationshipMap()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-story", "관계도", registry);
        var now = DateTimeOffset.Parse("2026-06-27T01:00:00+09:00");
        var story = new WebWorkbenchStory(
            [
                new WebWorkbenchStoryEntity("entity-0001", "Character", "윤서", "주연", "요약", "#2563EB", ["주연"], 80, 70),
                new WebWorkbenchStoryEntity("entity-0002", "Character", "도현", "조력자", "", "#DB2777", [], 260, 160)
            ],
            [
                new WebWorkbenchStoryRelationship("rel-0001", "entity-0001", "entity-0002", "동맹", "서로 돕는다", true)
            ]);
        var manifest = new ProjectManifest(
            1,
            "관계도 테스트",
            [
                new ProjectDocumentInfo("scene-0001", "첫 장면", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "관계도",
            "기본",
            autosaveEnabled: true,
            activeView: "relationship-map",
            story: story);

        Assert.Equal("relationship-map", payload.ActiveView);
        Assert.NotNull(payload.Story);
        Assert.Equal(["윤서", "도현"], payload.Story!.Entities.Select(entity => entity.Name));
        Assert.Equal("동맹", Assert.Single(payload.Story.Relationships).Label);
        Assert.Equal(80, payload.Story.Entities[0].X);
        Assert.Equal(160, payload.Story.Entities[1].Y);
    }

    [Fact]
    public void PayloadCarriesTrashItemsForHtmlReferencePanel()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-trash", "휴지통", registry);
        var now = DateTimeOffset.Parse("2026-06-27T01:00:00+09:00");
        var manifest = new ProjectManifest(
            1,
            "휴지통 테스트",
            [
                new ProjectDocumentInfo("scene-0001", "첫 장면", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "대기",
            "기본",
            autosaveEnabled: true,
            trash:
            [
                new WebWorkbenchTrashItem("scene-0002-20260627010000000", "scene-0002", "삭제된 장면", now)
            ]);

        Assert.NotNull(payload.Trash);
        var item = Assert.Single(payload.Trash!);
        Assert.Equal("scene-0002-20260627010000000", item.TrashId);
        Assert.Equal("삭제된 장면", item.Title);
    }
}
