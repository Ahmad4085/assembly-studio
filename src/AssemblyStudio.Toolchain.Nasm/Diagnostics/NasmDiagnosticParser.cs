using System.Globalization;
using System.Text.RegularExpressions;
using AssemblyStudio.Core;

namespace AssemblyStudio.Toolchain.Nasm.Diagnostics;

/// <summary>
/// Parses NASM's GNU-format diagnostics (<c>-Xgnu</c>, FR-029):
/// <c>file:line: info|warning|error|fatal|panic: message [-w+class]</c>. The message is kept as NASM
/// wrote it, including a warning-class suffix. Lines in any other form are not diagnostics; they stay
/// in the raw output, which the UI shows unmodified.
/// </summary>
internal static partial class NasmDiagnosticParser
{
    /// <summary>Parses <paramref name="output"/>. A diagnostic about <paramref name="assembledPath"/> (the
    /// file NASM was given, possibly a temporary copy) is reported against <paramref name="documentPath"/>
    /// (the saved path, or <see langword="null"/> for an untitled document).</summary>
    public static IReadOnlyList<Diagnostic> Parse(string output, string assembledPath, string? documentPath)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (string raw in output.Split('\n'))
        {
            Match match = GnuLine().Match(raw.TrimEnd('\r'));
            if (!match.Success)
            {
                continue;
            }

            string code = match.Groups["sev"].Value switch
            {
                "info" => BuildMessages.AssemblerInfo,
                "warning" => BuildMessages.AssemblerWarning,
                _ => BuildMessages.AssemblerError,
            };
            string file = match.Groups["file"].Value;
            string? reported = string.Equals(Normalize(file), Normalize(assembledPath), StringComparison.OrdinalIgnoreCase) ? documentPath : file;
            int line = int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture);
            diagnostics.Add(BuildMessages.FromAssembler(code, match.Groups["msg"].Value, reported, line));
        }

        return diagnostics;
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

    [GeneratedRegex(@"^(?<file>.+?):(?<line>\d+): (?<sev>info|warning|error|fatal|panic): (?<msg>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex GnuLine();
}
