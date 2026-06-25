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
        placements.AddRange(CreateDefaultRemoteControlPlacements(commandRegistry));

        var shortcuts = new List<ShortcutAssignment>
        {
            new("Editor", "Ctrl+S", AppCommandIds.ProjectSave),
            new("Workbench", "Ctrl+Alt+1", AppCommandIds.WorkspacePresetOne),
            new("Workbench", "Ctrl+Alt+2", AppCommandIds.WorkspacePresetTwo),
            new("Workbench", "Ctrl+Alt+3", AppCommandIds.WorkspacePresetThree),
            new("Workbench", "Ctrl+Alt+0", AppCommandIds.WorkspaceStartupPresetCycle),
            new("Workbench", "Ctrl+Alt+K", AppCommandIds.ShortcutsOpenSettings),
            new("Workbench", "Ctrl+Alt+P", AppCommandIds.ViewPreviewToggle),
            new("Workbench", "Ctrl+Alt+R", AppCommandIds.RemoteControlToggle),
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

    public static IReadOnlyList<CommandPlacement> CreateDefaultRemoteControlPlacements(CommandRegistry commandRegistry)
    {
        var slots = new[]
        {
            new CommandSlot(1, AppCommandIds.ProjectSave, "저장"),
            new CommandSlot(2, AppCommandIds.DocumentCreateScene, "새 장면"),
            new CommandSlot(3, AppCommandIds.StoryRelationshipMapOpen, "관계도"),
            new CommandSlot(4, AppCommandIds.ExportFullManuscript, "전체 TXT"),
            new CommandSlot(5, AppCommandIds.DocumentDetachCurrent, "창 분리"),
            new CommandSlot(6, AppCommandIds.SnapshotCreateCurrent, "스냅샷")
        };

        return slots
            .Select(slot =>
            {
                var command = commandRegistry.Get(slot.CommandId);
                return new CommandPlacement(
                    "remote",
                    "main",
                    $"remote-{slot.Slot:00}",
                    command.Id,
                    slot.Label,
                    slot.Slot,
                    new Dictionary<string, string>());
            })
            .ToList();
    }
}
