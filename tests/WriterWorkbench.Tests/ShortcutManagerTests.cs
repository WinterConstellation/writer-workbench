using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class ShortcutManagerTests
{
    [Fact]
    public void DetectsConflictInsideSameScope()
    {
        var manager = new ShortcutManager();

        Assert.True(manager.TryBind(new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Editor), out _));
        Assert.False(manager.TryBind(new ShortcutBinding("document.split", "Ctrl+S", CommandScope.Editor), out var conflict));
        Assert.Equal("project.save", conflict);
    }

    [Fact]
    public void AllowsSameShortcutAcrossDifferentScopes()
    {
        var manager = new ShortcutManager();

        Assert.True(manager.TryBind(new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Editor), out _));
        Assert.True(manager.TryBind(new ShortcutBinding("binder.rename", "Ctrl+S", CommandScope.Binder), out _));
    }

    [Fact]
    public void FindsCommandByGestureAndScope()
    {
        var manager = new ShortcutManager();

        Assert.True(manager.TryBind(new ShortcutBinding("workspace.preset.2", "Ctrl+Alt+2", CommandScope.Workbench), out _));

        Assert.Equal("workspace.preset.2", manager.FindCommand("ctrl+alt+2", CommandScope.Workbench));
        Assert.Null(manager.FindCommand("ctrl+alt+2", CommandScope.Editor));
    }

    [Fact]
    public void FallsBackToGlobalShortcut()
    {
        var manager = new ShortcutManager();

        Assert.True(manager.TryBind(new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Global), out _));

        Assert.Equal("project.save", manager.FindCommand("Ctrl+S", CommandScope.Editor));
    }

    [Fact]
    public async Task PersistsShortcutBindingsToDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchShortcutTests", Guid.NewGuid().ToString("N"), "shortcuts.json");
        var service = new ShortcutProfileService(path);
        var manager = new ShortcutManager();
        Assert.True(manager.TryBind(new ShortcutBinding("workspace.preset.3", "Ctrl+Alt+3", CommandScope.Workbench), out _));

        await service.SaveAsync(manager, CancellationToken.None);
        var loaded = await service.LoadOrCreateDefaultAsync(CancellationToken.None);

        Assert.Equal("workspace.preset.3", loaded.FindCommand("Ctrl+Alt+3", CommandScope.Workbench));
    }

    [Fact]
    public async Task CreatesDefaultShortcutsWhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchShortcutTests", Guid.NewGuid().ToString("N"), "shortcuts.json");
        var service = new ShortcutProfileService(path);

        var loaded = await service.LoadOrCreateDefaultAsync(CancellationToken.None);

        Assert.Equal(AppCommandIds.ProjectSave, loaded.FindCommand("Ctrl+S", CommandScope.Editor));
        Assert.Equal(AppCommandIds.WorkspacePresetOne, loaded.FindCommand("Ctrl+Alt+1", CommandScope.Workbench));
        Assert.Equal(AppCommandIds.WorkspacePresetTwo, loaded.FindCommand("Ctrl+Alt+2", CommandScope.Workbench));
        Assert.Equal(AppCommandIds.WorkspacePresetThree, loaded.FindCommand("Ctrl+Alt+3", CommandScope.Workbench));
        Assert.Equal(AppCommandIds.WorkspaceStartupPresetCycle, loaded.FindCommand("Ctrl+Alt+0", CommandScope.Workbench));
        Assert.Equal(AppCommandIds.ShortcutsOpenSettings, loaded.FindCommand("Ctrl+Alt+K", CommandScope.Workbench));
    }
}
