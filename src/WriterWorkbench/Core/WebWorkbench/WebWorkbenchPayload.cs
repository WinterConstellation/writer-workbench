namespace WriterWorkbench.Core.WebWorkbench;

public sealed record WebWorkbenchPayload(
    WebWorkbenchProject Project,
    WebWorkbenchScene? ActiveScene,
    IReadOnlyList<WebWorkbenchScene> Binder,
    IReadOnlyList<WebWorkbenchCommand> Commands,
    IReadOnlyList<WebWorkbenchCommand> MenuCommands,
    IReadOnlyList<WebWorkbenchCommand> RemoteCommands,
    IReadOnlyList<WebWorkbenchCommand> AvailableCommands,
    IReadOnlyList<WebWorkbenchShortcut> ShortcutBindings,
    string StatusText,
    string GraphicPresetName,
    string GraphicPresetId,
    bool AutosaveEnabled,
    string ActiveView = "editor",
    string PreviewText = "",
    WebWorkbenchStory? Story = null,
    IReadOnlyList<WebWorkbenchTrashItem>? Trash = null,
    IReadOnlyList<WebWorkbenchSettingsBookItem>? SettingsBook = null);

public sealed record WebWorkbenchProject(
    string Title,
    string RootPath,
    int SceneCount);

public sealed record WebWorkbenchScene(
    string Id,
    string Title,
    string Status,
    string FileCategory,
    string Summary,
    IReadOnlyList<string> Tags,
    int ContentLength,
    int ContentLengthWithSpaces,
    string SceneType,
    DateTimeOffset UpdatedAt,
    bool IsActive,
    string EditorText,
    string Memo);

public sealed record WebWorkbenchCommand(
    string CommandId,
    string Label,
    string Category,
    string Surface,
    string Area,
    string SlotKey,
    int Order);

public sealed record WebWorkbenchShortcut(
    string CommandId,
    string CommandName,
    string Category,
    string Scope,
    string Gesture);

public sealed record WebWorkbenchStory(
    IReadOnlyList<WebWorkbenchStoryEntity> Entities,
    IReadOnlyList<WebWorkbenchStoryRelationship> Relationships);

public sealed record WebWorkbenchStoryEntity(
    string Id,
    string Type,
    string Name,
    string Role,
    string Summary,
    string Color,
    IReadOnlyList<string> Tags,
    double X,
    double Y);

public sealed record WebWorkbenchStoryRelationship(
    string Id,
    string SourceEntityId,
    string TargetEntityId,
    string Label,
    string Notes,
    bool IsDirectional);

public sealed record WebWorkbenchTrashItem(
    string TrashId,
    string DocumentId,
    string Title,
    DateTimeOffset DeletedAt);

public sealed record WebWorkbenchSettingsBookItem(
    string Id,
    string Category,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt);
