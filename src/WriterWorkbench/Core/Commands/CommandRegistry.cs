namespace WriterWorkbench.Core.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, AppCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AppCommand> All => _commands.Values
        .OrderBy(command => command.Id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public void Register(AppCommand command)
    {
        if (!_commands.TryAdd(command.Id, command))
        {
            throw new InvalidOperationException($"Command already registered: {command.Id}");
        }
    }

    public AppCommand Get(string id)
    {
        return _commands.TryGetValue(id, out var command)
            ? command
            : throw new KeyNotFoundException($"Command not found: {id}");
    }
}
