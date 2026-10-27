namespace AssemblyStudio.Core;

/// <summary>
/// The emulated machine the debug session runs (C05, ADR-0004 §2): the CPU, its memory, the console
/// device and the interrupt router. Owned by the debug-session worker thread (architecture §4);
/// other threads see only immutable <see cref="MachineSnapshot"/>s.
/// </summary>
public interface IMachine
{
    /// <summary>The CPU engine (stepping is done through it, C04).</summary>
    ICpu Cpu { get; }

    /// <summary>The emulated memory with its write journal.</summary>
    IMemory Memory { get; }

    /// <summary>The console device (output buffer and input queue).</summary>
    IConsoleDevice Console { get; }

    /// <summary>The interrupt interception router, or <see langword="null"/> to vector every
    /// interrupt normally.</summary>
    IInterruptRouter? InterruptRouter { get; }
}
