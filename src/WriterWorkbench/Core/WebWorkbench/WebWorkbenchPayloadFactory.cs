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
        bool autosaveEnabled)
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

        var commands = new WorkbenchCustomizationResolver(profile)
            .GetPlacements("toolbar", "main")
            .Select(placement =>
            {
                var command = commandRegistry.Get(placement.CommandId);
                return new WebWorkbenchCommand(
                    command.Id,
                    string.IsNullOrWhiteSpace(placement.Label) ? command.Name : placement.Label,
                    command.Category,
                    placement.Surface,
                    placement.Area,
                    placement.Order);
            })
            .ToList();

        return new WebWorkbenchPayload(
            new WebWorkbenchProject(manifest.Title, projectRoot, manifest.Documents.Count),
            activeScene,
            binder,
            commands,
            statusText,
            graphicPresetName,
            autosaveEnabled);
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
