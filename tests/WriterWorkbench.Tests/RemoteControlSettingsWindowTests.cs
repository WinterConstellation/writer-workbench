using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;

namespace WriterWorkbench.Tests;

public sealed class RemoteControlSettingsWindowTests
{
    [Fact]
    public void ApplyRowsCreatesRemotePlacementsAndPreservesOtherPlacements()
    {
        var registry = AppCommandCatalog.CreateDefaultRegistry();
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        var profile = new WorkbenchCustomizationProfile(
            "profile-remote-edit",
            "리모콘 편집",
            [
                new CommandPlacement("toolbar", "main", "slot-01", AppCommandIds.ProjectSave, "저장", 1, new Dictionary<string, string>()),
                new CommandPlacement("remote", "main", "remote-old", AppCommandIds.HelpOpen, "도움말", 99, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
        var rows = new[]
        {
            new RemoteControlSettingsRow(AppCommandIds.DocumentDetachCurrent, "창 분리", "창", true, 20, "분리"),
            new RemoteControlSettingsRow(AppCommandIds.ProjectSave, "저장", "파일", true, 10, "빠른 저장"),
            new RemoteControlSettingsRow(AppCommandIds.HelpOpen, "도움말", "도움말", false, 30, "도움말")
        };

        var updated = RemoteControlSettingsWindow.ApplyRemoteRows(profile, rows, registry, now.AddMinutes(1));
        var remote = new WorkbenchCustomizationResolver(updated).GetPlacements("remote", "main");
        var toolbar = new WorkbenchCustomizationResolver(updated).GetPlacements("toolbar", "main");

        Assert.Single(toolbar);
        Assert.Equal(["project.save", "document.detachCurrent"], remote.Select(placement => placement.CommandId));
        Assert.Equal(["빠른 저장", "분리"], remote.Select(placement => placement.Label));
        Assert.Equal(["remote-01", "remote-02"], remote.Select(placement => placement.SlotKey));
        Assert.DoesNotContain(remote, placement => placement.CommandId == AppCommandIds.HelpOpen);
        Assert.True(updated.UpdatedAt > profile.UpdatedAt);
    }

    [Fact]
    public void WindowRowsReflectExistingRemotePlacements()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
                var profile = new WorkbenchCustomizationProfile(
                    "profile-remote-window",
                    "리모콘 창",
                    [
                        new CommandPlacement("remote", "main", "remote-01", AppCommandIds.ProjectSave, "빠른 저장", 1, new Dictionary<string, string>()),
                        new CommandPlacement("remote", "main", "remote-02", AppCommandIds.StoryRelationshipMapOpen, "관계도", 2, new Dictionary<string, string>())
                    ],
                    [],
                    [],
                    now,
                    now);

                var window = new RemoteControlSettingsWindow(registry, profile);
                var saveRow = window.Rows.Single(row => row.CommandId == AppCommandIds.ProjectSave);
                var helpRow = window.Rows.Single(row => row.CommandId == AppCommandIds.HelpOpen);

                Assert.True(saveRow.ShowInRemote);
                Assert.Equal(1, saveRow.Order);
                Assert.Equal("빠른 저장", saveRow.Label);
                Assert.False(helpRow.ShowInRemote);
                Assert.True(window.RowCount >= registry.All.Count);
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
}
