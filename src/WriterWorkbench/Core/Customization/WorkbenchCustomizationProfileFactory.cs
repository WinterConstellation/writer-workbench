using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Core.Customization;

public static class WorkbenchCustomizationProfileFactory
{
    public static WorkbenchCustomizationProfile CreateDefault(
        string profileId,
        string profileName,
        CommandRegistry commandRegistry)
    {
        var now = DateTimeOffset.UtcNow;
        var placements = commandRegistry.Placements
            .Select(ToProfilePlacement)
            .ToList();

        var shortcuts = commandRegistry.DefaultShortcuts
            .Select(binding => new ShortcutAssignment(binding.Scope.ToString(), binding.Gesture, binding.CommandId))
            .ToList();

        var macros = new List<MacroDefinition>
        {
            new(
                "macro.start-focus-writing",
                "집중 집필 시작",
                [
                    new CommandInvocation(AppCommandIds.ProjectSave, new Dictionary<string, string>()),
                    new CommandInvocation(AppCommandIds.WritingFocusToggle, new Dictionary<string, string>
                    {
                        ["durationMinutes"] = "40",
                        ["exitConfirmChars"] = "20"
                    })
                ])
        };

        return new WorkbenchCustomizationProfile(
            profileId,
            profileName,
            placements,
            shortcuts,
            macros,
            now,
            now);
    }

    public static IReadOnlyList<CommandPlacement> CreateDefaultRemoteControlPlacements(CommandRegistry commandRegistry)
    {
        return commandRegistry
            .GetPlacements("remote", "main")
            .Select(ToProfilePlacement)
            .ToList();
    }

    private static CommandPlacement ToProfilePlacement(CommandPlacementContribution placement)
    {
        return new CommandPlacement(
            placement.Surface,
            placement.Area,
            placement.SlotKey,
            placement.CommandId,
            placement.Label,
            placement.Order,
            new Dictionary<string, string>(placement.Parameters, StringComparer.OrdinalIgnoreCase));
    }
}
