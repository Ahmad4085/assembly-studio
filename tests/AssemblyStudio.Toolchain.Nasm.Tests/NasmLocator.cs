using System.Diagnostics;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

/// <summary>
/// Resolves the bundled NASM 3.02 binary for toolchain tests per ADR-0005.
/// Never falls back silently to PATH; missing tool fails with actionable instructions.
/// </summary>
public static class NasmLocator
{
    public const string ExpectedVersion = "3.02";

    public static string LocateNasmDirectory(string? overrideStartingDir = null)
    {
        var startDir = overrideStartingDir != null
            ? new DirectoryInfo(overrideStartingDir)
            : new DirectoryInfo(AppContext.BaseDirectory);

        var current = startDir;
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "tools", "nasm");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "nasm.exe")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(
            "Bundled NASM 3.02 not found. " +
            "Per ADR-0005, the bundled assembler must be fetched before running toolchain tests.\n" +
            "Please run: powershell -ExecutionPolicy Bypass -File build/fetch-nasm.ps1");
    }

    public static string GetNasmPath(string? overrideStartingDir = null)
    {
        var dir = LocateNasmDirectory(overrideStartingDir);
        var path = Path.Combine(dir, "nasm.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"nasm.exe not found at {path}", path);
        }
        return path;
    }

    public static string GetNdisasmPath(string? overrideStartingDir = null)
    {
        var dir = LocateNasmDirectory(overrideStartingDir);
        var path = Path.Combine(dir, "ndisasm.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"ndisasm.exe not found at {path}", path);
        }
        return path;
    }

    public static string GetVersion(string? nasmPath = null)
    {
        var exe = nasmPath ?? GetNasmPath();
        var psi = new ProcessStartInfo(exe, "-v")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exe}");

        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"nasm -v failed (exit {process.ExitCode}): {stderr}");
        }

        return stdout.Trim();
    }
}
