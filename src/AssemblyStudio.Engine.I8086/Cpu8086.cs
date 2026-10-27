using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// Managed 8086 core implementing <see cref="ICpu"/> (ADR-0003). In-process, headless, one
/// instruction per <see cref="StepInstruction"/>. Decode is pure (state unchanged on halt,
/// FR-037); memory goes through <see cref="IMemory"/> (1 MiB, journal); interrupts are intercepted
/// by an optional <see cref="IInterruptRouter"/> before vectoring (ADR-0003 §2, ADR-0004 §3).
/// No UI, DOS or file knowledge (architecture §2 C04). Not thread-safe: the debug-session worker
/// thread is the only caller.
/// </summary>
/// <param name="memory">The emulated memory the CPU fetches from and addresses.</param>
/// <param name="interruptRouter">Interrupt interception callback, or <see langword="null"/> to
/// vector every interrupt through the IVT.</param>
public sealed class Cpu8086(IMemory memory, IInterruptRouter? interruptRouter = null) : ICpu
{
    private CpuState _state;

    /// <inheritdoc />
    public CpuState State => _state;

    /// <inheritdoc />
    public void SetState(CpuState state) => _state = state;

    /// <inheritdoc />
    public StepResult StepInstruction()
    {
        CpuState before = _state;
        PrefixInfo opcode = Decoder.ParsePrefixes(_state, memory);

        switch (opcode.Opcode)
        {
            case 0xCD:
                return ExecuteIntImm8(opcode);
            default:
                // Every other opcode family arrives with TASK-012..018 (ADR-0003 §4). Halt with the
                // state inspectable at the instruction: CS:IP restored, nothing modified (FR-037).
                _state = before;
                return new StepResult(
                    StepOutcome.Halted, before.CS, before.IP,
                    HaltReason.UnsupportedInstruction, FlagBit.None);
        }
    }

    /// <summary>
    /// INT imm8 (0xCD): the interception contract (ADR-0003 §2, ADR-0004 §3). The router sees the
    /// vector and the state with CS:IP still on the INT instruction. When it returns a result, that
    /// result is the step (a service, a runtime error, an unsupported service, or
    /// <see cref="StepOutcome.WaitingForInput"/> with CS:IP left on the INT). Otherwise the CPU
    /// vectors through the IVT with real 8086 semantics.
    /// </summary>
    private StepResult ExecuteIntImm8(in PrefixInfo opcode)
    {
        CpuState atInt = _state;

        // The interrupt number follows the opcode byte.
        ushort immIp = (ushort)(opcode.OpcodeIp + 1);
        byte vector = memory.Read8(atInt.CS, immIp);

        StepResult? handled = interruptRouter?.Intercept(vector, atInt);
        if (handled is not null)
        {
            return handled.Value;
        }

        // Vector normally: push FLAGS, CS, return address (after the full instruction), then load
        // CS:IP from the IVT and clear IF/TF (ADR-0003 §2). SP wraps at 16 bits.
        ushort returnIp = (ushort)(immIp + 1);
        Push(atInt.Flags);
        Push(atInt.CS);
        Push(returnIp);

        _state = _state with
        {
            IP = memory.Read16(0, (ushort)(vector * 2)),
            CS = memory.Read16(0, (ushort)((vector * 2) + 2)),
        };
        _state = _state with
        {
            Flags = (ushort)(_state.Flags & ~(ushort)(FlagBit.IF | FlagBit.TF)),
        };

        return new StepResult(StepOutcome.Executed, opcode.OpcodeIp, opcode.OpcodeIp, HaltReason.None, FlagBit.None);
    }

    private void Push(ushort value)
    {
        _state = _state with { SP = (ushort)(_state.SP - 2) };
        memory.Write16(_state.SS, _state.SP, value);
    }
}
