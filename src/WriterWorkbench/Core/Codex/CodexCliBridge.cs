using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WriterWorkbench.Core.Codex;

public sealed record CodexCliRunRequest(
    string Prompt,
    string WorkingDirectory,
    TimeSpan Timeout);

public sealed record CodexCliRunResult(
    bool Started,
    bool Success,
    bool TimedOut,
    int? ExitCode,
    string ExecutablePath,
    string StandardOutput,
    string StandardError,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);

public sealed class CodexCliBridge
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
    private readonly string _executableNameOrPath;

    public CodexCliBridge(string executableNameOrPath = "codex")
    {
        _executableNameOrPath = string.IsNullOrWhiteSpace(executableNameOrPath)
            ? "codex"
            : executableNameOrPath.Trim();
    }

    public string? ResolveExecutablePath()
    {
        if (Path.IsPathRooted(_executableNameOrPath))
        {
            return File.Exists(_executableNameOrPath) ? _executableNameOrPath : null;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var extensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var candidateNames = Path.HasExtension(_executableNameOrPath)
            ? new[] { _executableNameOrPath }
            : extensions.Select(extension => _executableNameOrPath + extension).Prepend(_executableNameOrPath);

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var candidateName in candidateNames)
            {
                var candidate = Path.Combine(directory, candidateName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    public static ProcessStartInfo CreateStartInfo(
        string executablePath,
        string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Directory.Exists(workingDirectory)
                ? workingDirectory
                : Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--sandbox");
        startInfo.ArgumentList.Add("read-only");
        startInfo.ArgumentList.Add("--ask-for-approval");
        startInfo.ArgumentList.Add("never");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add("--ephemeral");
        startInfo.ArgumentList.Add("-");
        return startInfo;
    }

    public async Task<CodexCliRunResult> RunAsync(
        CodexCliRunRequest request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.Now;
        var timeout = request.Timeout <= TimeSpan.Zero ? DefaultTimeout : request.Timeout;
        var prompt = request.Prompt?.Trim() ?? "";
        if (prompt.Length == 0)
        {
            return new CodexCliRunResult(
                false,
                false,
                false,
                null,
                ResolveExecutablePath() ?? _executableNameOrPath,
                "",
                "Codex CLI prompt is empty.",
                startedAt,
                DateTimeOffset.Now);
        }

        var executablePath = ResolveExecutablePath() ?? _executableNameOrPath;
        var startInfo = CreateStartInfo(executablePath, request.WorkingDirectory);
        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return CreateStartFailure(startedAt, executablePath, "Codex CLI process did not start.");
            }
        }
        catch (Win32Exception ex)
        {
            return CreateStartFailure(startedAt, executablePath, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateStartFailure(startedAt, executablePath, ex.Message);
        }

        await process.StandardInput.WriteAsync(prompt);
        await process.StandardInput.FlushAsync(cancellationToken);
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        var standardOutput = await outputTask;
        var standardError = await errorTask;
        int? exitCode = process.HasExited ? process.ExitCode : null;
        return new CodexCliRunResult(
            true,
            !timedOut && exitCode == 0,
            timedOut,
            exitCode,
            executablePath,
            standardOutput.Trim(),
            standardError.Trim(),
            startedAt,
            DateTimeOffset.Now);
    }

    private static CodexCliRunResult CreateStartFailure(
        DateTimeOffset startedAt,
        string executablePath,
        string error)
    {
        return new CodexCliRunResult(
            false,
            false,
            false,
            null,
            executablePath,
            "",
            error,
            startedAt,
            DateTimeOffset.Now);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }
}
