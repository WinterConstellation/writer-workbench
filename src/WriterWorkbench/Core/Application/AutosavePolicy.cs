namespace WriterWorkbench.Core.Application;

public static class AutosavePolicy
{
    public static bool ShouldAutosave(
        bool autosaveEnabled,
        bool dirty,
        bool loadingDocument,
        bool saveInProgress,
        DateTimeOffset lastEditAt,
        DateTimeOffset now,
        TimeSpan idleDelay)
    {
        if (!autosaveEnabled || !dirty || loadingDocument || saveInProgress)
        {
            return false;
        }

        return now - lastEditAt >= idleDelay;
    }
}
