using System.Globalization;
using System.Resources;
using AssemblyStudio.Core;

namespace AssemblyStudio.Toolchain.Nasm.Diagnostics;

/// <summary>
/// The build message catalog (FR-066, NFR-024, AC-055): every build message has a stable code, a
/// severity, what happened, where (set when the message is created) and a next step. The texts live in
/// <c>BuildMessages.resx</c> (NFR-032). Codes are never renumbered or reused.
/// </summary>
internal static class BuildMessages
{
    /// <summary>Assembler error (NASM <c>error</c>, <c>fatal</c> or <c>panic</c>); the text is NASM's.</summary>
    public const string AssemblerError = "ASM001";

    /// <summary>Assembler warning; the text is NASM's.</summary>
    public const string AssemblerWarning = "ASM002";

    /// <summary>Assembler note (NASM <c>info</c>); the text is NASM's.</summary>
    public const string AssemblerInfo = "ASM003";

    /// <summary>No usable assembler; no build was attempted (FR-030, AC-016).</summary>
    public const string AssemblerMissing = "BLD001";

    /// <summary>The build ran past the timeout and was stopped (FR-030, AC-019).</summary>
    public const string TimedOut = "BLD002";

    /// <summary>The assembler crashed (FR-030, AC-019).</summary>
    public const string Crashed = "BLD003";

    /// <summary>Non-zero exit without a diagnostic for a source line; raw output shown (FR-029, FR-030).</summary>
    public const string FailedWithoutDiagnostics = "BLD004";

    /// <summary>The user cancelled the build.</summary>
    public const string Cancelled = "BLD005";

    /// <summary>The image exceeds the .COM size limit (FR-031, AC-020).</summary>
    public const string ImageTooLarge = "BLD006";

    /// <summary>MASM/TASM-style source detected in a failed build (FR-021, AC-014).</summary>
    public const string MasmHint = "BLD007";

    /// <summary>The assembler's version is older than 3.02 or unknown (FR-024).</summary>
    public const string VersionWarning = "BLD008";

    /// <summary>A CPU directive requests a processor level other than 8086 (FR-027, ADR-0005 §3).</summary>
    public const string UnsupportedCpu = "BLD009";

    /// <summary>The largest .COM image: 64 KiB minus the 256-byte PSP (FR-031).</summary>
    public const int MaxComImageSize = 65_280;

    /// <summary>Every code with its severity (the catalog the completeness test checks).</summary>
    public static readonly IReadOnlyDictionary<string, DiagnosticSeverity> Catalog = new Dictionary<string, DiagnosticSeverity>
    {
        [AssemblerError] = DiagnosticSeverity.Error,
        [AssemblerWarning] = DiagnosticSeverity.Warning,
        [AssemblerInfo] = DiagnosticSeverity.Info,
        [AssemblerMissing] = DiagnosticSeverity.Error,
        [TimedOut] = DiagnosticSeverity.Error,
        [Crashed] = DiagnosticSeverity.Error,
        [FailedWithoutDiagnostics] = DiagnosticSeverity.Error,
        [Cancelled] = DiagnosticSeverity.Info,
        [ImageTooLarge] = DiagnosticSeverity.Error,
        [MasmHint] = DiagnosticSeverity.Info,
        [VersionWarning] = DiagnosticSeverity.Warning,
        [UnsupportedCpu] = DiagnosticSeverity.Error,
    };

    /// <summary>Codes whose "what" text comes from the assembler instead of the catalog.</summary>
    public static readonly IReadOnlySet<string> AssemblerTextCodes = new HashSet<string> { AssemblerError, AssemblerWarning, AssemblerInfo };

    internal static readonly ResourceManager Resources =
        new("AssemblyStudio.Toolchain.Nasm.Diagnostics.BuildMessages", typeof(BuildMessages).Assembly);

    /// <summary>Creates a catalog message, formatting its "what" template with <paramref name="args"/>.</summary>
    public static Diagnostic Create(string code, string? file, int? line, params object?[] args)
    {
        string template = Text(code + "_What");
        return new Diagnostic(code, Catalog[code], string.Format(CultureInfo.CurrentCulture, template, args), file, line, Text(code + "_Next"));
    }

    /// <summary>Wraps an assembler diagnostic (ASM001–ASM003) with its catalog next step.</summary>
    public static Diagnostic FromAssembler(string code, string what, string? file, int? line) =>
        new(code, Catalog[code], what, file, line, Text(code + "_Next"));

    private static string Text(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new InvalidOperationException($"Build message resource '{key}' is missing.");
}
