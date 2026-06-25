using System.Windows.Controls;
using System.Windows.Threading;
using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;
using WriterWorkbench.Core.Workspace;
using Xunit.Abstractions;

namespace WriterWorkbench.Tests;

public sealed class WorkbenchDetachedWindowTests
{
    private readonly ITestOutputHelper _output;

    public WorkbenchDetachedWindowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DetachedWorkbenchStartsBlankAndDisablesSurfaceUsedByMainWindow()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = new WorkbenchSurfaceClaimRegistry();
                registry.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface, out _);

                var window = new WorkbenchDetachedWindow(registry, "detached-test-1");
                var editorButton = Assert.IsType<Button>(window.FindName("DetachedEditorSurfaceButton"));
                var mainButton = Assert.IsType<Button>(window.FindName("DetachedMainSurfaceButton"));

                Assert.Null(window.AssignedSurfaceId);
                Assert.False(editorButton.IsEnabled);
                Assert.True(mainButton.IsEnabled);
                Assert.Contains("작업대를 선택", window.StatusDisplay);
                window.Close();
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.ToString());
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
    public void DetachedWorkbenchClaimsSelectedSurfaceAndBlocksDuplicateSelection()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = new WorkbenchSurfaceClaimRegistry();
                registry.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface, out _);

                var window = new WorkbenchDetachedWindow(registry, "detached-test-2");

                Assert.True(window.TrySelectSurface(AppSessionState.RelationshipMapSurface));
                Assert.Equal(AppSessionState.RelationshipMapSurface, window.AssignedSurfaceId);
                Assert.True(registry.IsClaimedBy("detached-test-2", AppSessionState.RelationshipMapSurface));
                Assert.Contains("관계도", window.AssignedSurfaceDisplay);

                Assert.False(window.TrySelectSurface(AppSessionState.EditorSurface));
                Assert.Equal(AppSessionState.RelationshipMapSurface, window.AssignedSurfaceId);
                Assert.Contains("이미 사용 중", window.StatusDisplay);

                window.Close();
                Assert.False(registry.IsClaimed(AppSessionState.RelationshipMapSurface));
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.ToString());
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
    public async Task DetachedRelationshipMapSurfaceLoadsStoryDataIntoCanvas()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var store = new StoryStructureStore(ProjectPaths.ForRoot(root));
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        await store.SaveEntitiesAsync(
            [
                new StoryEntity("entity-0001", StoryEntityType.Character, "한서", "주인공", "요약", "#2563EB", ["주연"], now, now),
                new StoryEntity("entity-0002", StoryEntityType.Character, "윤재", "라이벌", "요약", "#7C3AED", ["라이벌"], now, now)
            ],
            CancellationToken.None);
        await store.SaveRelationshipsAsync(
            [
                new StoryRelationship("rel-0001", "entity-0001", "entity-0002", "대립", "서로 견제", true, now, now)
            ],
            CancellationToken.None);
        await store.SaveRelationLayoutAsync(
            [
                new StoryMapNodeLayout("entity-0001", 32, 48),
                new StoryMapNodeLayout("entity-0002", 260, 160)
            ],
            CancellationToken.None);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = new WorkbenchSurfaceClaimRegistry();
                registry.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface, out _);
                var window = new WorkbenchDetachedWindow(registry, "detached-map-test", store);

                var selected = WaitForTaskOnDispatcher(window.SelectSurfaceAsync(AppSessionState.RelationshipMapSurface));

                Assert.True(selected);
                Assert.Equal(2, window.RelationshipMapEntityCount);
                Assert.Equal(1, window.RelationshipMapRelationshipCount);
                Assert.True(window.RelationshipMapCanvasElementCount >= 4);
                Assert.True(window.RelationshipMapVisible);
                Assert.Contains("한서", window.RelationshipMapSummary);
                window.Close();
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.ToString());
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

#pragma warning disable xUnit1031
    private static T WaitForTaskOnDispatcher<T>(Task<T> task)
    {
        var frame = new DispatcherFrame();
        task.ContinueWith(
            _ => frame.Continue = false,
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        return task.GetAwaiter().GetResult();
    }
#pragma warning restore xUnit1031
}
