namespace WriterWorkbench.Core.Customization;

public sealed record WorkbenchCustomizationProfile(
    string Id,
    string Name,
    IReadOnlyList<CommandPlacement> Placements,
    IReadOnlyList<ShortcutAssignment> Shortcuts,
    IReadOnlyList<MacroDefinition> Macros,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CommandPlacement(
    string Surface,
    string Area,
    string SlotKey,
    string CommandId,
    string Label,
    int Order,
    Dictionary<string, string> Parameters);

public sealed record ShortcutAssignment(
    string Scope,
    string Gesture,
    string CommandId);

public sealed record MacroDefinition(
    string Id,
    string Name,
    IReadOnlyList<CommandInvocation> Steps);

public sealed record CommandInvocation(
    string CommandId,
    Dictionary<string, string> Parameters);
