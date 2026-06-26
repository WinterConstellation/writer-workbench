namespace WriterWorkbench.Core.WebWorkbench;

public sealed record WebWorkbenchPayload(
    WebWorkbenchProject Project,
    WebWorkbenchScene? ActiveScene,
    IReadOnlyList<WebWorkbenchScene> Binder,
    IReadOnlyList<WebWorkbenchCommand> Commands,
    IReadOnlyList<WebWorkbenchCommand> MenuCommands,
    IReadOnlyList<WebWorkbenchCommand> RemoteCommands,
    string StatusText,
    string GraphicPresetName,
    bool AutosaveEnabled);

public sealed record WebWorkbenchProject(
    string Title,
    string RootPath,
    int SceneCount);

public sealed record WebWorkbenchScene(
    string Id,
    string Title,
    string Status,
    string Summary,
    IReadOnlyList<string> Tags,
    int ContentLength,
    int ContentLengthWithSpaces,
    string SceneType,
    DateTimeOffset UpdatedAt,
    bool IsActive,
    string EditorText,
    bool IsSegmentMode,
    int VisibleParagraphCount);

public sealed record WebWorkbenchCommand(
    string CommandId,
    string Label,
    string Category,
    string Surface,
    string Area,
    string SlotKey,
    int Order);
