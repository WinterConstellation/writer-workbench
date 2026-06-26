namespace WriterWorkbench.Core.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, AppCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CommandPlacementContribution> _placements = [];
    private readonly List<ShortcutBinding> _defaultShortcuts = [];

    public IReadOnlyList<AppCommand> All => _commands.Values
        .OrderBy(command => command.Id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public IReadOnlyList<CommandPlacementContribution> Placements => _placements
        .OrderBy(placement => placement.Surface, StringComparer.OrdinalIgnoreCase)
        .ThenBy(placement => placement.Area, StringComparer.OrdinalIgnoreCase)
        .ThenBy(placement => placement.Order)
        .ThenBy(placement => placement.SlotKey, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public IReadOnlyList<ShortcutBinding> DefaultShortcuts => _defaultShortcuts
        .OrderBy(binding => binding.Scope)
        .ThenBy(binding => binding.CommandId, StringComparer.OrdinalIgnoreCase)
        .ThenBy(binding => binding.Gesture, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public void Register(AppCommand command)
    {
        Register(new CommandContribution(command, [], []));
    }

    public void Register(CommandContribution contribution)
    {
        var command = contribution.Command;
        if (_commands.ContainsKey(command.Id))
        {
            throw new InvalidOperationException($"Command already registered: {command.Id}");
        }

        var normalizedPlacements = NormalizePlacements(command.Id, contribution.Placements);
        var normalizedShortcuts = NormalizeShortcuts(command.Id, contribution.Shortcuts);
        ValidatePlacements(command.Id, normalizedPlacements);
        ValidateShortcuts(command.Id, normalizedShortcuts);

        _commands.Add(command.Id, command);
        _placements.AddRange(normalizedPlacements);
        _defaultShortcuts.AddRange(normalizedShortcuts);
    }

    public AppCommand Get(string id)
    {
        return _commands.TryGetValue(id, out var command)
            ? command
            : throw new KeyNotFoundException($"Command not found: {id}");
    }

    public IReadOnlyList<CommandPlacementContribution> GetPlacements(string surface, string area)
    {
        return _placements
            .Where(placement =>
                string.Equals(placement.Surface, surface, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(placement.Area, area, StringComparison.OrdinalIgnoreCase))
            .OrderBy(placement => placement.Order)
            .ThenBy(placement => placement.SlotKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<CommandPlacementContribution> NormalizePlacements(
        string commandId,
        IReadOnlyList<CommandPlacementContribution> placements)
    {
        return placements
            .Select(placement =>
            {
                if (!string.Equals(placement.CommandId, commandId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Command placement {placement.Surface}/{placement.Area}/{placement.SlotKey} points to {placement.CommandId}, not {commandId}.");
                }

                return placement with
                {
                    Surface = NormalizeRequired(placement.Surface, "placement surface"),
                    Area = NormalizeRequired(placement.Area, "placement area"),
                    SlotKey = NormalizeRequired(placement.SlotKey, "placement slot"),
                    CommandId = commandId,
                    Label = (placement.Label ?? "").Trim(),
                    Parameters = NormalizeParameters(placement.Parameters)
                };
            })
            .ToList();
    }

    private static IReadOnlyList<ShortcutBinding> NormalizeShortcuts(
        string commandId,
        IReadOnlyList<ShortcutBinding> shortcuts)
    {
        return shortcuts
            .Select(shortcut =>
            {
                if (!string.Equals(shortcut.CommandId, commandId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Shortcut {shortcut.Scope}/{shortcut.Gesture} points to {shortcut.CommandId}, not {commandId}.");
                }

                return shortcut with
                {
                    CommandId = commandId,
                    Gesture = ShortcutManager.NormalizeGesture(shortcut.Gesture)
                };
            })
            .ToList();
    }

    private void ValidatePlacements(string commandId, IReadOnlyList<CommandPlacementContribution> placements)
    {
        var newKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var placement in placements)
        {
            var key = $"{placement.Surface}/{placement.Area}/{placement.SlotKey}";
            if (!newKeys.Add(key) || _placements.Any(existing =>
                    string.Equals(existing.Surface, placement.Surface, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Area, placement.Area, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.SlotKey, placement.SlotKey, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Command placement slot already registered: {key} for {commandId}");
            }
        }
    }

    private void ValidateShortcuts(string commandId, IReadOnlyList<ShortcutBinding> shortcuts)
    {
        var newKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shortcut in shortcuts)
        {
            var key = $"{shortcut.Scope}/{shortcut.Gesture}";
            if (!newKeys.Add(key))
            {
                throw new InvalidOperationException($"Default shortcut already registered: {key} for {commandId}");
            }

            var conflict = _defaultShortcuts.FirstOrDefault(existing =>
                existing.Scope == shortcut.Scope &&
                string.Equals(existing.Gesture, shortcut.Gesture, StringComparison.OrdinalIgnoreCase));
            if (conflict is not null)
            {
                throw new InvalidOperationException(
                    $"Default shortcut {key} for {commandId} conflicts with {conflict.CommandId}");
            }
        }
    }

    private static Dictionary<string, string> NormalizeParameters(Dictionary<string, string>? parameters)
    {
        return (parameters ?? [])
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                pair => pair.Key.Trim(),
                pair => pair.Value?.Trim() ?? "",
                StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException($"Command registry requires {fieldName}.");
        }

        return normalized;
    }
}
