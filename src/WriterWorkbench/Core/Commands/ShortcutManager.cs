namespace WriterWorkbench.Core.Commands;

public sealed class ShortcutManager
{
    private readonly List<ShortcutBinding> _bindings = [];

    public IReadOnlyList<ShortcutBinding> Bindings => _bindings
        .OrderBy(binding => binding.Scope)
        .ThenBy(binding => binding.CommandId, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public bool TryBind(ShortcutBinding binding, out string? conflictCommandId)
    {
        binding = binding with { Gesture = NormalizeGesture(binding.Gesture) };
        var conflict = _bindings.FirstOrDefault(existing =>
            existing.Scope == binding.Scope &&
            string.Equals(existing.Gesture, binding.Gesture, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.CommandId, binding.CommandId, StringComparison.OrdinalIgnoreCase));

        if (conflict is not null)
        {
            conflictCommandId = conflict.CommandId;
            return false;
        }

        _bindings.RemoveAll(existing =>
            existing.Scope == binding.Scope &&
            string.Equals(existing.CommandId, binding.CommandId, StringComparison.OrdinalIgnoreCase));
        _bindings.Add(binding);

        conflictCommandId = null;
        return true;
    }

    public string? FindCommand(string gesture, CommandScope scope)
    {
        var normalizedGesture = NormalizeGesture(gesture);
        var scoped = _bindings.FirstOrDefault(binding =>
            binding.Scope == scope &&
            string.Equals(binding.Gesture, normalizedGesture, StringComparison.OrdinalIgnoreCase));

        if (scoped is not null)
        {
            return scoped.CommandId;
        }

        return _bindings.FirstOrDefault(binding =>
            binding.Scope == CommandScope.Global &&
            string.Equals(binding.Gesture, normalizedGesture, StringComparison.OrdinalIgnoreCase))?.CommandId;
    }

    private static string NormalizeGesture(string gesture)
    {
        return string.Join(
            '+',
            gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Equals("Control", StringComparison.OrdinalIgnoreCase) ? "Ctrl" : part)
                .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }
}
