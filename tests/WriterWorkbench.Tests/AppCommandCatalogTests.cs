using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class AppCommandCatalogTests
{
    [Fact]
    public void CreatesDefaultCommandRegistry()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();

        var commandIds = new[]
        {
            AppCommandIds.ProjectNew,
            AppCommandIds.ProjectOpen,
            AppCommandIds.ProjectSave,
            AppCommandIds.ExportCurrentScene,
            AppCommandIds.ExportFullManuscript,
            AppCommandIds.SnapshotCreateCurrent,
            AppCommandIds.SnapshotRestoreSelected,
            AppCommandIds.SnapshotDeleteSelected,
            AppCommandIds.StoryRelationshipMapOpen,
            AppCommandIds.StoryAddNode,
            AppCommandIds.StoryUpdateNode,
            AppCommandIds.StoryDeleteNode,
            AppCommandIds.StoryAddRelationship,
            AppCommandIds.StoryUpdateRelationship,
            AppCommandIds.StoryDeleteRelationship,
            AppCommandIds.SceneEntityLinkAdd,
            AppCommandIds.SceneEntityLinkDelete,
            AppCommandIds.DocumentCreateScene,
            AppCommandIds.DocumentCreateStressLarge,
            AppCommandIds.DocumentDetachCurrent,
            AppCommandIds.DocumentRenameScene,
            AppCommandIds.DocumentDuplicateScene,
            AppCommandIds.DocumentDeleteScene,
            AppCommandIds.DocumentMoveSceneUp,
            AppCommandIds.DocumentMoveSceneDown,
            AppCommandIds.WritingFocusToggle,
            AppCommandIds.WorkspacePresetOne,
            AppCommandIds.WorkspacePresetTwo,
            AppCommandIds.WorkspacePresetThree,
            AppCommandIds.WorkspaceStartupPresetCycle,
            AppCommandIds.ShortcutsOpenSettings,
            AppCommandIds.ViewEditorOpen,
            AppCommandIds.ViewMainOpen,
            AppCommandIds.ViewPreviewToggle,
            AppCommandIds.ViewFullscreenToggle,
            AppCommandIds.SearchRun,
            AppCommandIds.AutosaveToggle,
            AppCommandIds.CodexOpen,
            AppCommandIds.HelpOpen
        };

        foreach (var commandId in commandIds)
        {
            var command = registry.Get(commandId);
            Assert.Equal(commandId, command.Id);
            Assert.False(string.IsNullOrWhiteSpace(command.Name));
        }

        Assert.Equal("장면 이름 변경", registry.Get(AppCommandIds.DocumentRenameScene).Name);
        Assert.Equal("장면 복제", registry.Get(AppCommandIds.DocumentDuplicateScene).Name);
        Assert.Equal("장면 삭제", registry.Get(AppCommandIds.DocumentDeleteScene).Name);
        Assert.Equal("장면 위로", registry.Get(AppCommandIds.DocumentMoveSceneUp).Name);
        Assert.Equal("장면 아래로", registry.Get(AppCommandIds.DocumentMoveSceneDown).Name);
        Assert.Equal("현재 장면 내보내기", registry.Get(AppCommandIds.ExportCurrentScene).Name);
        Assert.Equal("전체 원고 내보내기", registry.Get(AppCommandIds.ExportFullManuscript).Name);
        Assert.Equal("현재 장면 스냅샷", registry.Get(AppCommandIds.SnapshotCreateCurrent).Name);
        Assert.Equal("스냅샷 복원", registry.Get(AppCommandIds.SnapshotRestoreSelected).Name);
        Assert.Equal("스냅샷 삭제", registry.Get(AppCommandIds.SnapshotDeleteSelected).Name);
        Assert.Equal("관계도 열기", registry.Get(AppCommandIds.StoryRelationshipMapOpen).Name);
        Assert.Equal("캐릭터 추가", registry.Get(AppCommandIds.StoryAddNode).Name);
        Assert.Equal("캐릭터 수정", registry.Get(AppCommandIds.StoryUpdateNode).Name);
        Assert.Equal("캐릭터 삭제", registry.Get(AppCommandIds.StoryDeleteNode).Name);
        Assert.Equal("관계 추가", registry.Get(AppCommandIds.StoryAddRelationship).Name);
        Assert.Equal("관계 수정", registry.Get(AppCommandIds.StoryUpdateRelationship).Name);
        Assert.Equal("관계 삭제", registry.Get(AppCommandIds.StoryDeleteRelationship).Name);
        Assert.Equal("작품 수정", registry.Get(AppCommandIds.ViewEditorOpen).Name);
        Assert.Equal("전체화면", registry.Get(AppCommandIds.ViewFullscreenToggle).Name);
        Assert.Contains(
            registry.DefaultShortcuts,
            shortcut => shortcut.CommandId == AppCommandIds.ViewFullscreenToggle &&
                        shortcut.Gesture == "F11" &&
                        shortcut.Scope == CommandScope.Workbench);
        Assert.Throws<KeyNotFoundException>(() => registry.Get(AppCommandIds.ViewHtmlWorkbenchOpen));
    }

    [Fact]
    public void CreatesDefaultCommandSlots()
    {
        var slots = AppCommandCatalog.CreateDefaultCommandSlots();

        Assert.Collection(
            slots,
            slot => Assert.Equal(AppCommandIds.ProjectNew, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ProjectOpen, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.DocumentCreateScene, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ProjectSave, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WritingFocusToggle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WorkspacePresetOne, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.AutosaveToggle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.DocumentCreateStressLarge, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.DocumentDetachCurrent, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.StoryRelationshipMapOpen, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WorkspacePresetTwo, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WorkspacePresetThree, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WorkspaceStartupPresetCycle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ShortcutsOpenSettings, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ViewMainOpen, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ViewEditorOpen, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ViewPreviewToggle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ViewFullscreenToggle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.HelpOpen, slot.CommandId));

        Assert.DoesNotContain(slots, slot => slot.CommandId == AppCommandIds.ViewHtmlWorkbenchOpen);
    }
}
