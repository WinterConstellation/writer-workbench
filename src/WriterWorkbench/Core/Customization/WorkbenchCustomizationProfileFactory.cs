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
        var placements = AppCommandCatalog.CreateDefaultCommandSlots()
            .Select(slot =>
            {
                var command = commandRegistry.Get(slot.CommandId);
                return new CommandPlacement(
                    "toolbar",
                    "main",
                    $"slot-{slot.Slot:00}",
                    command.Id,
                    slot.Label,
                    slot.Slot,
                    new Dictionary<string, string>());
            })
            .ToList();

        var shortcuts = new List<ShortcutAssignment>
        {
            new("Editor", "Ctrl+S", AppCommandIds.ProjectSave),
            new("Workbench", "Ctrl+Alt+1", AppCommandIds.WorkspacePresetOne),
            new("Workbench", "Ctrl+Alt+2", AppCommandIds.WorkspacePresetTwo),
            new("Workbench", "Ctrl+Alt+3", AppCommandIds.WorkspacePresetThree),
            new("Workbench", "Ctrl+Alt+0", AppCommandIds.WorkspaceStartupPresetCycle),
            new("Workbench", "Ctrl+Alt+K", AppCommandIds.ShortcutsOpenSettings),
            new("Workbench", "Ctrl+Alt+P", AppCommandIds.ViewPreviewToggle),
            new("Global", "F1", AppCommandIds.HelpOpen)
        };

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
}
