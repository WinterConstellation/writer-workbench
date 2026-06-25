using System.Windows;
using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Story;
using WriterWorkbench.Core.Workspace;

namespace WriterWorkbench;

public partial class WorkbenchDetachedWindow : Window
{
    private readonly WorkbenchSurfaceClaimRegistry _surfaceClaims;
    private readonly string _ownerId;
    private readonly StoryStructureStore? _storyStructureStore;
    private WorkbenchRelationshipMapView? _relationshipMapView;

    public WorkbenchDetachedWindow(WorkbenchSurfaceClaimRegistry surfaceClaims)
        : this(surfaceClaims, $"detached-{Guid.NewGuid():N}", null)
    {
    }

    public WorkbenchDetachedWindow(WorkbenchSurfaceClaimRegistry surfaceClaims, string ownerId)
        : this(surfaceClaims, ownerId, null)
    {
    }

    public WorkbenchDetachedWindow(
        WorkbenchSurfaceClaimRegistry surfaceClaims,
        string ownerId,
        StoryStructureStore? storyStructureStore)
    {
        _surfaceClaims = surfaceClaims;
        _ownerId = ownerId;
        _storyStructureStore = storyStructureStore;
        InitializeComponent();
        RefreshAvailability();
        DetachedWorkbenchStatusText.Text = "작업대를 선택하세요.";
    }

    public string? AssignedSurfaceId { get; private set; }

    public string StatusDisplay => DetachedWorkbenchStatusText.Text;

    public string AssignedSurfaceDisplay => DetachedAssignedSurfaceText.Text;

    public int RelationshipMapEntityCount => _relationshipMapView?.EntityCount ?? 0;

    public int RelationshipMapRelationshipCount => _relationshipMapView?.RelationshipCount ?? 0;

    public int RelationshipMapCanvasElementCount => _relationshipMapView?.CanvasElementCount ?? 0;

    public bool RelationshipMapVisible => DetachedRelationshipMapHost.Visibility == Visibility.Visible;

    public string RelationshipMapSummary => _relationshipMapView?.SummaryText ?? "";

    public bool TrySelectSurface(string surfaceId)
    {
        return SelectSurfaceAsync(surfaceId).GetAwaiter().GetResult();
    }

    public async Task<bool> SelectSurfaceAsync(string surfaceId)
    {
        if (!_surfaceClaims.TryClaim(_ownerId, surfaceId, out var occupiedBy))
        {
            var name = WorkbenchSurfaceCatalog.GetName(surfaceId);
            DetachedWorkbenchStatusText.Text = $"{name} 화면은 이미 사용 중입니다. ({occupiedBy})";
            RefreshAvailability();
            return false;
        }

        AssignedSurfaceId = surfaceId;
        var surfaceName = WorkbenchSurfaceCatalog.GetName(surfaceId);
        Title = $"분리 작업대 - {surfaceName}";
        DetachedAssignedSurfaceText.Text = surfaceName;
        DetachedAssignedSurfaceHintText.Text = $"{surfaceName} 화면으로 지정되었습니다. 같은 화면은 다른 창에서 동시에 열 수 없습니다.";
        DetachedWorkbenchStatusText.Text = $"{surfaceName} 작업대 선택됨";
        await ShowSelectedSurfaceAsync(surfaceId);
        await RefreshAvailabilityAsync();
        return true;
    }

    private async void SurfaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string surfaceId })
        {
            await SelectSurfaceAsync(surfaceId);
        }
    }

    private async Task ShowSelectedSurfaceAsync(string surfaceId)
    {
        DetachedBlankSurface.Visibility = Visibility.Visible;
        DetachedRelationshipMapHost.Visibility = Visibility.Collapsed;

        if (!string.Equals(surfaceId, AppSessionState.RelationshipMapSurface, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_storyStructureStore is null)
        {
            DetachedAssignedSurfaceHintText.Text = "관계도 저장소가 연결되지 않았습니다.";
            return;
        }

        _relationshipMapView ??= new WorkbenchRelationshipMapView();
        DetachedRelationshipMapHost.Content = _relationshipMapView;
        DetachedBlankSurface.Visibility = Visibility.Collapsed;
        DetachedRelationshipMapHost.Visibility = Visibility.Visible;
        await _relationshipMapView.LoadAsync(_storyStructureStore, CancellationToken.None);
        await SetStatusAsync("관계도 로드됨");
    }

    private Task RefreshAvailabilityAsync()
    {
        if (Dispatcher.CheckAccess())
        {
            RefreshAvailability();
            return Task.CompletedTask;
        }

        return Dispatcher.InvokeAsync(RefreshAvailability).Task;
    }

    private Task SetStatusAsync(string status)
    {
        if (Dispatcher.CheckAccess())
        {
            DetachedWorkbenchStatusText.Text = status;
            return Task.CompletedTask;
        }

        return Dispatcher.InvokeAsync(() => DetachedWorkbenchStatusText.Text = status).Task;
    }

    private void RefreshAvailability()
    {
        var availability = _surfaceClaims.GetAvailability(_ownerId)
            .ToDictionary(item => item.SurfaceId, StringComparer.OrdinalIgnoreCase);
        ApplyAvailability(DetachedEditorSurfaceButton, availability);
        ApplyAvailability(DetachedMainSurfaceButton, availability);
        ApplyAvailability(DetachedPreviewSurfaceButton, availability);
        ApplyAvailability(DetachedRelationshipMapSurfaceButton, availability);
    }

    private static void ApplyAvailability(
        System.Windows.Controls.Button button,
        IReadOnlyDictionary<string, WorkbenchSurfaceAvailability> availability)
    {
        if (button.Tag is not string surfaceId ||
            !availability.TryGetValue(surfaceId, out var item))
        {
            button.IsEnabled = false;
            button.ToolTip = "알 수 없는 화면입니다.";
            return;
        }

        button.IsEnabled = item.IsAvailable;
        button.ToolTip = item.IsAvailable
            ? item.Description
            : $"{item.Name} 화면은 이미 사용 중입니다. ({item.OccupiedBy})";
    }

    protected override void OnClosed(EventArgs e)
    {
        _surfaceClaims.ReleaseOwner(_ownerId);
        base.OnClosed(e);
    }
}
