using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class CommandRegistryTests
{
    [Fact]
    public void RegistersAndReturnsCommandById()
    {
        var registry = new CommandRegistry();
        var command = new AppCommand("project.save", "Save", "Project", CommandScope.Editor);

        registry.Register(command);

        Assert.Equal(command, registry.Get("project.save"));
    }

    [Fact]
    public void ListsRegisteredCommandsById()
    {
        var registry = new CommandRegistry();
        registry.Register(new AppCommand("workspace.preset.2", "Preset 2", "Workspace", CommandScope.Workbench));
        registry.Register(new AppCommand("project.save", "Save", "Project", CommandScope.Editor));

        var commands = registry.All;

        Assert.Collection(
            commands,
            command => Assert.Equal("project.save", command.Id),
            command => Assert.Equal("workspace.preset.2", command.Id));
    }
}
