namespace AssemblyStudio.Core;

/// <summary>Outcome of a single <see cref="ICpu.StepInstruction"/> or an intercepted interrupt.</summary>
public enum StepOutcome
{
    /// <summary>One instruction executed to completion.</summary>
    Executed,

    /// <summary>Execution halted; the state remains inspectable (FR-037, FR-038).</summary>
    Halted,

    /// <summary>A service is waiting for console input; CS:IP stays on the INT instruction so it
    /// re-executes when input arrives (ADR-0004 §5).</summary>
    WaitingForInput,

    /// <summary>The program ended normally through a termination service; CS:IP stays on the
    /// terminating instruction and <see cref="StepResult.Termination"/> holds the path and exit code (FR-041).</summary>
    Terminated,
}

/// <summary>Reason a step ended in <see cref="StepOutcome.Halted"/>. <see cref="StepOutcome.Executed"/>,
/// <see cref="StepOutcome.WaitingForInput"/> and <see cref="StepOutcome.Terminated"/> always report <see cref="None"/>.</summary>
public enum HaltReason
{
    /// <summary>Not halted.</summary>
    None,

    /// <summary>Unsupported or undocumented opcode; CS:IP, bytes and reason are inspectable (FR-037, ADR-0003 §4).</summary>
    UnsupportedInstruction,

    /// <summary>CPU exception (INT 0/1/3/4) with the default vector (FR-038, ADR-0004 §3).</summary>
    RuntimeError,

    /// <summary>Unsupported INT xx AH=yy function (FR-040, ADR-0004 §3).</summary>
    UnsupportedService,

    /// <summary>The HLT instruction was executed (ADR-0003 §4).</summary>
    Halt,
}

/// <summary>
/// Report of one executed instruction step (architecture §3, flows 3–5). Carries the outcome, the
/// address of the instruction, the mask of flags the instruction left undefined (ADR-0003 §3) and,
/// when halted, the reason. The CPU's updated state is read via <see cref="ICpu.State"/>, not from
/// this record.
/// </summary>
/// <param name="Outcome">What happened in the step.</param>
/// <param name="InstructionCs">CS of the instruction that was executed or caused the halt.</param>
/// <param name="InstructionIp">IP of the instruction that was executed or caused the halt.</param>
/// <param name="HaltReason">Halt reason, or <see cref="HaltReason.None"/>.</param>
/// <param name="UndefinedFlags">Mask of flags left undefined by the instruction (ADR-0003 §3).</param>
public readonly record struct StepResult(
    StepOutcome Outcome,
    ushort InstructionCs,
    ushort InstructionIp,
    HaltReason HaltReason,
    FlagBit UndefinedFlags)
{
    /// <summary>The termination path and exit code when <see cref="Outcome"/> is
    /// <see cref="StepOutcome.Terminated"/>, else <see langword="null"/> (FR-041).</summary>
    public ProgramTermination? Termination { get; init; }

    /// <summary>The runtime's message for a halt (code, what, where, next step; FR-038, FR-040, FR-066), when the component
    /// that halted provides one; else <see langword="null"/>.</summary>
    public RuntimeMessage? Message { get; init; }
}

/// <summary>A runtime message in the catalog's structure (FR-066, NFR-024): a stable code, what happened, where and a
/// suggested next step.</summary>
/// <param name="Code">Stable message code (for example <c>RTE001</c>).</param>
/// <param name="What">What happened.</param>
/// <param name="Where">Where (for example <c>at 0800:0105</c>).</param>
/// <param name="NextStep">A suggested next step.</param>
public sealed record RuntimeMessage(string Code, string What, string Where, string NextStep);
