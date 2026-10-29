using System.Diagnostics;
using System.Text;

namespace AssemblyStudio.Toolchain.Nasm;

/// <summary>What happened to one assembler process run.</summary>
internal sealed record ProcessRun(
    int? ExitCode,
    bool TimedOut,
    bool Cancelled,
    string StandardOutput,
    string StandardError,
    bool Truncated);

/// <summary>
/// Runs an external process safely (NFR-014, ADR-0005 §2): no shell, arguments passed as a list, a
/// timeout and cancellation that kill the whole process tree, and each output stream captured up to a
/// cap (the rest is drained and dropped so the process can't block on a full pipe).
/// </summary>
internal static class ProcessRunner
{
    public static async Task<ProcessRun> RunAsync(
        string executable,
        IEnumerable<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        int outputCap,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var stdout = new CappedText(outputCap);
        var stderr = new CappedText(outputCap);
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => stdout.AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => stderr.AppendLine(e.Data);

        process.Start();
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);
        bool timedOut = false;
        bool cancelled = false;
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            cancelled = cancellationToken.IsCancellationRequested;
            timedOut = !cancelled;
            Kill(process);
        }

        // Let the asynchronous readers drain what the process wrote before it exited or was killed.
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

        return new ProcessRun(
            timedOut || cancelled ? null : process.ExitCode,
            timedOut,
            cancelled,
            stdout.ToString(),
            stderr.ToString(),
            stdout.Truncated || stderr.Truncated);
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // It exited in the meantime.
        }
    }

    /// <summary>Line-based text capture that stops growing at the cap.</summary>
    private sealed class CappedText(int cap)
    {
        private readonly StringBuilder _text = new();

        public bool Truncated { get; private set; }

        public void AppendLine(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (_text)
            {
                int room = cap - _text.Length;
                if (room <= 0)
                {
                    Truncated = true;
                    return;
                }

                if (line.Length + Environment.NewLine.Length > room)
                {
                    _text.Append(line.AsSpan(0, Math.Min(line.Length, room)));
                    Truncated = true;
                    return;
                }

                _text.AppendLine(line);
            }
        }

        public override string ToString()
        {
            lock (_text)
            {
                return _text.ToString();
            }
        }
    }
}
