using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Console;

namespace AssemblyStudio.Runtime.Interrupts;

/// <summary>
/// Routes interrupts between the CPU execution engine, the Interrupt Vector Table (IVT),
/// the OS services layer, and the runtime error policy (ADR-0004 §3).
/// </summary>
public sealed class InterruptRouter : IInterruptRouter
{
    /// <summary>
    /// The emulated real-mode memory.
    /// </summary>
    public IMemory Memory { get; }

    /// <summary>
    /// The OS services layer (e.g. DOS INT 20h/21h), or <see langword="null"/> when no OS services layer is attached.
    /// </summary>
    public IOsServices? OsServices { get; }

    /// <summary>
    /// The host machine instance, or <see langword="null"/> if not yet attached.
    /// </summary>
    public IMachine? Machine { get; set; }

    /// <summary>
    /// Structured diagnostic information from the most recent halt (CPU exception or unsupported service),
    /// or <see langword="null"/> if no halt has occurred.
    /// </summary>
    public RuntimeHaltInfo? LastHalt { get; private set; }

    /// <summary>
    /// Creates a new <see cref="InterruptRouter"/> instance.
    /// </summary>
    /// <param name="memory">The emulated real-mode memory (required).</param>
    /// <param name="osServices">Optional OS services layer.</param>
    /// <param name="machine">Optional machine instance.</param>
    /// <param name="initializeIvt">Whether to populate uninitialized IVT vectors with default stubs (<see langword="true"/> by default).</param>
    public InterruptRouter(
        IMemory memory,
        IOsServices? osServices = null,
        IMachine? machine = null,
        bool initializeIvt = true)
    {
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        OsServices = osServices;
        Machine = machine;

        if (initializeIvt)
        {
            IvtStubs.Initialize(Memory);
        }
    }

    /// <summary>
    /// Clears the recorded <see cref="LastHalt"/> diagnostic.
    /// </summary>
    public void ClearLastHalt() => LastHalt = null;

    /// <inheritdoc />
    public StepResult? Intercept(byte vector, CpuState state)
    {
        // 1. Program-installed vector check:
        // If the vector was modified by the guest program, return null to let the CPU
        // vector through the IVT normally using real 8086 semantics (ADR-0004 §3).
        if (!IvtStubs.IsDefaultVector(Memory, vector))
        {
            return null;
        }

        // 2. CPU Exceptions (INT 0..4) with default vector:
        // Unhandled exceptions halt execution with a runtime error naming the condition and address (FR-038).
        if (vector <= 4)
        {
            var exceptionHalt = RuntimeErrorPolicy.CreateCpuExceptionHalt(vector, state);
            LastHalt = exceptionHalt;
            return new StepResult(StepOutcome.Halted, state.CS, state.IP, exceptionHalt.HaltReason, FlagBit.None) { Message = exceptionHalt.ToMessage() };
        }

        // 3. OS Services delegation (e.g. INT 20h, INT 21h):
        if (OsServices is not null)
        {
            IMachine machineContext = Machine ?? new MachineFallback(Memory);
            StepResult? serviceResult = OsServices.HandleService(vector, state, machineContext);

            if (serviceResult is not null)
            {
                if (serviceResult.Value.Outcome == StepOutcome.Halted &&
                    serviceResult.Value.HaltReason == HaltReason.UnsupportedService)
                {
                    LastHalt = RuntimeErrorPolicy.CreateUnsupportedServiceHalt(vector, state);
                    return serviceResult.Value with { Message = LastHalt.ToMessage() };
                }

                return serviceResult.Value;
            }
        }

        // 4. Any software interrupt or function outside the supported set halts with UnsupportedService (FR-040, NFR-013).
        var unsupportedHalt = RuntimeErrorPolicy.CreateUnsupportedServiceHalt(vector, state);
        LastHalt = unsupportedHalt;
        return new StepResult(StepOutcome.Halted, state.CS, state.IP, unsupportedHalt.HaltReason, FlagBit.None) { Message = unsupportedHalt.ToMessage() };
    }

    private sealed class MachineFallback(IMemory memory) : IMachine
    {
        public IMemory Memory { get; } = memory;
        public IConsoleDevice Console { get; } = new NullConsoleDevice();
        ICpu IMachine.Cpu => throw new InvalidOperationException("Machine is not attached to InterruptRouter.");
        IInterruptRouter? IMachine.InterruptRouter => null;
    }
}
