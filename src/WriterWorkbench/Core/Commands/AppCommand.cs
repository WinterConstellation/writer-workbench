namespace WriterWorkbench.Core.Commands;

public sealed record AppCommand(
    string Id,
    string Name,
    string Category,
    CommandScope DefaultScope
);
