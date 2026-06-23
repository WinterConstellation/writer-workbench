namespace WriterWorkbench.Core.Focus;

public sealed record FocusSessionOptions(
    TimeSpan Duration,
    int ExitConfirmMinChars,
    bool EnterFullscreen
);
