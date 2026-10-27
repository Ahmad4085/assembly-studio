namespace AssemblyStudio.Core;

/// <summary>
/// The CPU execution engine (C04, ADR-0003): decode and execute one instruction per
/// <see cref="Step"/>. It must not know about the UI, DOS or files, and must not expose mutable
/// state (architecture §2). Replaceable per CPU model via new <c>Engine.*</c> implementations
/// (NFR-019, architecture §9). Concrete implementations take their <see cref="IMemory"/> and
/// <see cref="IInterruptRouter"/> through construction.
/// </summary>
public interface ICpu
{
    /// <summary>Current CPU state (a copy; reads never alias live registers).</summary>
    CpuState State { get; }

    /// <summary>Replaces the CPU state (test setup, replay and service register updates).</summary>
    void SetState(CpuState state);

    /// <summary>Fetches, decodes and executes one instruction and reports the outcome.
    /// Never throws for guest-program behavior: unsupported/undefined execution halts with a
    /// <see cref="HaltReason"/> instead (FR-037, FR-038).</summary>
    StepResult StepInstruction();
}
