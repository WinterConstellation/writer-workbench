using WriterWorkbench.Core.Focus;

namespace WriterWorkbench.Tests;

public sealed class FocusSessionServiceTests
{
    [Fact]
    public void RejectsShortEarlyExitConfirmation()
    {
        var service = new FocusSessionService();
        service.Start(new FocusSessionOptions(TimeSpan.FromMinutes(40), 20, true));

        Assert.False(service.CanExitEarly("too short"));
        Assert.True(service.CanExitEarly("12345678901234567890"));
    }
}
