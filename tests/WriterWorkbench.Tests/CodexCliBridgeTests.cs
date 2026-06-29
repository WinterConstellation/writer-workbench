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
        Assert.Contains("--cd", startInfo.ArgumentList);
        Assert.Contains(startInfo.WorkingDirectory, startInfo.ArgumentList);
        Assert.DoesNotContain("--ask-for-approval", startInfo.ArgumentList);
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

    [Fact]
    public void ResolveExecutablePathPrefersWindowsCommandShim()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var oldPath = Environment.GetEnvironmentVariable("PATH");
        var oldPathExt = Environment.GetEnvironmentVariable("PATHEXT");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(Path.Combine(tempRoot, "fakecodex"), "not executable on Windows");
        File.WriteAllText(Path.Combine(tempRoot, "fakecodex.cmd"), "@echo fake");

        try
        {
            Environment.SetEnvironmentVariable("PATH", tempRoot);
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE;.CMD;.BAT");

            var bridge = new CodexCliBridge("fakecodex");

            Assert.Equal(
                Path.Combine(tempRoot, "fakecodex.cmd"),
                bridge.ResolveExecutablePath());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", oldPath);
            Environment.SetEnvironmentVariable("PATHEXT", oldPathExt);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveExecutablePathRejectsPackagedWindowsAppsCodexResource()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var codexPath = Path.Combine(
            tempRoot,
            "Program Files",
            "WindowsApps",
            "OpenAI.Codex_26.623.8305.0_x64__2p2nqsd0c76g0",
            "app",
            "resources",
            "codex.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(codexPath)!);
        File.WriteAllText(codexPath, "blocked packaged executable");

        try
        {
            var bridge = new CodexCliBridge(codexPath);

            Assert.Null(bridge.ResolveExecutablePath());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
