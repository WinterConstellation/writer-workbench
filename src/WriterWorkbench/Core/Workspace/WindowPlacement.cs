namespace WriterWorkbench.Core.Workspace;

public sealed record WindowPlacement(
    double Left,
    double Top,
    double Width,
    double Height,
    string WindowState
);
