using System.Diagnostics;

namespace GasEmissionsCheck.Common.Shared.Utils;

public static class ProcessRunner
{
    private const int ExitCodeCommandNotFound = 127;

    public static async Task<ProcessRunResult> RunToBytesAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            if (!process.Start())
            {
                return ProcessRunResult.Failed(ExitCodeCommandNotFound, "Process failed to start.");
            }
        }
        catch (Exception ex)
        {
            return ProcessRunResult.Failed(ExitCodeCommandNotFound, ex.ToString());
        }

        var stderrTask = process.StandardError.ReadToEndAsync();

        await using var ms = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken);

        using var timeoutCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationToken.CancelAfter(timeout);

        try
        {
            await Task.WhenAll(stdoutTask, process.WaitForExitAsync(timeoutCancellationToken.Token));
        }
        catch (OperationCanceledException)
        {
            TryKill(process);

            return ProcessRunResult.Timeout(await Safe(stderrTask));
        }

        return new ProcessRunResult(
            process.ExitCode,
            ms.ToArray(),
            await Safe(stderrTask));
    }

    private static async Task<string> Safe(Task<string> task)
    {
        try
        {
            return await task;
        }
        catch
        {
            return "";
        }
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
        catch { }
    }
}

public sealed record ProcessRunResult(int ExitCode, byte[] StdOut, string StdErr)
{
    public bool IsSuccess => ExitCode == 0;

    public static ProcessRunResult Failed(int code, string err)
        => new(code, [], err);

    public static ProcessRunResult Timeout(string err)
        => new(124, [], err);
}
