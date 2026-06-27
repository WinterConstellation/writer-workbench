using WriterWorkbench.Core.WebWorkbench;

namespace WriterWorkbench.Tests;

public sealed class WebWorkbenchAssetTests
{
    [Fact]
    public void WebWorkbenchIndexUriUsesAssetVersionForCacheBusting()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var assetDirectory = Path.Combine(tempRoot, "WebWorkbench");
        Directory.CreateDirectory(assetDirectory);
        var indexPath = Path.Combine(assetDirectory, "index.html");
        var scriptPath = Path.Combine(assetDirectory, "app.js");
        var stylePath = Path.Combine(assetDirectory, "styles.css");
        File.WriteAllText(indexPath, "<!doctype html>");
        File.WriteAllText(scriptPath, "console.log('v1');");
        File.WriteAllText(stylePath, "body{}");

        try
        {
            var first = WebWorkbenchAssetVersion.CreateIndexUri(indexPath);
            File.SetLastWriteTimeUtc(scriptPath, File.GetLastWriteTimeUtc(scriptPath).AddMinutes(1));
            var second = WebWorkbenchAssetVersion.CreateIndexUri(indexPath);

            Assert.Contains("?wwv=", first.AbsoluteUri);
            Assert.Contains("?wwv=", second.AbsoluteUri);
            Assert.NotEqual(first.AbsoluteUri, second.AbsoluteUri);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

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
        Assert.Contains("remote-drag-arrow-line", html);
        Assert.Contains("remote-drag-arrow-head", html);
        Assert.DoesNotContain(">✥<", html);
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
        Assert.Contains("story-relationship-notes", html);
        Assert.Contains("story-add-relationship", html);
        Assert.Contains("story-cancel-entity-edit", html);
        Assert.Contains("story-cancel-relationship-edit", html);
        Assert.Contains("relationship-map-canvas", html);
        Assert.Contains("story-entity-list", html);
        Assert.Contains("story-relationship-list", html);
        Assert.Contains("trash-list", html);
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
    public async Task WebWorkbenchHtmlPropagatesAssetVersionToStylesAndScript()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var htmlPath = Path.Combine(appDirectory, "WebWorkbench", "index.html");

        var html = await File.ReadAllTextAsync(htmlPath, CancellationToken.None);

        Assert.Contains("createWorkbenchAssetUrl", html);
        Assert.Contains("styles.css", html);
        Assert.Contains("app.js", html);
        Assert.Contains("wwv", html);
        Assert.DoesNotContain("<link rel=\"stylesheet\" href=\"styles.css\">", html);
        Assert.DoesNotContain("<script src=\"app.js\"></script>", html);
    }

    [Fact]
    public async Task WebWorkbenchUsesExplicitCharacterCountLabels()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var scriptPath = Path.Combine(appDirectory, "WebWorkbench", "app.js");

        var script = await File.ReadAllTextAsync(scriptPath, CancellationToken.None);

        Assert.Contains("normalizeCharacterCountLabels", script);
        Assert.Contains("전체 공백 제외", script);
        Assert.Contains("전체 공백 포함", script);
        Assert.Contains("공백 제외 ${formatNumber(length)}", script);
    }

    [Fact]
    public async Task WebWorkbenchDoesNotSplitManuscriptIntoRemainderPanel()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var scriptPath = Path.Combine(appDirectory, "WebWorkbench", "app.js");
        var htmlPath = Path.Combine(appDirectory, "WebWorkbench", "index.html");

        var script = await File.ReadAllTextAsync(scriptPath, CancellationToken.None);
        var html = await File.ReadAllTextAsync(htmlPath, CancellationToken.None);

        Assert.DoesNotContain("active-remainder-panel", html);
        Assert.DoesNotContain("active-remainder-text", html);
        Assert.DoesNotContain("active-segment-status", html);
        Assert.DoesNotContain("renderRemainderText", script);
        Assert.DoesNotContain("remainderText", script);
        Assert.DoesNotContain("RemainderText", script);
        Assert.DoesNotContain("active-remainder-text", script);
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
        Assert.Contains("dataset.binderDocument", script);
        Assert.Contains("scene-actions", script);
        Assert.Contains("showBinderContextMenu", script);
        Assert.Contains("hideBinderContextMenu", script);
        Assert.Contains("sendBinderCommand", script);
        Assert.Contains("document.addEventListener(\"contextmenu\"", script);
        Assert.Contains("closest(\".scene-item[data-document-id]\")", script);
        Assert.Contains("positionBinderContextMenu", script);
        Assert.Contains("menu.dataset.documentId", script);
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
        Assert.Contains("story.entity.update", script);
        Assert.Contains("story.entity.delete", script);
        Assert.Contains("story.relationship.add", script);
        Assert.Contains("story.relationship.update", script);
        Assert.Contains("story.relationship.delete", script);
        Assert.Contains("story.layout.update", script);
        Assert.Contains("trash.restore", script);
        Assert.Contains("shortcut.update", script);
        Assert.Contains("captureShortcutGesture", script);
    }

    [Fact]
    public async Task WebWorkbenchRemoteSettingsKeepsDraftDuringEditorRerender()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var scriptPath = Path.Combine(appDirectory, "WebWorkbench", "app.js");

        var script = await File.ReadAllTextAsync(scriptPath, CancellationToken.None);

        Assert.Contains("remoteSettingsDirty", script);
        Assert.Contains("syncRemoteDraftFromPayload", script);
        Assert.Contains("state.remoteSettingsDirty = true", script);
        Assert.Contains("state.remoteSettingsDirty = false", script);
        Assert.Contains("activeView === \"remote-settings\"", script);
    }

    [Fact]
    public async Task WebWorkbenchRelationshipCanvasNodesExposeEditAndDeleteActions()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var scriptPath = Path.Combine(appDirectory, "WebWorkbench", "app.js");
        var stylePath = Path.Combine(appDirectory, "WebWorkbench", "styles.css");

        var script = await File.ReadAllTextAsync(scriptPath, CancellationToken.None);
        var css = await File.ReadAllTextAsync(stylePath, CancellationToken.None);

        Assert.Contains("story-map-node-actions", script);
        Assert.Contains("createStoryActionButton(\"entityEdit\"", script);
        Assert.Contains("createStoryActionButton(\"entityDelete\"", script);
        Assert.Contains("event.target.closest(\"[data-story-action]\")", script);
        Assert.Contains(".story-map-node-actions", css);
    }

    [Fact]
    public async Task WebWorkbenchLocalMetricUpdateUsesWholeEditorText()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var scriptPath = Path.Combine(appDirectory, "WebWorkbench", "app.js");
        var htmlPath = Path.Combine(appDirectory, "WebWorkbench", "index.html");

        var script = await File.ReadAllTextAsync(scriptPath, CancellationToken.None);
        var html = await File.ReadAllTextAsync(htmlPath, CancellationToken.None);

        Assert.Contains("active-editor-metrics", html);
        Assert.Contains("function measureEditorText", script);
        Assert.Contains("updateActiveEditorMetrics", script);
        Assert.Contains("active-editor-metrics", script);
        Assert.DoesNotContain("visibleContentLength", script);
        Assert.DoesNotContain("visibleContentLengthWithSpaces", script);
        Assert.DoesNotContain("estimateFullSceneMetrics", script);
        var updateFunctionStart = script.IndexOf("function updateActiveEditorMetrics()", StringComparison.Ordinal);
        var nextFunctionStart = script.IndexOf("\nfunction addStoryEntity", updateFunctionStart, StringComparison.Ordinal);
        var updateFunction = script[updateFunctionStart..nextFunctionStart];

        Assert.Contains("$(\"active-length\").textContent = formatNumber(current.contentLength);", updateFunction);
        Assert.Contains("$(\"active-length-spaces\").textContent = formatNumber(current.contentLengthWithSpaces);", updateFunction);
        Assert.DoesNotContain("text.replace(/\\s/g, \"\").length", script);
        Assert.DoesNotContain("text.length", script);
    }

    [Fact]
    public async Task WebWorkbenchCssKeepsInWindowRemoteSecondary()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var cssPath = Path.Combine(appDirectory, "WebWorkbench", "styles.css");

        var css = await File.ReadAllTextAsync(cssPath, CancellationToken.None);

        Assert.Contains(".floating-remote", css);
        var remoteBlockStart = css.IndexOf(".floating-remote", StringComparison.Ordinal);
        var remoteBlockEnd = css.IndexOf("\n}", remoteBlockStart, StringComparison.Ordinal);
        var remoteBlock = css[remoteBlockStart..remoteBlockEnd];
        Assert.Contains("display: block", remoteBlock);
        Assert.DoesNotContain("display: none", remoteBlock);
        Assert.Contains("#rail-panel-binder", css);
        Assert.Contains("grid-template-rows: auto auto minmax(0, 1fr)", css);
        Assert.Contains(".scene-actions", css);
        Assert.Contains(".relationship-workbench", css);
        Assert.Contains(".relationship-map-canvas", css);
    }

    [Fact]
    public async Task WebWorkbenchRemoteSettingsRowsDoNotUseMoveCursor()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var cssPath = Path.Combine(appDirectory, "WebWorkbench", "styles.css");

        var css = await File.ReadAllTextAsync(cssPath, CancellationToken.None);

        Assert.Contains(".remote-settings-row", css);
        Assert.Contains("cursor: default", css);
        Assert.Contains(".remote-settings-actions button", css);
        Assert.Contains("cursor: pointer", css);
    }
}
