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
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-html", "Main", registry);
        var manifest = new ProjectManifest(
            1,
            "Korean Novel",
            [
                new ProjectDocumentInfo("scene-0001", "First Scene", "scene-0001.wwdoc.json", "scene-0001.txt", DateTimeOffset.Parse("2026-06-26T01:00:00+09:00")),
                new ProjectDocumentInfo("scene-0002", "Second Scene", "scene-0002.wwdoc.json", "scene-0002.txt", DateTimeOffset.Parse("2026-06-26T02:00:00+09:00"))
            ]);
        var metadata = new Dictionary<string, SceneMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["scene-0001"] = SceneMetadata.CreateDefault("scene-0001") with
            {
                Summary = "Opening",
                Tags = ["protagonist", "secret"],
                ContentLength = 120,
                ContentLengthWithSpaces = 140,
                SceneType = "Scene",
                Status = SceneStatus.Revising
            },
            ["scene-0002"] = SceneMetadata.CreateDefault("scene-0002") with
            {
                Summary = "Chase",
                Tags = ["action"],
                ContentLength = 90,
                ContentLengthWithSpaces = 100,
                SceneType = "Action",
                Status = SceneStatus.Draft
            }
        };
        var activeDocument = new WriterDocument(
            "scene-0001",
            "First Scene",
            [
                new WriterParagraph("p1", "Active editor body text", "Body", [], [])
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            activeDocument,
            metadata["scene-0001"],
            metadata,
            profile,
            registry,
            "Ready",
            "Default",
            autosaveEnabled: true,
            activeView: "editor",
            previewText: "Preview body",
            shortcutBindings:
            [
                new ShortcutBinding(AppCommandIds.ProjectSave, "Ctrl+S", CommandScope.Global),
                new ShortcutBinding(AppCommandIds.ViewPreviewToggle, "Ctrl+Alt+P", CommandScope.Workbench)
            ]);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        Assert.Equal("Korean Novel", payload.Project.Title);
        Assert.Equal("scene-0001", payload.ActiveScene!.Id);
        Assert.Equal("editor", payload.ActiveView);
        Assert.Equal("Preview body", payload.PreviewText);
        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.ProjectSave && command.Area == "catalog");
        Assert.Contains(payload.ShortcutBindings, shortcut =>
            shortcut.CommandId == AppCommandIds.ProjectSave &&
            shortcut.Gesture == "Ctrl+S" &&
            shortcut.Scope == CommandScope.Global.ToString());
        Assert.Equal("Opening", payload.ActiveScene.Summary);
        Assert.Equal("Active editor body text", payload.ActiveScene.EditorText);
        Assert.Equal(["scene-0001", "scene-0002"], payload.Binder.Select(item => item.Id));
        Assert.Contains(payload.Commands, command => command.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(payload.Commands, command => command.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains("Korean Novel", json);
        Assert.Contains("Active editor body text", json);
        Assert.DoesNotContain("second scene body is not loaded", json);
    }

    [Fact]
    public void PayloadCarriesFullLargeDocumentTextInEditor()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-html", "硫붿씤", registry);
        var now = DateTimeOffset.Parse("2026-06-26T01:00:00+09:00");
        var activeDocument = LargeDocumentFactory.Create("scene-large", "????λ㈃", 15_000);
        var manifest = new ProjectManifest(
            1,
            "????먭퀬",
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
            "Ready",
            "湲곕낯",
            autosaveEnabled: true);

        Assert.NotNull(payload.ActiveScene);
        Assert.Contains(activeDocument.Paragraphs[0].Text, payload.ActiveScene.EditorText);
        Assert.Contains(activeDocument.Paragraphs[^1].Text, payload.ActiveScene.EditorText);
    }

    [Fact]
    public void PayloadSeparatesTopMenuAndRemoteCommandsFromProfilePlacements()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var now = DateTimeOffset.Parse("2026-06-26T01:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-web-shell",
            "???묒뾽?",
            [
                new CommandPlacement("menu", "top.project", "save", AppCommandIds.ProjectSave, "Save", 10, new Dictionary<string, string>()),
                new CommandPlacement("menu", "top.story", "relationship", AppCommandIds.StoryRelationshipMapOpen, "愿怨꾨룄", 20, new Dictionary<string, string>()),
                new CommandPlacement("remote", "floating", "snapshot", AppCommandIds.SnapshotCreateCurrent, "Snapshot", 30, new Dictionary<string, string>()),
                new CommandPlacement("toolbar", "main", "legacy", AppCommandIds.HelpOpen, "Help", 40, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var manifest = new ProjectManifest(
            1,
            "Menu Test",
            [
                new ProjectDocumentInfo("scene-0001", "泥??λ㈃", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "Ready",
            "湲곕낯",
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
            "???묒뾽?",
            [
                new CommandPlacement("toolbar", "main", "legacy", AppCommandIds.HelpOpen, "Help", 40, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var widgetRegistry = new WorkbenchWidgetRegistry(
            "widget-registry",
            [
                new WidgetInstance("w-menu", "command-button", "menu", "top.project", "registry-save", 1, AppCommandIds.ProjectSave, "Save", new Dictionary<string, string>()),
                new WidgetInstance("w-main", "command-button", "menu", "top.view", "registry-main", 2, AppCommandIds.ViewHtmlWorkbenchOpen, "硫붿씤", new Dictionary<string, string>()),
                new WidgetInstance("w-remote", "command-button", "remote", "floating", "registry-focus", 2, AppCommandIds.WritingFocusToggle, "吏묒쨷", new Dictionary<string, string>())
            ]);
        var manifest = new ProjectManifest(
            1,
            "Registry Test",
            [
                new ProjectDocumentInfo("scene-0001", "泥??λ㈃", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "Ready",
            "湲곕낯",
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
            "Remote Edit",
            [
                new CommandPlacement("remote", "main", "remote-01", AppCommandIds.HelpOpen, "Help", 1, new Dictionary<string, string>()),
                new CommandPlacement("remote", "main", "remote-02", AppCommandIds.ProjectSave, "Save", 2, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var widgetRegistry = new WorkbenchWidgetRegistry(
            "widget-registry",
            [
                new WidgetInstance("w-remote", "command-button", "remote", "floating", "registry-focus", 1, AppCommandIds.WritingFocusToggle, "吏묒쨷", new Dictionary<string, string>())
            ]);
        var manifest = new ProjectManifest(1, "Remote", []);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "Ready",
            "湲곕낯",
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
            "Small Profile",
            [
                new CommandPlacement("remote", "main", "remote-01", AppCommandIds.ProjectSave, "Save", 1, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var manifest = new ProjectManifest(1, "移댄깉濡쒓렇", []);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "Ready",
            "湲곕낯",
            autosaveEnabled: true);

        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.HelpOpen);
        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Equal(AppCommandIds.ProjectSave, Assert.Single(payload.RemoteCommands).CommandId);
    }

    [Fact]
    public void PayloadCarriesStoryStructureForHtmlRelationshipMap()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-story", "Relationship", registry);
        var now = DateTimeOffset.Parse("2026-06-27T01:00:00+09:00");
        var story = new WebWorkbenchStory(
            [
                new WebWorkbenchStoryEntity("entity-0001", "Character", "Lead", "Main", "Summary", "#2563EB", ["main"], 80, 70),
                new WebWorkbenchStoryEntity("entity-0002", "Character", "Ally", "Support", "", "#DB2777", [], 260, 160)
            ],
            [
                new WebWorkbenchStoryRelationship("rel-0001", "entity-0001", "entity-0002", "Companion", "Knows each other", true)
            ]);
        var manifest = new ProjectManifest(
            1,
            "Relationship Test",
            [
                new ProjectDocumentInfo("scene-0001", "泥??λ㈃", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "Relationship",
            "Default",
            autosaveEnabled: true,
            activeView: "relationship-map",
            story: story);

        Assert.Equal("relationship-map", payload.ActiveView);
        Assert.NotNull(payload.Story);
        Assert.Equal(["Lead", "Ally"], payload.Story!.Entities.Select(entity => entity.Name));
        Assert.Equal("Companion", Assert.Single(payload.Story.Relationships).Label);
        Assert.Equal(80, payload.Story.Entities[0].X);
        Assert.Equal(160, payload.Story.Entities[1].Y);
    }

    [Fact]
    public void PayloadCarriesTrashItemsForHtmlReferencePanel()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-trash", "Trash", registry);
        var now = DateTimeOffset.Parse("2026-06-27T01:00:00+09:00");
        var manifest = new ProjectManifest(
            1,
            "Trash Test",
            [
                new ProjectDocumentInfo("scene-0001", "泥??λ㈃", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "Ready",
            "湲곕낯",
            autosaveEnabled: true,
            trash:
            [
                new WebWorkbenchTrashItem("scene-0002-20260627010000000", "scene-0002", "??젣???λ㈃", now)
            ]);

        Assert.NotNull(payload.Trash);
        var item = Assert.Single(payload.Trash!);
        Assert.Equal("scene-0002-20260627010000000", item.TrashId);
        Assert.Equal("??젣???λ㈃", item.Title);
    }
}
