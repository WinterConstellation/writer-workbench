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
            AppCommandIds.StoryAddNode,
            AppCommandIds.StoryAddRelationship,
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
            AppCommandIds.ViewMainOpen,
            AppCommandIds.ViewPreviewToggle,
            AppCommandIds.SearchRun,
            AppCommandIds.AutosaveToggle,
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
        Assert.Equal("구조 노드 추가", registry.Get(AppCommandIds.StoryAddNode).Name);
        Assert.Equal("관계 추가", registry.Get(AppCommandIds.StoryAddRelationship).Name);
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
            slot => Assert.Equal(AppCommandIds.WorkspacePresetTwo, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WorkspacePresetThree, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.WorkspaceStartupPresetCycle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ShortcutsOpenSettings, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ViewMainOpen, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.ViewPreviewToggle, slot.CommandId),
            slot => Assert.Equal(AppCommandIds.HelpOpen, slot.CommandId));
    }
}
