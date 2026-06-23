using WriterWorkbench.Core.Progress;

namespace WriterWorkbench.Tests;

public sealed class LongOperationProgressTrackerTests
{
    [Fact]
    public void EstimatesRemainingTimeFromCompletedSteps()
    {
        var startedAt = new DateTimeOffset(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);
        var tracker = new LongOperationProgressTracker("Stress 15k", 100, startedAt);

        var progress = tracker.Report(25, "Generating", startedAt.AddSeconds(10));

        Assert.Equal("Stress 15k", progress.OperationName);
        Assert.Equal("Generating", progress.Stage);
        Assert.Equal(25, progress.CompletedSteps);
        Assert.Equal(25, progress.PercentComplete);
        Assert.Equal(TimeSpan.FromSeconds(30), progress.EstimatedRemaining);
    }

    [Fact]
    public void ClampsCompletedStepsToTotal()
    {
        var startedAt = new DateTimeOffset(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);
        var tracker = new LongOperationProgressTracker("Save", 10, startedAt);

        var progress = tracker.Report(25, "Done", startedAt.AddSeconds(1));

        Assert.Equal(10, progress.CompletedSteps);
        Assert.Equal(100, progress.PercentComplete);
        Assert.Equal(TimeSpan.Zero, progress.EstimatedRemaining);
    }

    [Fact]
    public void KeepsRemainingUnknownUntilProgressStarts()
    {
        var startedAt = new DateTimeOffset(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);
        var tracker = new LongOperationProgressTracker("Load", 10, startedAt);

        var progress = tracker.Report(0, "Preparing", startedAt.AddSeconds(1));

        Assert.Null(progress.EstimatedRemaining);
        Assert.Equal(0, progress.PercentComplete);
    }
}
