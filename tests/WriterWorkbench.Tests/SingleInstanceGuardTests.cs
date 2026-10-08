using WriterWorkbench.Core.Application;

namespace WriterWorkbench.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void SecondAcquireForSameNameFails()
    {
        var name = "WriterWorkbenchTests-" + Guid.NewGuid().ToString("N");
        using var first = SingleInstanceGuard.TryAcquire(name);
        using var second = SingleInstanceGuard.TryAcquire(name);

        Assert.NotNull(first);
        Assert.Null(second);
    }
}
