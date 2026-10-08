using WriterWorkbench.Core.AppSettings;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.WebWorkbench;

public static class WebWorkbenchPayloadFactory
{
    public static WebWorkbenchPayload Create(
        ProjectManifest manifest,
        string projectRoot,
        WriterDocument? activeDocument,
        SceneMetadata? activeMetadata,
        IReadOnlyDictionary<string, SceneMetadata> metadataByDocumentId,
        WorkbenchCustomizationProfile profile,
        CommandRegistry commandRegistry,
        string statusText,
        string graphicPresetName,
        bool autosaveEnabled,
        WorkbenchWidgetRegistry? widgetRegistry = null,
        string activeView = "editor",
        string previewText = "",
        IReadOnlyList<ShortcutBinding>? shortcutBindings = null,
        WebWorkbenchStory? story = null,
        IReadOnlyList<WebWorkbenchTrashItem>? trash = null,
        IReadOnlyList<WebWorkbenchSettingsBookItem>? settingsBook = null,
        IReadOnlyList<WebWorkbenchTextReplacementRule>? textReplacements = null,
        WebWorkbenchWordAnalysis? wordAnalysis = null,
        string graphicPresetId = "default",
        WebWorkbenchCodexCliState? codexCli = null)
    {
        var activeDocumentId = activeDocument?.Id ?? activeMetadata?.DocumentId ?? "";
        var activeEditorView = activeDocument is null
            ? DocumentEditorTextView.Empty
            : DocumentEditorTextService.CreateView(activeDocument);
        var binder = manifest.Documents
            .Select(document =>
            {
                metadataByDocumentId.TryGetValue(document.Id, out var metadata);
                if (metadata is null && string.Equals(document.Id, activeMetadata?.DocumentId, StringComparison.OrdinalIgnoreCase))
                {
                    metadata = activeMetadata;
                }

                return CreateScene(document, metadata, activeDocumentId, activeEditorView);
            })
            .ToList();

        var activeScene = binder.FirstOrDefault(scene => scene.IsActive)
                          ?? (activeDocument is null
                              ? null
                              : new WebWorkbenchScene(
                                  activeDocument.Id,
                                   activeDocument.Title,
                                   FormatSceneStatus(activeMetadata?.Status ?? SceneStatus.Draft),
                                   activeMetadata?.FileCategory ?? "원고",
                                   activeMetadata?.Summary ?? "",
                                  activeMetadata?.Tags.ToList() ?? [],
                                  activeMetadata?.ContentLength ?? 0,
                                  activeMetadata?.ContentLengthWithSpaces ?? 0,
                                  activeMetadata?.SceneType ?? "Scene",
                                  activeMetadata?.UpdatedAt ?? DateTimeOffset.MinValue,
                                  true,
                                  activeEditorView.Text,
                                  activeMetadata?.Memo ?? ""));

        var resolver = new WorkbenchCustomizationResolver(profile);
        var commands = CreateCommands(resolver.GetPlacements("toolbar", "main"), commandRegistry);
        var defaultMenuCommands = CreateCommands(CreateMenuPlacementsWithBaseline(profile), commandRegistry);
        var menuCommands = MergeMenuCommands(
            CreateCommandsFromWidgets(widgetRegistry, "menu", commandRegistry),
            defaultMenuCommands);
        EnsureRequiredMenuCommand(menuCommands, commandRegistry, AppCommandIds.ViewEditorOpen, "top.view", "view.editor", "작품 수정");
        EnsureRequiredMenuCommand(menuCommands, commandRegistry, AppCommandIds.CodexOpen, "top.tools", "tools.codex", "Codex");
        var remoteCommands = CreateCommands(resolver.GetPlacements("remote", "main"), commandRegistry);
        if (remoteCommands.Count == 0)
        {
            remoteCommands = CreateCommands(resolver.GetPlacements("remote", "floating"), commandRegistry);
        }
        if (remoteCommands.Count == 0)
        {
            remoteCommands = CreateCommandsFromWidgets(widgetRegistry, "remote", commandRegistry) ?? [];
        }
        var availableCommands = CreateAvailableCommands(commandRegistry);
        var htmlShortcutBindings = CreateShortcutBindings(shortcutBindings, profile, commandRegistry);

        return new WebWorkbenchPayload(
            new WebWorkbenchProject(manifest.Title, projectRoot, manifest.Documents.Count),
            activeScene,
            binder,
            commands,
            menuCommands,
            remoteCommands,
            availableCommands,
            htmlShortcutBindings,
            statusText,
            graphicPresetName,
            NormalizeGraphicPresetId(graphicPresetId),
            autosaveEnabled,
            NormalizeActiveView(activeView),
            previewText ?? "",
            story,
            trash ?? [],
            settingsBook ?? [],
            textReplacements ?? [],
            wordAnalysis,
            codexCli);
    }

    private static string NormalizeGraphicPresetId(string? graphicPresetId)
    {
        var normalized = (graphicPresetId ?? "").Trim();
        return normalized is
            "default" or
            "dark" or
            "comfort-1" or
            "comfort-2" or
            "comfort-3" or
            "lavender"
            ? normalized
            : "default";
    }

    private static string NormalizeActiveView(string? activeView)
    {
        var normalized = (activeView ?? "").Trim();
        return normalized is
            "editor" or
            "preview" or
            "relationship-map" or
            "shortcuts" or
            "remote-settings" or
            "codex" or
            "help"
            ? normalized
            : "editor";
    }

    private static void EnsureRequiredMenuCommand(
        List<WebWorkbenchCommand> commands,
        CommandRegistry commandRegistry,
        string commandId,
        string area,
        string slotKey,
        string label)
    {
        if (commands.Any(command => string.Equals(command.CommandId, commandId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var command = commandRegistry.Get(commandId);
        var order = commands.Count == 0 ? 10 : commands.Max(item => item.Order) + 10;
        commands.Add(new WebWorkbenchCommand(
            command.Id,
            label,
            command.Category,
            "menu",
            area,
            slotKey,
            order));
    }

    private static IReadOnlyList<CommandPlacement> CreateMenuPlacementsWithBaseline(WorkbenchCustomizationProfile profile)
    {
        var placements = profile.Placements
            .Where(placement => string.Equals(placement.Surface, "menu", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var commandIds = placements
            .Select(placement => placement.CommandId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var placement in WorkbenchCustomizationProfileFactory.CreateDefaultTopMenuPlacements())
        {
            if (commandIds.Add(placement.CommandId))
            {
                placements.Add(placement);
            }
        }

        return placements
            .OrderBy(placement => placement.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(placement => placement.Order)
            .ThenBy(placement => placement.Label, StringComparer.CurrentCulture)
            .ToList();
    }

    private static List<WebWorkbenchCommand> MergeMenuCommands(
        IReadOnlyList<WebWorkbenchCommand>? primary,
        IReadOnlyList<WebWorkbenchCommand> defaults)
    {
        var merged = (primary ?? []).ToList();
        var commandIds = merged
            .Select(command => command.CommandId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var command in defaults)
        {
            if (commandIds.Add(command.CommandId))
            {
                merged.Add(command);
            }
        }

        return merged
            .OrderBy(command => command.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(command => command.Order)
            .ThenBy(command => command.Label, StringComparer.CurrentCulture)
            .ToList();
    }

    private static List<WebWorkbenchCommand> CreateCommands(
        IReadOnlyList<CommandPlacement> placements,
        CommandRegistry commandRegistry)
    {
        return placements
            .Select(placement =>
            {
                var commandId = AppCommandIds.NormalizeLegacyId(placement.CommandId);
                var command = commandRegistry.Get(commandId);
                return new WebWorkbenchCommand(
                    command.Id,
                    string.IsNullOrWhiteSpace(placement.Label) ? command.Name : placement.Label,
                    command.Category,
                    placement.Surface,
                    placement.Area,
                    placement.SlotKey,
                    placement.Order);
            })
            .ToList();
    }

    private static List<WebWorkbenchCommand>? CreateCommandsFromWidgets(
        WorkbenchWidgetRegistry? widgetRegistry,
        string surface,
        CommandRegistry commandRegistry)
    {
        var widgets = (widgetRegistry?.Instances ?? [])
            .Where(widget => string.Equals(widget.Surface, surface, StringComparison.OrdinalIgnoreCase))
            .OrderBy(widget => widget.Order)
            .ThenBy(widget => widget.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(widget => widget.SlotKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (widgets.Count == 0)
        {
            return null;
        }

        return widgets
            .Select(widget =>
            {
                var commandId = AppCommandIds.NormalizeLegacyId(widget.CommandId);
                var command = commandRegistry.Get(commandId);
                return new WebWorkbenchCommand(
                    command.Id,
                    string.IsNullOrWhiteSpace(widget.Label) ? command.Name : widget.Label,
                    command.Category,
                    widget.Surface,
                    widget.Area,
                    widget.SlotKey,
                    widget.Order);
            })
            .ToList();
    }

    private static IReadOnlyList<WebWorkbenchCommand> CreateAvailableCommands(CommandRegistry commandRegistry)
    {
        return commandRegistry.All
            .Select((command, index) => new WebWorkbenchCommand(
                command.Id,
                command.Name,
                command.Category,
                "catalog",
                "catalog",
                command.Id,
                index + 1))
            .ToList();
    }

    private static IReadOnlyList<WebWorkbenchShortcut> CreateShortcutBindings(
        IReadOnlyList<ShortcutBinding>? shortcutBindings,
        WorkbenchCustomizationProfile profile,
        CommandRegistry commandRegistry)
    {
        var commandById = commandRegistry.All
            .ToDictionary(command => command.Id, StringComparer.OrdinalIgnoreCase);
        var shortcuts = shortcutBindings is null
            ? profile.Shortcuts.Select(shortcut => (CommandId: shortcut.CommandId, Scope: shortcut.Scope, shortcut.Gesture))
            : shortcutBindings.Select(shortcut => (shortcut.CommandId, Scope: shortcut.Scope.ToString(), shortcut.Gesture));

        return shortcuts
            .Select(shortcut => CreateShortcut(shortcut.CommandId, shortcut.Scope, shortcut.Gesture, commandById))
            .Where(shortcut => shortcut is not null)
            .Select(shortcut => shortcut!)
            .OrderBy(shortcut => shortcut.Category, StringComparer.CurrentCulture)
            .ThenBy(shortcut => shortcut.CommandName, StringComparer.CurrentCulture)
            .ToList();
    }

    private static WebWorkbenchShortcut? CreateShortcut(
        string commandId,
        string scope,
        string gesture,
        IReadOnlyDictionary<string, AppCommand> commandById)
    {
        commandId = AppCommandIds.NormalizeLegacyId(commandId);
        if (!commandById.TryGetValue(commandId, out var command))
        {
            return null;
        }

        return new WebWorkbenchShortcut(
            command.Id,
            command.Name,
            command.Category,
            scope,
            gesture);
    }

    private static WebWorkbenchScene CreateScene(
        ProjectDocumentInfo document,
        SceneMetadata? metadata,
        string activeDocumentId,
        DocumentEditorTextView activeEditorView)
    {
        var isActive = string.Equals(document.Id, activeDocumentId, StringComparison.OrdinalIgnoreCase);
        return new WebWorkbenchScene(
            document.Id,
            document.Title,
            FormatSceneStatus(metadata?.Status ?? SceneStatus.Draft),
            metadata?.FileCategory ?? "원고",
            metadata?.Summary ?? "",
            metadata?.Tags.ToList() ?? [],
            metadata?.ContentLength ?? 0,
            metadata?.ContentLengthWithSpaces ?? 0,
            metadata?.SceneType ?? "Scene",
            metadata?.UpdatedAt == default ? document.UpdatedAt : metadata!.UpdatedAt,
            isActive,
            isActive ? activeEditorView.Text : "",
            metadata?.Memo ?? "");
    }

    private static string FormatSceneStatus(SceneStatus status)
    {
        return status switch
        {
            SceneStatus.Draft => "초고",
            SceneStatus.Revising => "퇴고중",
            SceneStatus.RevisionComplete or SceneStatus.Final => "퇴고완료",
            SceneStatus.UploadPending => "업로드대기",
            SceneStatus.Uploaded => "업로드완료",
            SceneStatus.Excluded => "제외",
            _ => status.ToString()
        };
    }
}
