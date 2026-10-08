namespace WriterWorkbench.Core.Progress;

public sealed class LongOperationProgressTracker
{
    private readonly DateTimeOffset _startedAt;

    public LongOperationProgressTracker(string operationName, int totalSteps)
        : this(operationName, totalSteps, DateTimeOffset.Now)
    {
    }

    public LongOperationProgressTracker(string operationName, int totalSteps, DateTimeOffset startedAt)
    {
        if (totalSteps < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSteps), "Total steps must be positive.");
        }

        OperationName = operationName;
        TotalSteps = totalSteps;
        _startedAt = startedAt;
    }

    public string OperationName { get; }

    public int TotalSteps { get; }

    public LongOperationProgress Report(int completedSteps, string stage)
    {
        return Report(completedSteps, stage, DateTimeOffset.Now);
    }

    public LongOperationProgress Report(int completedSteps, string stage, DateTimeOffset now)
    {
        var clampedCompleted = Math.Clamp(completedSteps, 0, TotalSteps);
        var elapsed = now - _startedAt;
        var percent = (int)Math.Round(clampedCompleted * 100.0 / TotalSteps);

        TimeSpan? remaining = null;
        if (clampedCompleted >= TotalSteps)
        {
            remaining = TimeSpan.Zero;
        }
        else if (clampedCompleted > 0 && elapsed > TimeSpan.Zero)
        {
            var secondsPerStep = elapsed.TotalSeconds / clampedCompleted;
            remaining = TimeSpan.FromSeconds(secondsPerStep * (TotalSteps - clampedCompleted));
        }

        return new LongOperationProgress(
            OperationName,
            stage,
            clampedCompleted,
            TotalSteps,
            percent,
            elapsed,
            remaining);
    }
}
