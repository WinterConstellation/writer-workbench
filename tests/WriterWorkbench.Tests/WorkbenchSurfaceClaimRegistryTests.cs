using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Workspace;

namespace WriterWorkbench.Tests;

public sealed class WorkbenchSurfaceClaimRegistryTests
{
    [Fact]
    public void MainSurfaceClaimBlocksDetachedDuplicateUntilMainMovesAway()
    {
        var registry = new WorkbenchSurfaceClaimRegistry();

        Assert.True(registry.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface, out _));
        Assert.False(registry.TryClaim("detached-1", AppSessionState.EditorSurface, out var occupiedOwner));
        Assert.Equal(WorkbenchSurfaceClaimRegistry.MainOwnerId, occupiedOwner);

        Assert.True(registry.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.HtmlWorkbenchSurface, out _));
        Assert.True(registry.TryClaim("detached-1", AppSessionState.EditorSurface, out _));

        Assert.True(registry.IsClaimedBy("detached-1", AppSessionState.EditorSurface));
        Assert.False(registry.IsClaimed(AppSessionState.PreviewSurface));
    }

    [Fact]
    public void OwnerCanMoveBetweenSurfacesWithoutLeavingDuplicateClaims()
    {
        var registry = new WorkbenchSurfaceClaimRegistry();

        Assert.True(registry.TryClaim("detached-1", AppSessionState.PreviewSurface, out _));
        Assert.True(registry.TryClaim("detached-1", AppSessionState.RelationshipMapSurface, out _));

        Assert.False(registry.IsClaimed(AppSessionState.PreviewSurface));
        Assert.True(registry.IsClaimedBy("detached-1", AppSessionState.RelationshipMapSurface));
    }

    [Fact]
    public void AvailabilityMarksSurfacesOccupiedByOtherOwners()
    {
        var registry = new WorkbenchSurfaceClaimRegistry();
        registry.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface, out _);
        registry.TryClaim("detached-2", AppSessionState.PreviewSurface, out _);

        var availability = registry.GetAvailability("detached-1");

        Assert.False(availability.Single(item => item.SurfaceId == AppSessionState.EditorSurface).IsAvailable);
        Assert.False(availability.Single(item => item.SurfaceId == AppSessionState.PreviewSurface).IsAvailable);
        Assert.True(availability.Single(item => item.SurfaceId == AppSessionState.HtmlWorkbenchSurface).IsAvailable);
        Assert.DoesNotContain(availability, item => item.SurfaceId == AppSessionState.MainSurface);
        Assert.Equal("메인", availability.Single(item => item.SurfaceId == AppSessionState.HtmlWorkbenchSurface).Name);
        Assert.Equal("작품 수정", availability.Single(item => item.SurfaceId == AppSessionState.EditorSurface).Name);
    }
}
