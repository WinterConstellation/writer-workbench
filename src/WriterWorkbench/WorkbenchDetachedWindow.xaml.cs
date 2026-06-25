using System.Windows;
using WriterWorkbench.Core.Workspace;

namespace WriterWorkbench;

public partial class WorkbenchDetachedWindow : Window
{
    private readonly WorkbenchSurfaceClaimRegistry _surfaceClaims;
    private readonly string _ownerId;

    public WorkbenchDetachedWindow(WorkbenchSurfaceClaimRegistry surfaceClaims)
        : this(surfaceClaims, $"detached-{Guid.NewGuid():N}")
    {
    }

    public WorkbenchDetachedWindow(WorkbenchSurfaceClaimRegistry surfaceClaims, string ownerId)
    {
        _surfaceClaims = surfaceClaims;
        _ownerId = ownerId;
        InitializeComponent();
        RefreshAvailability();
        DetachedWorkbenchStatusText.Text = "작업대를 선택하세요.";
    }

    public string? AssignedSurfaceId { get; private set; }

    public string StatusDisplay => DetachedWorkbenchStatusText.Text;

    public string AssignedSurfaceDisplay => DetachedAssignedSurfaceText.Text;

    public bool TrySelectSurface(string surfaceId)
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
        RefreshAvailability();
        return true;
    }

    private void SurfaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string surfaceId })
        {
            TrySelectSurface(surfaceId);
        }
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

    private static void ApplyAvailability(System.Windows.Controls.Button button, IReadOnlyDictionary<string, WorkbenchSurfaceAvailability> availability)
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
