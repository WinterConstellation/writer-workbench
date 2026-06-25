namespace WriterWorkbench.Core.Commands;

public static class AppCommandCatalog
{
    public static CommandRegistry CreateDefaultRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new AppCommand(AppCommandIds.ProjectNew, "새 프로젝트", "프로젝트", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ProjectOpen, "프로젝트 열기", "프로젝트", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ExportCurrentScene, "현재 장면 내보내기", "내보내기", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ExportFullManuscript, "전체 원고 내보내기", "내보내기", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SnapshotCreateCurrent, "현재 장면 스냅샷", "스냅샷", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SnapshotRestoreSelected, "스냅샷 복원", "스냅샷", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SnapshotDeleteSelected, "스냅샷 삭제", "스냅샷", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryRelationshipMapOpen, "관계도 열기", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryAddNode, "캐릭터 추가", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryUpdateNode, "캐릭터 수정", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryDeleteNode, "캐릭터 삭제", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryAddRelationship, "관계 추가", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryUpdateRelationship, "관계 수정", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.StoryDeleteRelationship, "관계 삭제", "구조", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SceneEntityLinkAdd, "장면 연결 추가", "장면", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SceneEntityLinkDelete, "장면 연결 삭제", "장면", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentCreateScene, "새 장면", "문서", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentCreateStressLarge, "스트레스 15k", "문서", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentDetachCurrent, "창 분리", "문서", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.DocumentRenameScene, "장면 이름 변경", "문서", CommandScope.Binder));
        registry.Register(new AppCommand(AppCommandIds.DocumentDuplicateScene, "장면 복제", "문서", CommandScope.Binder));
        registry.Register(new AppCommand(AppCommandIds.DocumentDeleteScene, "장면 삭제", "문서", CommandScope.Binder));
        registry.Register(new AppCommand(AppCommandIds.DocumentMoveSceneUp, "장면 위로", "문서", CommandScope.Binder));
        registry.Register(new AppCommand(AppCommandIds.DocumentMoveSceneDown, "장면 아래로", "문서", CommandScope.Binder));
        registry.Register(new AppCommand(AppCommandIds.ProjectSave, "저장", "프로젝트", CommandScope.Editor));
        registry.Register(new AppCommand(AppCommandIds.WritingFocusToggle, "집중", "집필", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspacePresetOne, "프리셋 1", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspacePresetTwo, "프리셋 2", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspacePresetThree, "프리셋 3", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.WorkspaceStartupPresetCycle, "시작 프리셋", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.RemoteControlShow, "리모콘 표시", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.RemoteControlOpenSettings, "리모콘 편집", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ShortcutsOpenSettings, "단축키", "작업공간", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ViewMainOpen, "메인", "보기", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.ViewPreviewToggle, "미리보기", "보기", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.SearchRun, "검색", "검색", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.AutosaveToggle, "자동저장", "프로젝트", CommandScope.Workbench));
        registry.Register(new AppCommand(AppCommandIds.HelpOpen, "도움말", "도움말", CommandScope.Workbench));
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
            new CommandSlot(10, AppCommandIds.StoryRelationshipMapOpen, "관계도"),
            new CommandSlot(11, AppCommandIds.WorkspacePresetTwo, "프리셋 2"),
            new CommandSlot(12, AppCommandIds.WorkspacePresetThree, "프리셋 3"),
            new CommandSlot(13, AppCommandIds.WorkspaceStartupPresetCycle, "시작 적용 안 함"),
            new CommandSlot(14, AppCommandIds.ShortcutsOpenSettings, "단축키"),
            new CommandSlot(15, AppCommandIds.ViewMainOpen, "메인"),
            new CommandSlot(16, AppCommandIds.ViewPreviewToggle, "미리보기"),
            new CommandSlot(17, AppCommandIds.HelpOpen, "도움말")
        ];
    }
}
