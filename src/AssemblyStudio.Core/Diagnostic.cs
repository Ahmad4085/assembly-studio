namespace AssemblyStudio.Core;

/// <summary>Severity of a <see cref="Diagnostic"/>.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Supplementary information, such as a hint or a note on another diagnostic.</summary>
    Info,

    /// <summary>A problem that doesn't stop the build.</summary>
    Warning,

    /// <summary>A problem that fails the build.</summary>
    Error,
}

/// <summary>
/// One build message (FR-029, FR-066, NFR-024): a stable code, what happened, where (a source file and
/// line, or the build as a whole when both are <see langword="null"/>) and a suggested next step.
/// </summary>
/// <param name="Code">Stable message code from the build message catalog (for example <c>ASM001</c>, <c>BLD006</c>).</param>
/// <param name="Severity">Severity.</param>
/// <param name="What">What happened. For assembler diagnostics this is the assembler's own message.</param>
/// <param name="File">The source file the message is about, or <see langword="null"/> for the document being built
/// (an untitled document) or for the build as a whole.</param>
/// <param name="Line">The 1-based source line, or <see langword="null"/> when the message is not about one line.</param>
/// <param name="NextStep">A suggested next step.</param>
public sealed record Diagnostic(string Code, DiagnosticSeverity Severity, string What, string? File, int? Line, string NextStep);
