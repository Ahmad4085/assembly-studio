namespace AssemblyStudio.Core;

/// <summary>How a program ended normally (FR-041, ADR-0004 §7, AC-064).</summary>
public enum TerminationPath
{
    /// <summary>INT 20h (requires CS = PSP).</summary>
    Int20h,

    /// <summary>INT 21h AH=00h, the DOS 1 terminate (requires CS = PSP, D-012).</summary>
    Int21hFunction00h,

    /// <summary>INT 21h AH=4Ch with the exit code in AL (any CS).</summary>
    Int21hFunction4Ch,

    /// <summary>A near RET to PSP:0000 through the initial stack word, which runs the INT 20h stored there.</summary>
    ReturnToPsp,

    /// <summary>Ctrl-C: the default INT 23h handler, or a program INT 23h handler returning with RETF and CF set (ADR-0004 §4).</summary>
    CtrlC,
}

/// <summary>The termination path and exit code of a program that ended normally (FR-041).</summary>
/// <param name="Path">The termination path.</param>
/// <param name="ExitCode">The DOS exit code: AL for <see cref="TerminationPath.Int21hFunction4Ch"/>, 00h for the other paths.</param>
public readonly record struct ProgramTermination(TerminationPath Path, byte ExitCode);
