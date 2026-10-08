namespace WriterWorkbench.Core.Commands;

public sealed record ShortcutBinding(
    string CommandId,
    string Gesture,
    CommandScope Scope
);
