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
        WorkbenchWidgetRegistry? widgetRegistry = null)
    {
        var activeDocumentId = activeDocument?.Id ?? activeMetadata?.DocumentId ?? "";
        var binder = manifest.Documents
            .Select(document =>
            {
                metadataByDocumentId.TryGetValue(document.Id, out var metadata);
                if (metadata is null && string.Equals(document.Id, activeMetadata?.DocumentId, StringComparison.OrdinalIgnoreCase))
                {
                    metadata = activeMetadata;
                }

                return CreateScene(document, metadata, activeDocumentId);
            })
            .ToList();

        var activeScene = binder.FirstOrDefault(scene => scene.IsActive)
                          ?? (activeDocument is null
                              ? null
                              : new WebWorkbenchScene(
                                  activeDocument.Id,
                                  activeDocument.Title,
                                  FormatSceneStatus(activeMetadata?.Status ?? SceneStatus.Draft),
                                  activeMetadata?.Summary ?? "",
                                  activeMetadata?.Tags.ToList() ?? [],
                                  activeMetadata?.ContentLength ?? 0,
                                  activeMetadata?.ContentLengthWithSpaces ?? 0,
                                  activeMetadata?.SceneType ?? "Scene",
                                  activeMetadata?.UpdatedAt ?? DateTimeOffset.MinValue,
                                  true));

        var resolver = new WorkbenchCustomizationResolver(profile);
        var commands = CreateCommands(resolver.GetPlacements("toolbar", "main"), commandRegistry);
        var menuCommands = CreateCommandsFromWidgets(widgetRegistry, "menu", commandRegistry)
            ?? CreateCommands(
                profile.Placements
                    .Where(placement => string.Equals(placement.Surface, "menu", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(placement => placement.Order)
                    .ToList(),
                commandRegistry);
        EnsureRequiredMenuCommand(menuCommands, commandRegistry, AppCommandIds.ViewEditorOpen, "top.view", "view.editor", "작품 수정");
        var remoteCommands = CreateCommandsFromWidgets(widgetRegistry, "remote", commandRegistry)
            ?? CreateCommands(resolver.GetPlacements("remote", "main"), commandRegistry);
        if (remoteCommands.Count == 0)
        {
            remoteCommands = CreateCommands(resolver.GetPlacements("remote", "floating"), commandRegistry);
        }

        return new WebWorkbenchPayload(
            new WebWorkbenchProject(manifest.Title, projectRoot, manifest.Documents.Count),
            activeScene,
            binder,
            commands,
            menuCommands,
            remoteCommands,
            statusText,
            graphicPresetName,
            autosaveEnabled);
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

    private static List<WebWorkbenchCommand> CreateCommands(
        IReadOnlyList<CommandPlacement> placements,
        CommandRegistry commandRegistry)
    {
        return placements
            .Select(placement =>
            {
                var command = commandRegistry.Get(placement.CommandId);
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
                var command = commandRegistry.Get(widget.CommandId);
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

    private static WebWorkbenchScene CreateScene(
        ProjectDocumentInfo document,
        SceneMetadata? metadata,
        string activeDocumentId)
    {
        return new WebWorkbenchScene(
            document.Id,
            document.Title,
            FormatSceneStatus(metadata?.Status ?? SceneStatus.Draft),
            metadata?.Summary ?? "",
            metadata?.Tags.ToList() ?? [],
            metadata?.ContentLength ?? 0,
            metadata?.ContentLengthWithSpaces ?? 0,
            metadata?.SceneType ?? "Scene",
            metadata?.UpdatedAt == default ? document.UpdatedAt : metadata!.UpdatedAt,
            string.Equals(document.Id, activeDocumentId, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatSceneStatus(SceneStatus status)
    {
        return status switch
        {
            SceneStatus.Draft => "초고",
            SceneStatus.Revising => "수정중",
            SceneStatus.Final => "완료",
            SceneStatus.Excluded => "제외",
            _ => status.ToString()
        };
    }
}
