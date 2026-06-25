using System.Windows.Controls;
using WriterWorkbench.Core.Application;
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
}
