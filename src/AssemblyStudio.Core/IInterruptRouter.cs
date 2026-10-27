namespace AssemblyStudio.Core;

/// <summary>
/// Intercept callback between the CPU and the IVT (ADR-0003 §2, ADR-0004 §3). The engine calls it
/// for <c>INT n</c>, CPU exceptions and the like, <b>before</b> vectoring.
/// </summary>
public interface IInterruptRouter
{
    /// <summary>
    /// Called before the CPU vectors through the IVT.
    /// Returns the resulting <see cref="StepResult"/> when the host handled the interrupt
    /// (default vector: a DOS service, a runtime error, an unsupported service, or
    /// <see cref="StepOutcome.WaitingForInput"/>); returns <see langword="null"/> to let the CPU
    /// vector normally (a program-installed vector, real 8086 semantics: push FLAGS/CS/IP,
    /// clear IF/TF, ADR-0003 §2).
    /// </summary>
    /// <param name="vector">Interrupt number (0–255).</param>
    /// <param name="state">CPU state at the INT instruction (CS:IP still on it).</param>
    StepResult? Intercept(byte vector, CpuState state);
}
