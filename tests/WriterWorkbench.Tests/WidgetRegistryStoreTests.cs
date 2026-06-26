using WriterWorkbench.Core.AppSettings;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class WidgetRegistryStoreTests
{
    [Fact]
    public async Task MissingWidgetRegistryCreatesDefaultInstancesFromPlacements()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new WidgetRegistryStore(paths.WidgetRegistryPath);
        var defaults = new[]
        {
            new CommandPlacement("menu", "top.project", "save", AppCommandIds.ProjectSave, "저장", 10, new Dictionary<string, string>()),
            new CommandPlacement("remote", "floating", "snapshot", AppCommandIds.SnapshotCreateCurrent, "스냅샷", 20, new Dictionary<string, string>())
        };

        var registry = await store.LoadOrCreateAsync(defaults, CancellationToken.None);

        Assert.Equal("widget-registry", registry.Schema);
        Assert.Equal(2, registry.Instances.Count);
        Assert.Contains(registry.Instances, item => item.Surface == "menu" && item.Area == "top.project");
        Assert.Contains(registry.Instances, item => item.Surface == "remote" && item.Area == "floating");
        Assert.Contains("스냅샷", await File.ReadAllTextAsync(paths.WidgetRegistryPath, CancellationToken.None));
    }

    [Fact]
    public async Task ExistingWidgetRegistryLoadsWithoutReplacingUserOrder()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new WidgetRegistryStore(paths.WidgetRegistryPath);
        var userRegistry = new WorkbenchWidgetRegistry(
            "widget-registry",
            [
                new WidgetInstance(
                    "widget-user-save",
                    "command-button",
                    "remote",
                    "floating",
                    "user-save",
                    99,
                    AppCommandIds.ProjectSave,
                    "내 저장",
                    new Dictionary<string, string>())
            ]);
        await store.SaveAsync(userRegistry, CancellationToken.None);
        var before = await File.ReadAllTextAsync(paths.WidgetRegistryPath, CancellationToken.None);

        var loaded = await store.LoadOrCreateAsync(
            [new CommandPlacement("menu", "top.project", "ignored", AppCommandIds.HelpOpen, "무시", 1, new Dictionary<string, string>())],
            CancellationToken.None);
        var after = await File.ReadAllTextAsync(paths.WidgetRegistryPath, CancellationToken.None);

        var instance = Assert.Single(loaded.Instances);
        Assert.Equal("user-save", instance.SlotKey);
        Assert.Equal(99, instance.Order);
        Assert.Equal(before, after);
    }

    private static string NewRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
