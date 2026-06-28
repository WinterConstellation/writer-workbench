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
        Assert.Contains("binder-category-filter", html);
        Assert.Contains("binder-status-filter", html);
        Assert.Contains("binder-filter-summary", html);
        Assert.Contains("binder-context-menu", html);
        Assert.Contains("active-body-editor", html);
        Assert.Contains("html-view-editor", html);
        Assert.Contains("html-view-preview", html);
        Assert.Contains("active-file-category", html);
        Assert.Contains("active-status-editor", html);
        Assert.Contains("active-file-category-editor", html);
        Assert.Contains("active-metadata-save", html);
        Assert.Contains("active-memo-editor", html);
        Assert.Contains("active-memo-save", html);
        Assert.Contains("scene-memo-overview", html);
        Assert.Contains("inspector-file-category", html);
        Assert.Contains("pipeline-revision-complete", html);
        Assert.Contains("pipeline-upload-pending", html);
        Assert.Contains("pipeline-uploaded", html);
        Assert.Contains("html-view-relationship", html);
        Assert.Contains("relationship-workbench", html);
        Assert.Contains("story-entity-name", html);
        Assert.Contains("story-entity-type", html);
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
        Assert.Contains("settings-book-category", html);
        Assert.Contains("value=\"Synopsis\"", html);
        Assert.Contains("settings-book-title", html);
        Assert.Contains("settings-book-body", html);
        Assert.Contains("settings-book-tags", html);
        Assert.Contains("settings-book-save", html);
        Assert.Contains("settings-book-cancel", html);
        Assert.Contains("text-replacement-source", html);
        Assert.Contains("text-replacement-target", html);
        Assert.Contains("text-replacement-add", html);
        Assert.Contains("text-replacement-apply", html);
        Assert.Contains("text-replacement-list", html);
        Assert.Contains("rail-panel-reference", html);
        Assert.Contains("reference-form", html);
        Assert.Contains("reference-title", html);
        Assert.Contains("reference-body", html);
        Assert.Contains("reference-tags", html);
        Assert.Contains("reference-save", html);
        Assert.Contains("reference-cancel", html);
        Assert.Contains("reference-summary-list", html);
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
        Assert.Contains("document.reorder", script);
        Assert.Contains("scene.metadata.update", script);
        Assert.Contains("scene.memo.update", script);
        Assert.Contains("dataset.binderDocument", script);
        Assert.Contains("row.draggable = true", script);
        Assert.Contains("handleBinderDragStart", script);
        Assert.Contains("handleBinderDrop", script);
        Assert.Contains("reorderBinderDocumentIds", script);
        Assert.Contains("saveActiveSceneMetadata", script);
        Assert.Contains("saveActiveSceneMemo", script);
        Assert.Contains("renderSceneMemoOverview", script);
        Assert.Contains("binderFileCategoryFilter", script);
        Assert.Contains("binderWorkflowStatusFilter", script);
        Assert.Contains("renderBinderCategoryFilter", script);
        Assert.Contains("countBinderWorkflowStatuses", script);
        Assert.Contains("filterBinderByCategory", script);
        Assert.Contains("filterBinderByWorkflowStatus", script);
        Assert.Contains("normalizeFileCategory", script);
        Assert.Contains("normalizeWorkflowStatus", script);
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
        Assert.Contains("renderSettingsPanel(menuCommands, settingsBook)", script);
        Assert.Contains("applyGraphicPresetTheme", script);
        Assert.Contains("normalizeGraphicPresetId", script);
        Assert.Contains("dataset.graphicPreset", script);
        Assert.Contains("graphicPresetId", script);
        Assert.Contains("story.settingsBook.add", script);
        Assert.Contains("story.settingsBook.update", script);
        Assert.Contains("story.settingsBook.delete", script);
        Assert.Contains("textReplacement.add", script);
        Assert.Contains("textReplacement.delete", script);
        Assert.Contains("textReplacement.applyActive", script);
        Assert.Contains("renderTextReplacements", script);
        Assert.Contains("normalizeSettingsBookItem", script);
        Assert.Contains("data-settings-book-action", script);
        Assert.Contains("data-text-replacement-delete", script);
        Assert.Contains("renderReferencePanel(project, active, trash, settingsBook)", script);
        Assert.Contains("saveReferenceItem", script);
        Assert.Contains("handleReferenceAction", script);
        Assert.Contains("dataset.referenceAction", script);
        Assert.Contains("category: \"Reference\"", script);
        Assert.Contains("renderShortcutSettings", script);
        Assert.Contains("filterShortcutSettings", script);
        Assert.Contains("scheduleLocalMetricUpdate", script);
        Assert.Contains("1000", script);
        Assert.Contains("pipeline-revision-complete", script);
        Assert.Contains("pipeline-upload-pending", script);
        Assert.Contains("pipeline-uploaded", script);
        Assert.Contains("퇴고중", script);
        Assert.Contains("퇴고완료", script);
        Assert.Contains("업로드대기", script);
        Assert.Contains("업로드완료", script);
        Assert.Contains("fileCategory", script);
        Assert.Contains("renderRelationshipMap", script);
        Assert.Contains("story.entity.add", script);
        Assert.Contains("story.entity.update", script);
        Assert.Contains("entityType", script);
        Assert.Contains("formatStoryEntityType", script);
        Assert.Contains("story.entity.delete", script);
        Assert.Contains("story.relationship.add", script);
        Assert.Contains("story.relationship.update", script);
        Assert.Contains("story.relationship.delete", script);
        Assert.Contains("story.layout.update", script);
        Assert.Contains("storyZoom", script);
        Assert.Contains("setStoryZoom", script);
        Assert.Contains("handleStoryMapWheel", script);
        Assert.Contains("handleStoryZoomKey", script);
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
        Assert.Contains("function getEditorParagraphTexts", script);
        Assert.Contains(".split(/\\n\\n+/)", script);
        Assert.Contains("contentLength: paragraphs.reduce((sum, paragraph) => sum + paragraph.replace(/\\s/g, \"\").length, 0)", script);
        Assert.Contains("contentLengthWithSpaces: paragraphs.reduce((sum, paragraph) => sum + paragraph.length, 0)", script);
        var updateFunctionStart = script.IndexOf("function updateActiveEditorMetrics()", StringComparison.Ordinal);
        var nextFunctionStart = script.IndexOf("\nfunction addStoryEntity", updateFunctionStart, StringComparison.Ordinal);
        var updateFunction = script[updateFunctionStart..nextFunctionStart];
        var renderFunctionStart = script.IndexOf("function renderActiveScene(active)", StringComparison.Ordinal);
        var renderFunctionEnd = script.IndexOf("\nfunction normalizeCharacterCountLabels", renderFunctionStart, StringComparison.Ordinal);
        var renderFunction = script[renderFunctionStart..renderFunctionEnd];

        Assert.Contains("$(\"active-length\").textContent = formatNumber(current.contentLength);", updateFunction);
        Assert.Contains("$(\"active-length-spaces\").textContent = formatNumber(current.contentLengthWithSpaces);", updateFunction);
        Assert.Contains("$(\"active-length\").textContent = formatNumber(visibleMetrics.contentLength);", renderFunction);
        Assert.Contains("$(\"active-length-spaces\").textContent = formatNumber(visibleMetrics.contentLengthWithSpaces);", renderFunction);
    }

    [Fact]
    public async Task WebWorkbenchCssKeepsInWindowRemoteDisabledWhenNativeLayerIsPrimary()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var cssPath = Path.Combine(appDirectory, "WebWorkbench", "styles.css");

        var css = await File.ReadAllTextAsync(cssPath, CancellationToken.None);

        Assert.Contains(".floating-remote", css);
        var remoteBlockStart = css.IndexOf(".floating-remote", StringComparison.Ordinal);
        var remoteBlockEnd = css.IndexOf("\n}", remoteBlockStart, StringComparison.Ordinal);
        var remoteBlock = css[remoteBlockStart..remoteBlockEnd];
        Assert.Contains("display: none", remoteBlock);
        Assert.DoesNotContain("display: block", remoteBlock);
        Assert.Contains("background: linear-gradient(180deg, var(--surface), var(--parchment))", remoteBlock);
        Assert.Contains("color: var(--ink)", remoteBlock);
        Assert.Contains(".remote-header strong", css);
        Assert.Contains(".remote-header button:hover,", css);
        Assert.Contains(".remote-command:hover", css);
        Assert.Contains("stroke: var(--blue)", css);
        Assert.Contains("fill: var(--blue)", css);
        Assert.Contains(".remote-command:active", css);
        Assert.Contains("font-weight: 650", css);
        Assert.Contains("#rail-panel-binder", css);
        Assert.Contains("grid-template-rows: auto auto auto minmax(0, 1fr)", css);
        Assert.Contains(".binder-filter-bar", css);
        Assert.Contains(".scene-actions", css);
        Assert.Contains(".scene-item[draggable=\"true\"]", css);
        Assert.Contains(".scene-item.drop-before", css);
        Assert.Contains(".scene-item.drop-after", css);
        Assert.Contains(".scene-metadata-editor", css);
        Assert.Contains(".relationship-workbench", css);
        Assert.Contains(".relationship-map-canvas", css);
        Assert.Contains(".story-map-content", css);
        Assert.Contains(".text-replacement-panel", css);
        Assert.Contains(".text-replacement-row", css);
        Assert.Contains(".editor-workbench", css);
        Assert.Contains(".scene-memo-panel", css);
        Assert.Contains(".memo-editor", css);
        Assert.Contains(".scene-memo-overview", css);
        Assert.Contains("right: 18px", css);
    }

    [Fact]
    public async Task WebWorkbenchCssUsesRestrainedManuscriptWorkbenchTheme()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var cssPath = Path.Combine(appDirectory, "WebWorkbench", "styles.css");

        var css = await File.ReadAllTextAsync(cssPath, CancellationToken.None);

        Assert.Contains("--parchment", css);
        Assert.Contains("--rubric", css);
        Assert.Contains("--gild", css);
        Assert.Contains("--chrome: #28313a", css);
        Assert.Contains("--blue: #245c73", css);
        Assert.Contains(":root[data-graphic-preset=\"dark\"]", css);
        Assert.Contains(":root[data-graphic-preset=\"comfort-1\"]", css);
        Assert.Contains(":root[data-graphic-preset=\"comfort-2\"]", css);
        Assert.Contains(":root[data-graphic-preset=\"comfort-3\"]", css);
        Assert.Contains(":root[data-graphic-preset=\"lavender\"]", css);
        Assert.Contains(".workbench-shell::before", css);
        Assert.Contains(".writing-sheet::before", css);
        Assert.Contains(".writing-sheet::after", css);
        Assert.Contains("\"Palatino Linotype\"", css);
        Assert.Contains("background: var(--bg);", css);
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
