using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class AppCommandCatalogTests
{
    [Fact]
    public void CreatesDefaultCommandRegistry()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();

        Assert.Equal("새 프로젝트", registry.Get(AppCommandIds.ProjectNew).Name);
        Assert.Equal("프로젝트 열기", registry.Get(AppCommandIds.ProjectOpen).Name);
        Assert.Equal("저장", registry.Get(AppCommandIds.ProjectSave).Name);
        Assert.Equal("새 장면", registry.Get(AppCommandIds.DocumentCreateScene).Name);
        Assert.Equal("집중", registry.Get(AppCommandIds.WritingFocusToggle).Name);
        Assert.Equal("프리셋 1", registry.Get(AppCommandIds.WorkspacePresetOne).Name);
        Assert.Equal("검색", registry.Get(AppCommandIds.SearchRun).Name);
        Assert.Equal("자동저장", registry.Get(AppCommandIds.AutosaveToggle).Name);
        Assert.Equal("스트레스 15k", registry.Get(AppCommandIds.DocumentCreateStressLarge).Name);
        Assert.Equal("창 분리", registry.Get(AppCommandIds.DocumentDetachCurrent).Name);
        Assert.Equal("프리셋 2", registry.Get(AppCommandIds.WorkspacePresetTwo).Name);
        Assert.Equal("프리셋 3", registry.Get(AppCommandIds.WorkspacePresetThree).Name);
        Assert.Equal("시작 프리셋", registry.Get(AppCommandIds.WorkspaceStartupPresetCycle).Name);
        Assert.Equal("단축키", registry.Get(AppCommandIds.ShortcutsOpenSettings).Name);
        Assert.Equal("메인", registry.Get(AppCommandIds.ViewMainOpen).Name);
        Assert.Equal("미리보기", registry.Get(AppCommandIds.ViewPreviewToggle).Name);
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
            slot => Assert.Equal(AppCommandIds.ViewPreviewToggle, slot.CommandId));
    }
}
