namespace WriterWorkbench.Core.WebWorkbench;

public sealed record WebWorkbenchPayload(
    WebWorkbenchProject Project,
    WebWorkbenchScene? ActiveScene,
    IReadOnlyList<WebWorkbenchScene> Binder,
    IReadOnlyList<WebWorkbenchCommand> Commands,
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
    bool IsActive);

public sealed record WebWorkbenchCommand(
    string CommandId,
    string Label,
    string Category,
    string Surface,
    string Area,
    int Order);
