using WriterWorkbench.Core.Application;

namespace WriterWorkbench.Tests;

public sealed class AutosavePolicyTests
{
    [Fact]
    public void WaitsUntilEditorHasBeenIdle()
    {
        var now = DateTimeOffset.Parse("2026-06-23T19:55:00+09:00");

        var shouldSave = AutosavePolicy.ShouldAutosave(
            autosaveEnabled: true,
            dirty: true,
            loadingDocument: false,
            saveInProgress: false,
            lastEditAt: now - TimeSpan.FromSeconds(1),
            now,
            idleDelay: TimeSpan.FromSeconds(3));

        Assert.False(shouldSave);
    }

    [Fact]
    public void BlocksOverlappingSaves()
    {
        var now = DateTimeOffset.Parse("2026-06-23T19:55:00+09:00");

        var shouldSave = AutosavePolicy.ShouldAutosave(
            autosaveEnabled: true,
            dirty: true,
            loadingDocument: false,
            saveInProgress: true,
            lastEditAt: now - TimeSpan.FromMinutes(1),
            now,
            idleDelay: TimeSpan.FromSeconds(3));

        Assert.False(shouldSave);
    }

    [Fact]
    public void SavesDirtyDocumentAfterIdleDelay()
    {
        var now = DateTimeOffset.Parse("2026-06-23T19:55:00+09:00");

        var shouldSave = AutosavePolicy.ShouldAutosave(
            autosaveEnabled: true,
            dirty: true,
            loadingDocument: false,
            saveInProgress: false,
            lastEditAt: now - TimeSpan.FromSeconds(4),
            now,
            idleDelay: TimeSpan.FromSeconds(3));

        Assert.True(shouldSave);
    }
}
