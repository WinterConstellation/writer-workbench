namespace WriterWorkbench.Core.Workspace;

public sealed record WorkspacePreset(
    int Slot,
    string Name,
    MonitorRegion Region,
    bool AutoApplyOnStartup,
    WindowPlacement? Placement = null,
    IReadOnlyList<WorkspaceDetachedWindowPlacement>? DetachedWindows = null
);
