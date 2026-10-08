using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class CommandContributionTests
{
    [Fact]
    public void RegistryStoresCommandPlacementsAndShortcutsFromContributions()
    {
        var registry = new CommandRegistry();
        registry.Register(new CommandContribution(
            new AppCommand("project.save", "저장", "프로젝트", CommandScope.Editor),
            [
                new CommandPlacementContribution(
                    "toolbar",
                    "main",
                    "slot-01",
                    "project.save",
                    "빠른 저장",
                    10,
                    new Dictionary<string, string>())
            ],
            [
                new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Global)
            ]));

        var placements = registry.GetPlacements("toolbar", "main");

        Assert.Single(placements);
        Assert.Equal("project.save", placements[0].CommandId);
        Assert.Equal("빠른 저장", placements[0].Label);
        Assert.Equal(AppCommandIds.ProjectSave, registry.Get("PROJECT.SAVE").Id);
        Assert.Equal(AppCommandIds.ProjectSave, Assert.Single(registry.DefaultShortcuts).CommandId);
    }

    [Fact]
    public void DefaultCatalogExposesWorkbenchContributionsForToolbarRemoteAndShortcuts()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();

        var toolbar = registry.GetPlacements("toolbar", "main");
        var remote = registry.GetPlacements("remote", "main");
        var shortcuts = registry.DefaultShortcuts;

        Assert.Contains(toolbar, placement => placement.CommandId == AppCommandIds.ProjectSave && placement.SlotKey == "slot-04");
        Assert.Contains(toolbar, placement => placement.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains(remote, placement => placement.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(remote, placement => placement.CommandId == AppCommandIds.SnapshotCreateCurrent);
        Assert.Contains(shortcuts, shortcut => shortcut.CommandId == AppCommandIds.ProjectSave && shortcut.Gesture == "Ctrl+S");
        Assert.Contains(shortcuts, shortcut => shortcut.CommandId == AppCommandIds.RemoteControlToggle && shortcut.Gesture == "Ctrl+Alt+R");
    }

    [Fact]
    public void RegistryRejectsDuplicateContributionSlotsAndShortcutConflicts()
    {
        var registry = new CommandRegistry();
        registry.Register(new CommandContribution(
            new AppCommand("project.save", "저장", "프로젝트", CommandScope.Editor),
            [
                new CommandPlacementContribution("toolbar", "main", "slot-01", "project.save", "저장", 10, new Dictionary<string, string>())
            ],
            [
                new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Global)
            ]));

        var duplicateSlot = Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new CommandContribution(
                new AppCommand("project.open", "열기", "프로젝트", CommandScope.Workbench),
                [
                    new CommandPlacementContribution("toolbar", "main", "slot-01", "project.open", "열기", 20, new Dictionary<string, string>())
                ],
                [])));
        Assert.Contains("toolbar/main/slot-01", duplicateSlot.Message);

        var shortcutConflict = Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new CommandContribution(
                new AppCommand("project.new", "새 프로젝트", "프로젝트", CommandScope.Workbench),
                [],
                [
                    new ShortcutBinding("project.new", "Ctrl+S", CommandScope.Global)
                ])));
        Assert.Contains("Ctrl+S", shortcutConflict.Message);
    }
}
