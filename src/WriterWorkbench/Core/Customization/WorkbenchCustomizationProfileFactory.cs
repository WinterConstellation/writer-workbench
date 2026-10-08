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
        placements.AddRange(CreateDefaultTopMenuPlacements());

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

    public static IReadOnlyList<CommandPlacement> CreateDefaultTopMenuPlacements()
    {
        return
        [
            new CommandPlacement("menu", "top.project", "project.new", AppCommandIds.ProjectNew, "새 프로젝트", 10, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.project", "project.open", AppCommandIds.ProjectOpen, "열기", 20, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.project", "project.save", AppCommandIds.ProjectSave, "저장", 30, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.manuscript", "document.create", AppCommandIds.DocumentCreateScene, "새 장면", 10, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.manuscript", "document.exportCurrent", AppCommandIds.ExportCurrentScene, "현재 장면 TXT", 20, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.manuscript", "document.exportFull", AppCommandIds.ExportFullManuscript, "전체 원고 TXT", 30, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.story", "story.relationship", AppCommandIds.StoryRelationshipMapOpen, "관계도", 10, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.story", "story.addNode", AppCommandIds.StoryAddNode, "캐릭터 추가", 20, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.view", "view.main", AppCommandIds.ViewMainOpen, "메인", 10, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.view", "view.editor", AppCommandIds.ViewEditorOpen, "작품 수정", 20, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.view", "view.preview", AppCommandIds.ViewPreviewToggle, "미리보기", 30, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.view", "view.fullscreen", AppCommandIds.ViewFullscreenToggle, "전체화면", 40, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.tools", "tools.focus", AppCommandIds.WritingFocusToggle, "집중", 10, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.tools", "tools.remote", AppCommandIds.RemoteControlToggle, "리모컨", 20, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.tools", "tools.shortcuts", AppCommandIds.ShortcutsOpenSettings, "단축키", 30, new Dictionary<string, string>()),
            new CommandPlacement("menu", "top.help", "help.open", AppCommandIds.HelpOpen, "도움말", 10, new Dictionary<string, string>())
        ];
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
