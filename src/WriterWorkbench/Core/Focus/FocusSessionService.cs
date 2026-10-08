namespace WriterWorkbench.Core.Focus;

public sealed class FocusSessionService
{
    private FocusSessionState? _state;

    public FocusSessionState Start(FocusSessionOptions options)
    {
        var now = DateTimeOffset.Now;
        _state = new FocusSessionState(now, now.Add(options.Duration), options);
        return _state;
    }

    public bool CanExitEarly(string confirmationText)
    {
        return _state is not null &&
               confirmationText.Trim().Length >= _state.Options.ExitConfirmMinChars;
    }
}
