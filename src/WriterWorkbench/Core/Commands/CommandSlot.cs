namespace WriterWorkbench.Core.Commands;

public sealed record CommandSlot(
    int Slot,
    string CommandId,
    string Label
);
