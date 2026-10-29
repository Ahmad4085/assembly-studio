using System.Text.RegularExpressions;

namespace AssemblyStudio.Toolchain.Nasm.Diagnostics;

/// <summary>
/// Spots MASM/TASM-only constructs in a source that failed to build (FR-021, AC-014): simplified
/// segment directives (<c>.model</c>, <c>.code</c>, <c>.data</c>, <c>.stack</c>, …), <c>assume</c>,
/// <c>name proc</c>/<c>name endp</c>, <c>name segment</c>/<c>name ends</c> and <c>@data</c>. Comments are
/// ignored. NASM's own <c>segment .text</c> form doesn't match.
/// </summary>
internal static partial class MasmHint
{
    /// <summary>The first MASM-style construct and its 1-based line, or <see langword="null"/>.</summary>
    public static (string Construct, int Line)? Find(string source)
    {
        string[] lines = source.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string code = lines[i];
            int comment = code.IndexOf(';', StringComparison.Ordinal);
            if (comment >= 0)
            {
                code = code[..comment];
            }

            Match match = MasmConstruct().Match(code);
            if (match.Success)
            {
                return (match.Value.Trim(), i + 1);
            }
        }

        return null;
    }

    [GeneratedRegex(
        @"^\s*\.(model|code|data|stack|const|startup|exit|386|486|586)\b.*|^\s*assume\b.*|^\s*[A-Za-z_@$?][\w@$?]*\s+(proc|endp|segment|ends)\b.*|@data\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MasmConstruct();
}
