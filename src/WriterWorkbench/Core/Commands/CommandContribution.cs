namespace WriterWorkbench.Core.Commands;

public sealed record CommandContribution(
    AppCommand Command,
    IReadOnlyList<CommandPlacementContribution> Placements,
    IReadOnlyList<ShortcutBinding> Shortcuts);

public sealed record CommandPlacementContribution(
    string Surface,
    string Area,
    string SlotKey,
    string CommandId,
    string Label,
    int Order,
    Dictionary<string, string> Parameters);
