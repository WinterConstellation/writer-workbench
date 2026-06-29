using WriterWorkbench.Core.Codex;

namespace WriterWorkbench.Tests;

public sealed class CodexCliBridgeTests
{
    [Fact]
    public void CreateStartInfoUsesReadOnlyNonInteractiveExec()
    {
        var startInfo = CodexCliBridge.CreateStartInfo("codex", @"C:\WriterWorkbench\Projects\Sample.writerproj");

        Assert.Equal("codex", startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardInput);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Contains("exec", startInfo.ArgumentList);
        Assert.Contains("--sandbox", startInfo.ArgumentList);
        Assert.Contains("read-only", startInfo.ArgumentList);
        Assert.Contains("--ask-for-approval", startInfo.ArgumentList);
        Assert.Contains("never", startInfo.ArgumentList);
        Assert.Contains("--skip-git-repo-check", startInfo.ArgumentList);
        Assert.Contains("--ephemeral", startInfo.ArgumentList);
        Assert.Equal("-", startInfo.ArgumentList[^1]);
    }

    [Fact]
    public async Task RunAsyncRejectsBlankPromptWithoutStartingProcess()
    {
        var bridge = new CodexCliBridge("missing-codex-for-test");

        var result = await bridge.RunAsync(
            new CodexCliRunRequest("   ", Environment.CurrentDirectory, TimeSpan.FromSeconds(1)),
            CancellationToken.None);

        Assert.False(result.Started);
        Assert.False(result.Success);
        Assert.Contains("empty", result.StandardError, StringComparison.OrdinalIgnoreCase);
    }
}
