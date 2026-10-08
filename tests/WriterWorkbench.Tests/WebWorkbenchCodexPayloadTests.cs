using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.WebWorkbench;

namespace WriterWorkbench.Tests;

public sealed class WebWorkbenchCodexPayloadTests
{
    [Fact]
    public void PayloadCarriesCodexCliStateAndMenuCommand()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-html", "main", registry);
        var now = DateTimeOffset.Parse("2026-06-29T23:00:00+09:00");
        var manifest = new ProjectManifest(
            1,
            "Novel",
            [
                new ProjectDocumentInfo("scene-0001", "First scene", "scene-0001.wwdoc.json", "scene-0001.txt", now)
            ]);
        var codex = new WebWorkbenchCodexCliState(
            true,
            false,
            "codex",
            "Codex CLI ready",
            "",
            "",
            "",
            null,
            false,
            null);

        var payload = WebWorkbenchPayloadFactory.Create(
            manifest,
            @"C:\WriterWorkbench\Projects\Sample.writerproj",
            null,
            null,
            new Dictionary<string, SceneMetadata>(),
            profile,
            registry,
            "ready",
            "default",
            true,
            activeView: "codex",
            codexCli: codex);

        Assert.Equal("codex", payload.ActiveView);
        Assert.Same(codex, payload.CodexCli);
        Assert.Contains(payload.MenuCommands, command => command.CommandId == AppCommandIds.CodexOpen);
        Assert.Contains(payload.AvailableCommands, command => command.CommandId == AppCommandIds.CodexOpen);
    }
}
