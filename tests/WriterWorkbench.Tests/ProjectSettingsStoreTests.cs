using WriterWorkbench.Core.AppSettings;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class ProjectSettingsStoreTests
{
    [Fact]
    public void ProjectPathsExposeSeparatedSettingsFiles()
    {
        var paths = ProjectPaths.ForRoot(@"C:\WriterWorkbench\Sample.writerproj");

        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\app.json", paths.AppSettingsPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\editor-profiles.json", paths.EditorProfilesPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\workspace-options.json", paths.WorkspaceOptionsPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\widget-registry.json", paths.WidgetRegistryPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\command-assignments.json", paths.CommandAssignmentsPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\text-replacements.json", paths.TextReplacementsPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\export-profiles.json", paths.ExportProfilesPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\path-restorer.json", paths.PathRestorerPath);
        Assert.Equal(@"C:\WriterWorkbench\Sample.writerproj\settings\migration-state.json", paths.MigrationStatePath);
    }

    [Fact]
    public async Task MissingAppSettingsCreatesDefaultFileWithKoreanSafeJson()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectSettingsStore(paths.AppSettingsPath);

        var settings = await store.LoadOrCreateAsync(CancellationToken.None);

        Assert.Equal("app", settings.Schema);
        Assert.Equal("마지막 작업", settings.LastWorkspaceLabel);
        Assert.True(settings.AutosaveEnabled);
        Assert.True(File.Exists(paths.AppSettingsPath));
        Assert.Contains("마지막 작업", await File.ReadAllTextAsync(paths.AppSettingsPath, CancellationToken.None));
    }

    [Fact]
    public async Task ExistingAppSettingsRoundtripWithoutOverwritingUserValues()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectSettingsStore(paths.AppSettingsPath);
        var userSettings = new ProjectAppSettings(
            "app",
            "내 작업",
            false,
            "relationship-map",
            "scene-9999");
        await store.SaveAsync(userSettings, CancellationToken.None);
        var before = await File.ReadAllTextAsync(paths.AppSettingsPath, CancellationToken.None);

        var loaded = await store.LoadOrCreateAsync(CancellationToken.None);
        var after = await File.ReadAllTextAsync(paths.AppSettingsPath, CancellationToken.None);

        Assert.Equal(userSettings, loaded);
        Assert.Equal(before, after);
    }

    private static string NewRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
