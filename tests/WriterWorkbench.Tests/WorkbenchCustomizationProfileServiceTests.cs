using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class WorkbenchCustomizationProfileServiceTests
{
    [Fact]
    public async Task MissingProfileFileCreatesDefaultProfileUnderProjectSettings()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var service = new WorkbenchCustomizationProfileService(
            paths.WorkbenchProfilesPath,
            AppCommandCatalog.CreateDefaultRegistry());

        var profile = await service.LoadOrCreateActiveProfileAsync(CancellationToken.None);

        Assert.Equal("profile-default", profile.Id);
        Assert.Contains(profile.Placements, placement => placement.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(profile.Shortcuts, shortcut => shortcut.CommandId == AppCommandIds.ProjectSave && shortcut.Gesture == "Ctrl+S");
        Assert.True(File.Exists(paths.WorkbenchProfilesPath));
        Assert.Contains("기본 작업대", await File.ReadAllTextAsync(paths.WorkbenchProfilesPath, CancellationToken.None));
    }

    [Fact]
    public async Task ExistingUserProfileIsLoadedWithoutBeingReplacedByDefault()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var store = new WorkbenchCustomizationProfileStore(paths.WorkbenchProfilesPath, registry);
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        var userProfile = new WorkbenchCustomizationProfile(
            "profile-user",
            "내 작업대",
            [
                new CommandPlacement("toolbar", "main", "save", AppCommandIds.ProjectSave, "저장", 1, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        await store.SaveProfileAsync(userProfile, CancellationToken.None);
        var before = await File.ReadAllTextAsync(paths.WorkbenchProfilesPath, CancellationToken.None);
        var service = new WorkbenchCustomizationProfileService(paths.WorkbenchProfilesPath, registry);

        var loaded = await service.LoadOrCreateActiveProfileAsync(CancellationToken.None);
        var after = await File.ReadAllTextAsync(paths.WorkbenchProfilesPath, CancellationToken.None);

        Assert.Equal("profile-user", loaded.Id);
        Assert.Equal(before, after);
        Assert.DoesNotContain("profile-default", after);
    }

    [Fact]
    public async Task RequestedProfileIdWinsWhenMultipleProfilesExist()
    {
        var root = NewRoot();
        var paths = ProjectPaths.ForRoot(root);
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var store = new WorkbenchCustomizationProfileStore(paths.WorkbenchProfilesPath, registry);
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        await store.SaveProfileAsync(new WorkbenchCustomizationProfile(
            "profile-a",
            "A 작업대",
            [new CommandPlacement("toolbar", "main", "a", AppCommandIds.ProjectSave, "저장", 1, new Dictionary<string, string>())],
            [],
            [],
            now,
            now), CancellationToken.None);
        await store.SaveProfileAsync(new WorkbenchCustomizationProfile(
            "profile-b",
            "B 작업대",
            [new CommandPlacement("toolbar", "main", "b", AppCommandIds.ViewMainOpen, "메인", 1, new Dictionary<string, string>())],
            [],
            [],
            now,
            now), CancellationToken.None);
        var service = new WorkbenchCustomizationProfileService(paths.WorkbenchProfilesPath, registry);

        var loaded = await service.LoadOrCreateActiveProfileAsync(CancellationToken.None, "profile-b");

        Assert.Equal("profile-b", loaded.Id);
        Assert.Contains(loaded.Placements, placement => placement.CommandId == AppCommandIds.ViewMainOpen);
    }

    private static string NewRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
