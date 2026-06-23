namespace WriterWorkbench.Core.Progress;

public sealed record LongOperationProgress(
    string OperationName,
    string Stage,
    int CompletedSteps,
    int TotalSteps,
    int PercentComplete,
    TimeSpan Elapsed,
    TimeSpan? EstimatedRemaining
);
