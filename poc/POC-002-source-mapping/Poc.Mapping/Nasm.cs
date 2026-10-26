using System.Diagnostics;
using System.Text;

namespace Poc.Mapping;

public sealed record BuildOutput(
    int ExitCode, bool TimedOut, IReadOnlyList<Diagnostic> Diagnostics, string RawOutput,
    byte[]? Image, string? ListingText, string? MapText, string WorkDir);

/// <summary>
/// Invokes NASM safely (R-ASM, NFR-014): no shell, argument list, isolated per-build temp directory,
/// timeout with process-tree kill, captured output capped in size.
/// </summary>
public sealed class Nasm(string nasmPath, TimeSpan timeout)
{
    public const int OutputCapChars = 1 << 20;

    public string NasmPath { get; } = nasmPath;

    public static string FindOnPath() =>
        Environment.GetEnvironmentVariable("NASM_PATH")
        ?? (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Select(d => Path.Combine(d.Trim('"'), "nasm.exe")).FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException("nasm.exe not found (set NASM_PATH or add NASM to PATH).");

    /// <summary>Assembles <paramref name="sourceFile"/> as an 8086 flat .COM with listing and map.</summary>
    public BuildOutput Build(string sourceFile)
    {
        string work = Directory.CreateTempSubdirectory("asmstudio-poc002-").FullName;
        string com = Path.Combine(work, "out.com"), lst = Path.Combine(work, "out.lst"), map = Path.Combine(work, "out.map");
        var args = new List<string>
        {
            "-f", "bin",
            "--before", "cpu 8086",
            "--before", $"[map all {map}]",
            "-l", lst,
            "-Xgnu",
            "--reproducible",
            "-o", com,
            Path.GetFullPath(sourceFile),
        };
        var (exit, timedOut, output) = Run(args, Path.GetDirectoryName(Path.GetFullPath(sourceFile))!);
        return new BuildOutput(
            exit, timedOut, Diagnostic.ParseAll(output), output,
            File.Exists(com) ? File.ReadAllBytes(com) : null,
            File.Exists(lst) ? File.ReadAllText(lst) : null,
            File.Exists(map) ? File.ReadAllText(map) : null,
            work);
    }

    /// <summary>Runs the preprocessor only (<c>-E</c>), used to detect CPU directives hidden in macros or includes.</summary>
    public string Preprocess(string sourceFile)
    {
        var (_, _, output) = Run(["-E", Path.GetFullPath(sourceFile)], Path.GetDirectoryName(Path.GetFullPath(sourceFile))!);
        return output;
    }

    private (int Exit, bool TimedOut, string Output) Run(List<string> args, string workingDir)
    {
        var psi = new ProcessStartInfo(NasmPath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDir,
        };
        foreach (string a in args) psi.ArgumentList.Add(a);

        var output = new StringBuilder();
        void Append(string? line)
        {
            if (line is null) return;
            lock (output) { if (output.Length < OutputCapChars) output.AppendLine(line); }
        }

        using var p = new Process { StartInfo = psi };
        p.OutputDataReceived += (_, e) => Append(e.Data);
        p.ErrorDataReceived += (_, e) => Append(e.Data);
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        if (!p.WaitForExit(timeout))
        {
            p.Kill(entireProcessTree: true);
            p.WaitForExit();
            return (-1, true, output.ToString());
        }
        p.WaitForExit(); // flush async readers
        return (p.ExitCode, false, output.ToString());
    }
}
