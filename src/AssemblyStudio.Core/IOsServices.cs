namespace AssemblyStudio.Core;

/// <summary>
/// OS services behind the interrupt router (C06, ADR-0004 §3–4): for the MVP the DOS subset
/// INT 21h 00h/01h/02h/07h/08h/09h/0Ah/4Ch and INT 20h with DOS 3.3/4.0 semantics (D-008, D-012).
/// Services must not touch the host: no files, devices, clock or network (NFR-013). They read the
/// request from <paramref name="state"/>, write results through <paramref name="machine"/>
/// (memory and console, and <see cref="ICpu.SetState"/> for register results) and return the
/// resulting <see cref="StepResult"/>.
/// </summary>
public interface IOsServices
{
    /// <summary>
    /// Handles one service call. Returns the resulting <see cref="StepResult"/> when this vector is
    /// a supported service (including <see cref="StepOutcome.WaitingForInput"/> when input is
    /// needed, and <see cref="StepOutcome.Halted"/> with <see cref="HaltReason.UnsupportedService"/>
    /// for an unsupported function, FR-040); returns <see langword="null"/> when the vector is not
    /// one of its services, leaving the decision to the router.
    /// </summary>
    /// <param name="vector">Interrupt number (for the MVP: 0x20 and 0x21).</param>
    /// <param name="state">CPU state at the INT instruction (CS:IP still on it; AH is the function).</param>
    /// <param name="machine">The machine to read from and write results to.</param>
    StepResult? HandleService(byte vector, CpuState state, IMachine machine);
}
