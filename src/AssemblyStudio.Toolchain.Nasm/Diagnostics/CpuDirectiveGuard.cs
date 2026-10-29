using System.Globalization;
using System.Text.RegularExpressions;
using AssemblyStudio.Core;

namespace AssemblyStudio.Toolchain.Nasm.Diagnostics;

/// <summary>
/// Scans preprocessed NASM output (<c>nasm -E</c>, FR-027, ADR-0005 §3) for CPU directives requesting
/// a processor level other than 8086. Non-8086 directives are rejected as build errors at the line where
/// the directive appears, including directives expanded from macros or included files.
/// </summary>
internal static partial class CpuDirectiveGuard
{
    private const string SupportedCpuLevel = "8086";

    /// <summary>
    /// Scans <paramref name="preprocessedOutput"/> for CPU directives.
    /// A directive in <paramref name="assembledPath"/> (the file NASM preprocessed, possibly a temporary
    /// copy) is reported against <paramref name="documentPath"/> (the user-facing source file path, or
    /// <see langword="null"/> for an untitled document). Directives in included files are resolved relative
    /// to <paramref name="workingDirectory"/>.
    /// </summary>
    public static IReadOnlyList<Diagnostic> Scan(
        string preprocessedOutput,
        string assembledPath,
        string? documentPath,
        string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(preprocessedOutput);
        ArgumentNullException.ThrowIfNull(assembledPath);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var diagnostics = new List<Diagnostic>();
        int currentLine = 1;
        int step = 1;
        string currentFile = assembledPath;

        foreach (string rawLine in preprocessedOutput.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            Match lineMatch = LineDirectivePattern().Match(line);
            if (lineMatch.Success)
            {
                currentLine = int.Parse(lineMatch.Groups["line"].Value, CultureInfo.InvariantCulture);
                step = lineMatch.Groups["step"].Success
                    ? int.Parse(lineMatch.Groups["step"].Value, CultureInfo.InvariantCulture)
                    : 1;

                if (lineMatch.Groups["file"].Success)
                {
                    string candidate = lineMatch.Groups["file"].Value.Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        currentFile = candidate;
                    }
                }

                continue;
            }

            Match cpuMatch = CpuDirectivePattern().Match(line);
            if (cpuMatch.Success)
            {
                string level = cpuMatch.Groups["level"].Value;
                if (!string.Equals(level, SupportedCpuLevel, StringComparison.OrdinalIgnoreCase))
                {
                    string? reportedFile = ResolveReportedFile(currentFile, assembledPath, documentPath, workingDirectory);
                    diagnostics.Add(BuildMessages.Create(BuildMessages.UnsupportedCpu, reportedFile, currentLine, level));
                }
            }

            currentLine += step;
        }

        return diagnostics;
    }

    private static string? ResolveReportedFile(
        string file,
        string assembledPath,
        string? documentPath,
        string workingDirectory)
    {
        string resolved;
        try
        {
            resolved = Path.IsPathFullyQualified(file)
                ? Path.GetFullPath(file)
                : Path.GetFullPath(Path.Combine(workingDirectory, file));
        }
        catch (ArgumentException)
        {
            resolved = file;
        }
        catch (NotSupportedException)
        {
            resolved = file;
        }

        return string.Equals(Normalize(resolved), Normalize(assembledPath), StringComparison.OrdinalIgnoreCase)
            ? documentPath
            : resolved;
    }

    private static string Normalize(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (ArgumentException)
        {
            return path;
        }
        catch (NotSupportedException)
        {
            return path;
        }
    }

    [GeneratedRegex(@"^%line\s+(?<line>\d+)(?:\+(?<step>\d+))?(?:\s+(?<file>.*))?$", RegexOptions.CultureInvariant)]
    private static partial Regex LineDirectivePattern();

    [GeneratedRegex(@"^\s*\[\s*cpu\s+(?<level>[^\]\s]+)\s*\]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CpuDirectivePattern();
}
