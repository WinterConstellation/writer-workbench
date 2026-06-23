using WriterWorkbench.Core.Commands;

namespace WriterWorkbench;

public sealed class ShortcutSettingsRow(
    string commandId,
    string name,
    string category,
    CommandScope scope,
    string gesture)
{
    public string CommandId { get; set; } = commandId;

    public string Name { get; set; } = name;

    public string Category { get; set; } = category;

    public CommandScope Scope { get; set; } = scope;

    public string Gesture { get; set; } = gesture;
}
