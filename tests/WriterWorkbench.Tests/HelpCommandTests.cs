using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class HelpCommandTests
{
    [Fact]
    public void HelpCommandIsRegisteredAndSlotted()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var slots = AppCommandCatalog.CreateDefaultCommandSlots();

        Assert.Equal("도움말", registry.Get(AppCommandIds.HelpOpen).Name);
        Assert.Contains(slots, slot => slot.CommandId == AppCommandIds.HelpOpen);
    }

    [Fact]
    public async Task F1OpensHelpByDefault()
    {
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchShortcutTests", Guid.NewGuid().ToString("N"), "shortcuts.json");
        var service = new ShortcutProfileService(path);

        var loaded = await service.LoadOrCreateDefaultAsync(CancellationToken.None);

        Assert.Equal(AppCommandIds.HelpOpen, loaded.FindCommand("F1", CommandScope.Workbench));
        Assert.Equal(AppCommandIds.HelpOpen, loaded.FindCommand("F1", CommandScope.Editor));
    }
}
