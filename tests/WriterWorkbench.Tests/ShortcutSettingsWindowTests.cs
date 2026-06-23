using System.Windows.Controls;
using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class ShortcutSettingsWindowTests
{
    [Fact]
    public void ShortcutSettingsWindowListsCommandsOnStaThread()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var manager = ShortcutProfileService.CreateDefaultManager();

                var window = new ShortcutSettingsWindow(registry, manager);

                Assert.True(window.RowCount >= 1);
                Assert.Contains(window.Rows, row => row.CommandId == AppCommandIds.ProjectSave && row.Gesture == "Ctrl+S");
                window.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void ShortcutSettingsWindowAppliesEditedRows()
    {
        var registry = new CommandRegistry();
        registry.Register(new AppCommand(AppCommandIds.ProjectSave, "Save", "Project", CommandScope.Editor));
        var manager = new ShortcutManager();

        var row = new ShortcutSettingsRow(AppCommandIds.ProjectSave, "Save", "Project", CommandScope.Editor, "Ctrl+Shift+S");
        var updated = ShortcutSettingsWindow.CreateManagerFromRows([row], out var conflict);

        Assert.Null(conflict);
        Assert.Equal(AppCommandIds.ProjectSave, updated.FindCommand("Ctrl+Shift+S", CommandScope.Editor));
    }
}
