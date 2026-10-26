using System.Text.RegularExpressions;

namespace Poc.Mapping;

public enum Severity { Info, Warning, Error, Fatal }

/// <summary>One NASM diagnostic in GNU format: <c>file:line: severity: message [-w+class]</c> (FR-029).</summary>
public sealed partial record Diagnostic(string File, int Line, Severity Severity, string Message, string? WarningClass)
{
    [GeneratedRegex(@"^(?<file>.+?):(?<line>\d+): (?<sev>info|warning|error|fatal|panic): (?<msg>.*?)(?: \[-w\+(?<cls>[\w-]+)\])?$")]
    private static partial Regex GnuLine();

    /// <summary>Parses NASM output. Lines that don't match are not diagnostics (callers show raw output as fallback).</summary>
    public static IReadOnlyList<Diagnostic> ParseAll(string output) =>
        output.Split('\n').Select(l => l.TrimEnd('\r')).Select(TryParse).OfType<Diagnostic>().ToList();

    public static Diagnostic? TryParse(string line)
    {
        var m = GnuLine().Match(line);
        if (!m.Success) return null;
        var sev = m.Groups["sev"].Value switch
        {
            "info" => Severity.Info,
            "warning" => Severity.Warning,
            "error" => Severity.Error,
            _ => Severity.Fatal,
        };
        return new Diagnostic(m.Groups["file"].Value, int.Parse(m.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture), sev,
            m.Groups["msg"].Value, m.Groups["cls"].Success ? m.Groups["cls"].Value : null);
    }
}
