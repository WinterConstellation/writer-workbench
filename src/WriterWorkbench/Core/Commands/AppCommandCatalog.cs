namespace WriterWorkbench.Core.Commands;

public static class AppCommandCatalog
{
    public static CommandRegistry CreateDefaultRegistry()
    {
        var registry = new CommandRegistry();
        var toolbarSlots = CreateDefaultCommandSlots();
        var remoteSlots = CreateDefaultRemoteControlSlots();
        var shortcuts = CreateDefaultShortcutBindings();

        Register(registry, AppCommandIds.ProjectNew, "새 프로젝트", "프로젝트", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ProjectOpen, "프로젝트 열기", "프로젝트", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ExportCurrentScene, "현재 장면 내보내기", "내보내기", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ExportFullManuscript, "전체 원고 내보내기", "내보내기", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.SnapshotCreateCurrent, "현재 장면 스냅샷", "스냅샷", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.SnapshotRestoreSelected, "스냅샷 복원", "스냅샷", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.SnapshotDeleteSelected, "스냅샷 삭제", "스냅샷", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryRelationshipMapOpen, "관계도 열기", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryAddNode, "캐릭터 추가", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryUpdateNode, "캐릭터 수정", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryDeleteNode, "캐릭터 삭제", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryAddRelationship, "관계 추가", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryUpdateRelationship, "관계 수정", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.StoryDeleteRelationship, "관계 삭제", "구조", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.SceneEntityLinkAdd, "장면 연결 추가", "장면", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.SceneEntityLinkDelete, "장면 연결 삭제", "장면", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentCreateScene, "새 장면", "문서", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentCreateStressLarge, "스트레스 15k", "문서", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentDetachCurrent, "창 분리", "문서", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentRenameScene, "장면 이름 변경", "문서", CommandScope.Binder, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentDuplicateScene, "장면 복제", "문서", CommandScope.Binder, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentDeleteScene, "장면 삭제", "문서", CommandScope.Binder, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentMoveSceneUp, "장면 위로", "문서", CommandScope.Binder, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.DocumentMoveSceneDown, "장면 아래로", "문서", CommandScope.Binder, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ProjectSave, "저장", "프로젝트", CommandScope.Editor, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.WritingFocusToggle, "집중", "집필", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.WorkspacePresetOne, "프리셋 1", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.WorkspacePresetTwo, "프리셋 2", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.WorkspacePresetThree, "프리셋 3", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.WorkspaceStartupPresetCycle, "시작 프리셋", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.RemoteControlShow, "리모콘 표시", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.RemoteControlToggle, "리모콘 켜기/끄기", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.RemoteControlOpenSettings, "리모콘 편집", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ShortcutsOpenSettings, "단축키", "작업공간", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ViewHtmlWorkbenchOpen, "HTML 작업대", "보기", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ViewEditorOpen, "작품 수정", "보기", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ViewMainOpen, "메인", "보기", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.ViewPreviewToggle, "미리보기", "보기", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.SearchRun, "검색", "검색", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.AutosaveToggle, "자동저장", "프로젝트", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
        Register(registry, AppCommandIds.HelpOpen, "도움말", "도움말", CommandScope.Workbench, toolbarSlots, remoteSlots, shortcuts);
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
            new CommandSlot(15, AppCommandIds.ViewHtmlWorkbenchOpen, "HTML 작업대"),
            new CommandSlot(16, AppCommandIds.ViewMainOpen, "메인"),
            new CommandSlot(17, AppCommandIds.ViewPreviewToggle, "미리보기"),
            new CommandSlot(18, AppCommandIds.HelpOpen, "도움말")
        ];
    }

    public static IReadOnlyList<ShortcutBinding> CreateDefaultShortcutBindings()
    {
        return
        [
            new ShortcutBinding(AppCommandIds.ProjectSave, "Ctrl+S", CommandScope.Global),
            new ShortcutBinding(AppCommandIds.AutosaveToggle, "Ctrl+Alt+S", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.WorkspacePresetOne, "Ctrl+Alt+1", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.WorkspacePresetTwo, "Ctrl+Alt+2", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.WorkspacePresetThree, "Ctrl+Alt+3", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.WorkspaceStartupPresetCycle, "Ctrl+Alt+0", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.ShortcutsOpenSettings, "Ctrl+Alt+K", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.ViewPreviewToggle, "Ctrl+Alt+P", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.RemoteControlToggle, "Ctrl+Alt+R", CommandScope.Workbench),
            new ShortcutBinding(AppCommandIds.HelpOpen, "F1", CommandScope.Global)
        ];
    }

    private static IReadOnlyList<CommandSlot> CreateDefaultRemoteControlSlots()
    {
        return
        [
            new CommandSlot(1, AppCommandIds.ProjectSave, "저장"),
            new CommandSlot(2, AppCommandIds.DocumentCreateScene, "새 장면"),
            new CommandSlot(3, AppCommandIds.StoryRelationshipMapOpen, "관계도"),
            new CommandSlot(4, AppCommandIds.ExportFullManuscript, "전체 TXT"),
            new CommandSlot(5, AppCommandIds.DocumentDetachCurrent, "창 분리"),
            new CommandSlot(6, AppCommandIds.SnapshotCreateCurrent, "스냅샷")
        ];
    }

    private static void Register(
        CommandRegistry registry,
        string id,
        string name,
        string category,
        CommandScope scope,
        IReadOnlyList<CommandSlot> toolbarSlots,
        IReadOnlyList<CommandSlot> remoteSlots,
        IReadOnlyList<ShortcutBinding> shortcuts)
    {
        var placements = toolbarSlots
            .Where(slot => string.Equals(slot.CommandId, id, StringComparison.OrdinalIgnoreCase))
            .Select(slot => new CommandPlacementContribution(
                "toolbar",
                "main",
                $"slot-{slot.Slot:00}",
                id,
                slot.Label,
                slot.Slot,
                new Dictionary<string, string>()))
            .Concat(remoteSlots
                .Where(slot => string.Equals(slot.CommandId, id, StringComparison.OrdinalIgnoreCase))
                .Select(slot => new CommandPlacementContribution(
                    "remote",
                    "main",
                    $"remote-{slot.Slot:00}",
                    id,
                    slot.Label,
                    slot.Slot,
                    new Dictionary<string, string>())))
            .ToList();

        registry.Register(new CommandContribution(
            new AppCommand(id, name, category, scope),
            placements,
            shortcuts
                .Where(shortcut => string.Equals(shortcut.CommandId, id, StringComparison.OrdinalIgnoreCase))
                .ToList()));
    }
}
