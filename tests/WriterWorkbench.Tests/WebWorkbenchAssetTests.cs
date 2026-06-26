namespace WriterWorkbench.Tests;

public sealed class WebWorkbenchAssetTests
{
    [Fact]
    public void WebWorkbenchAssetsAreCopiedNextToApplication()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var assetDirectory = Path.Combine(appDirectory, "WebWorkbench");

        Assert.True(File.Exists(Path.Combine(assetDirectory, "index.html")));
        Assert.True(File.Exists(Path.Combine(assetDirectory, "styles.css")));
        Assert.True(File.Exists(Path.Combine(assetDirectory, "app.js")));
    }

    [Fact]
    public async Task WebWorkbenchHtmlContainsFullWorkbenchShellAnchors()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var htmlPath = Path.Combine(appDirectory, "WebWorkbench", "index.html");

        var html = await File.ReadAllTextAsync(htmlPath, CancellationToken.None);

        Assert.Contains("top-menu", html);
        Assert.Contains("left-rail", html);
        Assert.Contains("center-surface", html);
        Assert.Contains("right-inspector", html);
        Assert.Contains("floating-remote", html);
        Assert.Contains("remote-drag-handle", html);
        Assert.Contains("remote-drag-icon", html);
        Assert.Contains("binder-action-bar", html);
        Assert.Contains("binder-context-menu", html);
        Assert.Contains("active-body-editor", html);
        Assert.Contains("html-view-editor", html);
        Assert.Contains("html-view-preview", html);
        Assert.Contains("html-view-relationship", html);
        Assert.Contains("relationship-workbench", html);
        Assert.Contains("story-entity-name", html);
        Assert.Contains("story-entity-role", html);
        Assert.Contains("story-add-entity", html);
        Assert.Contains("story-relationship-source", html);
        Assert.Contains("story-relationship-target", html);
        Assert.Contains("story-relationship-label", html);
        Assert.Contains("story-add-relationship", html);
        Assert.Contains("relationship-map-canvas", html);
        Assert.Contains("story-entity-list", html);
        Assert.Contains("story-relationship-list", html);
        Assert.Contains("html-view-shortcuts", html);
        Assert.Contains("html-view-remote-settings", html);
        Assert.Contains("html-view-help", html);
        Assert.Contains("shortcut-search", html);
        Assert.Contains("remote-settings-current-list", html);
        Assert.Contains("remote-settings-available-list", html);
        Assert.Contains("remote-settings-save", html);
        Assert.Contains("rail-panel-settings", html);
        Assert.Contains("rail-panel-reference", html);
        Assert.Contains("bottom-status", html);
    }

    [Fact]
    public async Task WebWorkbenchScriptWiresRemoteDragTabsAndEditorUpdate()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var scriptPath = Path.Combine(appDirectory, "WebWorkbench", "app.js");

        var script = await File.ReadAllTextAsync(scriptPath, CancellationToken.None);

        Assert.Contains("startRemoteDrag", script);
        Assert.Contains("activeScene.update", script);
        Assert.Contains("document.select", script);
        Assert.Contains("document.command", script);
        Assert.Contains("showBinderContextMenu", script);
        Assert.Contains("hideBinderContextMenu", script);
        Assert.Contains("sendBinderCommand", script);
        Assert.Contains("setRailMode", script);
        Assert.Contains("setActiveView", script);
        Assert.Contains("renderPreview", script);
        Assert.Contains("renderRemoteSettings", script);
        Assert.Contains("saveRemoteSettings", script);
        Assert.Contains("remoteSettings.update", script);
        Assert.Contains("renderShortcutSettings", script);
        Assert.Contains("filterShortcutSettings", script);
        Assert.Contains("scheduleLocalMetricUpdate", script);
        Assert.Contains("1000", script);
        Assert.Contains("renderRelationshipMap", script);
        Assert.Contains("story.entity.add", script);
        Assert.Contains("story.relationship.add", script);
        Assert.Contains("story.layout.update", script);
    }

    [Fact]
    public async Task WebWorkbenchCssKeepsInWindowRemoteSecondary()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var cssPath = Path.Combine(appDirectory, "WebWorkbench", "styles.css");

        var css = await File.ReadAllTextAsync(cssPath, CancellationToken.None);

        Assert.Contains(".floating-remote", css);
        Assert.Contains("display: none", css);
        Assert.Contains("#rail-panel-binder", css);
        Assert.Contains("grid-template-rows: auto auto minmax(0, 1fr)", css);
        Assert.Contains(".relationship-workbench", css);
        Assert.Contains(".relationship-map-canvas", css);
    }
}
