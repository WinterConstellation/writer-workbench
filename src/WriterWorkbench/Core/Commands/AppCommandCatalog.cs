namespace WriterWorkbench.Core.Commands;

public static class AppCommandCatalog
{
    public static CommandRegistry CreateDefaultRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new AppCommand(AppCommandIds.ProjectNew, "새 프로젝트", "프로젝트", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ProjectOpen, "프로젝트 열기", "프로젝트", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentCreateScene, "새 장면", "문서", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentCreateStressLarge, "스트레스 15k", "문서", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentDetachCurrent, "창 분리", "문서", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ProjectSave, "저장", "프로젝트", CommandScope.Editor));
        registry.Register(new AppCommand(AppCommandIds.WritingFocusToggle, "집중", "집필", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspacePresetOne, "프리셋 1", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspacePresetTwo, "프리셋 2", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspacePresetThree, "프리셋 3", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspaceStartupPresetCycle, "시작 프리셋", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ShortcutsOpenSettings, "단축키", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ViewMainOpen, "메인", "보기", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ViewPreviewToggle, "미리보기", "보기", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SearchRun, "검색", "검색", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.AutosaveToggle, "자동저장", "프로젝트", CommandScope.Workbench));
        return registry;
    }

    public static IReadOnlyList<CommandSlot> CreateDefaultCommandSlots()
    {
        return
        [
            new CommandSlot(1, AppCommandIds.ProjectNew, "새 프로젝트"),
            new CommandSlot(2, AppCommandIds.ProjectOpen, "프로젝트 열기"),
            new CommandSlot(3, AppCommandIds.DocumentCreateScene, "새 장면"),
            new CommandSlot(4, AppCommandIds.ProjectSave, "저장"),
            new CommandSlot(5, AppCommandIds.WritingFocusToggle, "집중 40:00"),
            new CommandSlot(6, AppCommandIds.WorkspacePresetOne, "프리셋 1"),
            new CommandSlot(7, AppCommandIds.AutosaveToggle, "자동저장 켬"),
            new CommandSlot(8, AppCommandIds.DocumentCreateStressLarge, "스트레스 15k"),
            new CommandSlot(9, AppCommandIds.DocumentDetachCurrent, "창 분리"),
            new CommandSlot(10, AppCommandIds.WorkspacePresetTwo, "프리셋 2"),
            new CommandSlot(11, AppCommandIds.WorkspacePresetThree, "프리셋 3"),
            new CommandSlot(12, AppCommandIds.WorkspaceStartupPresetCycle, "시작 적용 끔"),
            new CommandSlot(13, AppCommandIds.ShortcutsOpenSettings, "단축키"),
            new CommandSlot(14, AppCommandIds.ViewMainOpen, "메인"),
            new CommandSlot(15, AppCommandIds.ViewPreviewToggle, "미리보기")
        ];
    }
}
