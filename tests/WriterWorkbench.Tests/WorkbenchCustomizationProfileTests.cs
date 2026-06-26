using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class WorkbenchCustomizationProfileTests
{
    [Fact]
    public async Task ProfileCanAssignCommandsToArbitrarySurfacesAndRoundtripKoreanText()
    {
        var registry = CreateRegistry();
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"), "settings", "workbench-profiles.json");
        var store = new WorkbenchCustomizationProfileStore(path, registry);
        var createdAt = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-author-desk",
            "작가 작업대",
            [
                new CommandPlacement("toolbar", "main", "primary.save", "project.save", "저장", 10, new Dictionary<string, string>()),
                new CommandPlacement("panel", "right.inspector", "focus.start", "writing.focus.toggle", "집중 시작", 20, new Dictionary<string, string>
                {
                    ["durationMinutes"] = "40",
                    ["exitConfirmChars"] = "20"
                }),
                new CommandPlacement("contextMenu", "binder.scene", "duplicate", "document.duplicateScene", "장면 복제", 30, new Dictionary<string, string>())
            ],
            [
                new ShortcutAssignment("Editor", "Ctrl+S", "project.save"),
                new ShortcutAssignment("Workbench", "Ctrl+Alt+1", "writing.focus.toggle")
            ],
            [
                new MacroDefinition(
                    "macro.start-writing",
                    "집필 시작",
                    [
                        new CommandInvocation("project.save", new Dictionary<string, string>()),
                        new CommandInvocation("writing.focus.toggle", new Dictionary<string, string>
                        {
                            ["durationMinutes"] = "40"
                        })
                    ])
            ],
            createdAt,
            createdAt);

        await store.SaveProfileAsync(profile, CancellationToken.None);

        var loaded = Assert.Single(await store.LoadProfilesAsync(CancellationToken.None));
        Assert.Equal("작가 작업대", loaded.Name);
        Assert.Equal("집중 시작", loaded.Placements.Single(placement => placement.SlotKey == "focus.start").Label);
        Assert.Equal("20", loaded.Placements.Single(placement => placement.SlotKey == "focus.start").Parameters["exitConfirmChars"]);
        Assert.Equal("Ctrl+Alt+1", loaded.Shortcuts.Single(shortcut => shortcut.CommandId == "writing.focus.toggle").Gesture);
        Assert.Equal("집필 시작", Assert.Single(loaded.Macros).Name);
        Assert.Contains("작가 작업대", await File.ReadAllTextAsync(path, CancellationToken.None));
    }

    [Fact]
    public async Task ProfileSupportsManyCustomSlotsWithoutMvpSlotLimit()
    {
        var registry = CreateRegistry();
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"), "settings", "workbench-profiles.json");
        var store = new WorkbenchCustomizationProfileStore(path, registry);
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        var placements = Enumerable.Range(1, 40)
            .Select(index => new CommandPlacement("toolbar", "main", $"slot-{index:00}", "project.save", $"저장 {index}", index, new Dictionary<string, string>()))
            .ToList();
        var profile = new WorkbenchCustomizationProfile("profile-many-slots", "40 슬롯", placements, [], [], now, now);

        await store.SaveProfileAsync(profile, CancellationToken.None);

        var loaded = Assert.Single(await store.LoadProfilesAsync(CancellationToken.None));
        Assert.Equal(40, loaded.Placements.Count);
        Assert.Contains(loaded.Placements, placement => placement.SlotKey == "slot-40");
    }

    [Fact]
    public async Task ValidationRejectsUnknownCommandsDuplicateSlotsAndShortcutConflicts()
    {
        var registry = CreateRegistry();
        var path = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"), "settings", "workbench-profiles.json");
        var store = new WorkbenchCustomizationProfileStore(path, registry);
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");

        var unknown = new WorkbenchCustomizationProfile(
            "profile-unknown",
            "알 수 없는 명령",
            [new CommandPlacement("toolbar", "main", "slot", "missing.command", "없음", 1, new Dictionary<string, string>())],
            [],
            [],
            now,
            now);
        var unknownError = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProfileAsync(unknown, CancellationToken.None));
        Assert.Contains("missing.command", unknownError.Message);

        var duplicateSlot = new WorkbenchCustomizationProfile(
            "profile-duplicate-slot",
            "중복 슬롯",
            [
                new CommandPlacement("toolbar", "main", "slot", "project.save", "저장", 1, new Dictionary<string, string>()),
                new CommandPlacement("toolbar", "main", "slot", "writing.focus.toggle", "집중", 2, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var duplicateSlotError = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProfileAsync(duplicateSlot, CancellationToken.None));
        Assert.Contains("toolbar/main/slot", duplicateSlotError.Message);

        var shortcutConflict = new WorkbenchCustomizationProfile(
            "profile-shortcut-conflict",
            "단축키 충돌",
            [],
            [
                new ShortcutAssignment("Workbench", "Ctrl+Alt+1", "project.save"),
                new ShortcutAssignment("Workbench", "Ctrl+Alt+1", "writing.focus.toggle")
            ],
            [],
            now,
            now);
        var shortcutError = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProfileAsync(shortcutConflict, CancellationToken.None));
        Assert.Contains("Workbench/Ctrl+Alt+1", shortcutError.Message);
    }

    [Fact]
    public void ResolverReturnsOrderedPlacementsForSurfaceAreaAndMacroSteps()
    {
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-resolver",
            "해석기",
            [
                new CommandPlacement("toolbar", "main", "third", "document.duplicateScene", "복제", 30, new Dictionary<string, string>()),
                new CommandPlacement("toolbar", "main", "first", "project.save", "저장", 10, new Dictionary<string, string>()),
                new CommandPlacement("panel", "right", "hidden", "writing.focus.toggle", "집중", 20, new Dictionary<string, string>())
            ],
            [],
            [
                new MacroDefinition(
                    "macro.start-writing",
                    "집필 시작",
                    [
                        new CommandInvocation("project.save", new Dictionary<string, string>()),
                        new CommandInvocation("writing.focus.toggle", new Dictionary<string, string>
                        {
                            ["durationMinutes"] = "40"
                        })
                    ])
            ],
            now,
            now);
        var resolver = new WorkbenchCustomizationResolver(profile);

        var toolbar = resolver.GetPlacements("toolbar", "main");
        var macro = resolver.GetMacroSteps("macro.start-writing");

        Assert.Equal(["first", "third"], toolbar.Select(placement => placement.SlotKey));
        Assert.DoesNotContain(toolbar, placement => placement.Area == "right");
        Assert.Equal(["project.save", "writing.focus.toggle"], macro.Select(step => step.CommandId));
        Assert.Equal("40", macro.Last().Parameters["durationMinutes"]);
    }

    [Fact]
    public void ProjectPathsExposeV2WorkbenchProfilePathUnderSettings()
    {
        var paths = ProjectPaths.ForRoot(@"C:\WriterWorkbench\Novel.writerproj");

        Assert.Equal(
            @"C:\WriterWorkbench\Novel.writerproj\settings\workbench-profiles.json",
            paths.WorkbenchProfilesPath);
    }

    [Fact]
    public void DefaultProfileFactoryConvertsCommandCatalogSlotsIntoEditableProfileData()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();

        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-default", "기본 작업대", registry);
        var toolbar = new WorkbenchCustomizationResolver(profile).GetPlacements("toolbar", "main");

        Assert.Equal("profile-default", profile.Id);
        Assert.Contains(toolbar, placement => placement.CommandId == AppCommandIds.ProjectSave && placement.SlotKey == "slot-04");
        Assert.Contains(toolbar, placement => placement.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains(profile.Shortcuts, shortcut => shortcut.CommandId == AppCommandIds.ProjectSave && shortcut.Gesture == "Ctrl+S");
        Assert.Contains(profile.Shortcuts, shortcut => shortcut.CommandId == AppCommandIds.ShortcutsOpenSettings && shortcut.Gesture == "Ctrl+Alt+K");
        Assert.Contains(profile.Macros, macro => macro.Id == "macro.start-focus-writing");
    }

    [Fact]
    public void DefaultProfileFactoryIncludesCustomizableRemoteControlPlacements()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();

        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-default", "기본 작업대", registry);
        var remote = new WorkbenchCustomizationResolver(profile).GetPlacements("remote", "main");

        Assert.NotEmpty(remote);
        Assert.Contains(remote, placement => placement.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(remote, placement => placement.CommandId == AppCommandIds.DocumentCreateScene);
        Assert.Contains(remote, placement => placement.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains(remote, placement => placement.CommandId == AppCommandIds.DocumentDetachCurrent);
        Assert.Equal(remote.Count, remote.Select(placement => placement.SlotKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void DefaultProfileFactoryIncludesTopMenuPlacementsForHtmlWorkbench()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();

        var profile = WorkbenchCustomizationProfileFactory.CreateDefault("profile-default", "기본 작업대", registry);
        var menu = profile.Placements
            .Where(placement => string.Equals(placement.Surface, "menu", StringComparison.OrdinalIgnoreCase))
            .OrderBy(placement => placement.Order)
            .ToList();

        Assert.Contains(menu, placement => placement.Area == "top.project" && placement.CommandId == AppCommandIds.ProjectSave);
        Assert.Contains(menu, placement => placement.Area == "top.manuscript" && placement.CommandId == AppCommandIds.DocumentCreateScene);
        Assert.Contains(menu, placement => placement.Area == "top.story" && placement.CommandId == AppCommandIds.StoryRelationshipMapOpen);
        Assert.Contains(menu, placement => placement.Area == "top.view" && placement.CommandId == AppCommandIds.ViewMainOpen);
        Assert.Contains(menu, placement => placement.Area == "top.tools" && placement.CommandId == AppCommandIds.ShortcutsOpenSettings);
        Assert.Contains(menu, placement => placement.Area == "top.help" && placement.CommandId == AppCommandIds.HelpOpen);
    }

    private static CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new AppCommand("project.save", "저장", "프로젝트", CommandScope.Editor));
        registry.Register(new AppCommand("writing.focus.toggle", "집중", "집필", CommandScope.Workbench));
        registry.Register(new AppCommand("document.duplicateScene", "장면 복제", "문서", CommandScope.Binder));
        return registry;
    }
}
