using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Memory;

namespace AssemblyStudio.Runtime;

/// <summary>
/// Composition of the emulated real-mode machine: CPU engine, 1 MiB memory, console device,
/// optional interrupt router and optional OS services (C05, ADR-0004 §2).
/// The debug-session worker thread owns and mutates the machine state; other components see only
/// immutable <see cref="MachineSnapshot"/> copies (ADR-0007 §2).
/// </summary>
public sealed class Machine : IMachine
{
    /// <inheritdoc />
    public ICpu Cpu { get; }

    /// <inheritdoc />
    public IMemory Memory { get; }

    /// <inheritdoc />
    public IConsoleDevice Console { get; }

    /// <inheritdoc />
    public IInterruptRouter? InterruptRouter { get; }

    /// <summary>
    /// The injected OS services (such as DOS INT 20h/21h emulation), or <see langword="null"/> when no OS services layer is attached.
    /// </summary>
    public IOsServices? OsServices { get; }

    /// <summary>
    /// Creates a new emulated machine instance.
    /// </summary>
    /// <param name="cpu">The CPU execution engine (required).</param>
    /// <param name="memory">The emulated memory (defaults to a fresh <see cref="RealModeMemory"/> instance if omitted).</param>
    /// <param name="console">The console device (defaults to <see cref="NullConsoleDevice"/> if omitted).</param>
    /// <param name="interruptRouter">Optional interrupt interception router.</param>
    /// <param name="osServices">Optional OS services implementation.</param>
    public Machine(
        ICpu cpu,
        IMemory? memory = null,
        IConsoleDevice? console = null,
        IInterruptRouter? interruptRouter = null,
        IOsServices? osServices = null)
    {
        Cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        Memory = memory ?? new RealModeMemory();
        Console = console ?? new NullConsoleDevice();
        InterruptRouter = interruptRouter;
        OsServices = osServices;
    }

    /// <summary>
    /// Stepping helper that executes the next instruction on <see cref="Cpu"/>.
    /// </summary>
    /// <returns>Outcome and state information of the executed step.</returns>
    public StepResult Step() => Cpu.StepInstruction();
}
