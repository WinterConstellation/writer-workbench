using System.Text.Json;
using System.Text.Encodings.Web;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.WebWorkbench;

namespace WriterWorkbench.Tests;

public sealed class WebWorkbenchPayloadFactoryTests
{
    [Fact]
    public void CreatesDashboardPayloadWithoutDocumentBodyText()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-html", "HTML 작업대", registry);
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
                new WriterParagraph("p1", "본문은 HTML 작업대 상태에 들어가면 안 된다", "Body", [], [])
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
            autosaveEnabled: true);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        Assert.Equal("한국어 장편", payload.Project.Title);
        Assert.Equal("scene-0001", payload.ActiveScene!.Id);
        Assert.Equal("도입부", payload.ActiveScene.Summary);
        Assert.Equal(["scene-0001", "scene-0002"], payload.Binder.Select(item => item.Id));
        Assert.Contains(payload.Commands, command => command.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(payload.Commands, command => command.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains("한국어 장편", json);
        Assert.DoesNotContain("본문은 HTML 작업대 상태에 들어가면 안 된다", json);
    }
}
