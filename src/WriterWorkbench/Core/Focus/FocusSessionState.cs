namespace WriterWorkbench.Core.Focus;

public sealed record FocusSessionState(
    DateTimeOffset StartedAt,
    DateTimeOffset EndsAt,
    FocusSessionOptions Options
);
