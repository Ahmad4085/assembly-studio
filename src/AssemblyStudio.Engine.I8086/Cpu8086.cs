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
public sealed partial class Cpu8086(IMemory memory, IInterruptRouter? interruptRouter = null) : ICpu
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

        // Handlers return null for unsupported or undefined forms: the state is then restored and
        // the step halts with the instruction inspectable (FR-037). Data transfer: TASK-012
        // (Cpu8086.DataTransfer.cs); arithmetic: TASK-013 (Cpu8086.Arithmetic.cs); the other
        // families arrive with TASK-014..018 (ADR-0003 §4).
        StepResult? result = opcode.Opcode switch
        {
            0xCD => ExecuteIntImm8(opcode),
            0x00 or 0x01 or 0x02 or 0x03 => ExecArithRmR(opcode),
            0x04 or 0x05 => ExecArithAccImm(opcode),
            >= 0x10 and <= 0x13 => ExecArithRmR(opcode),
            0x14 or 0x15 => ExecArithAccImm(opcode),
            >= 0x18 and <= 0x1B => ExecArithRmR(opcode),
            0x1C or 0x1D => ExecArithAccImm(opcode),
            >= 0x28 and <= 0x2B => ExecArithRmR(opcode),
            0x2C or 0x2D => ExecArithAccImm(opcode),
            >= 0x38 and <= 0x3B => ExecArithRmR(opcode),
            0x3C or 0x3D => ExecArithAccImm(opcode),
            >= 0x40 and <= 0x4F => ExecIncDecReg16(opcode),
            0x80 or 0x81 or 0x82 or 0x83 => ExecArithGroupImm(opcode),
            0xF6 => ExecNeg(opcode, wide: false),
            0xF7 => ExecNeg(opcode, wide: true),
            0x88 => ExecMovRm8FromR8(opcode),
            0x89 => ExecMovRm16FromR16(opcode),
            0x8A => ExecMovR8FromRm8(opcode),
            0x8B => ExecMovR16FromRm16(opcode),
            0x8C => ExecMovRm16FromSreg(opcode),
            0x8E => ExecMovSregFromRm16(opcode),
            >= 0xA0 and <= 0xA3 => ExecMovMoffs(opcode),
            >= 0xB0 and <= 0xB7 => ExecMovReg8FromImm8(opcode),
            >= 0xB8 and <= 0xBF => ExecMovReg16FromImm16(opcode),
            0xC6 => ExecMovRm8FromImm8(opcode),
            0xC7 => ExecMovRm16FromImm16(opcode),
            0x86 => ExecXchgRm8R8(opcode),
            0x87 => ExecXchgRm16R16(opcode),
            >= 0x90 and <= 0x97 => ExecXchgAxReg(opcode),
            0x8D => ExecLea(opcode),
            0xC4 => ExecLesOrLds(opcode, isLes: true),
            0xC5 => ExecLesOrLds(opcode, isLes: false),
            0xD7 => ExecXlat(opcode),
            0x9E => ExecSahf(opcode),
            0x9F => ExecLahf(opcode),
            >= 0x50 and <= 0x57 => ExecPushReg(opcode),
            >= 0x58 and <= 0x5F => ExecPopReg(opcode),
            0x06 => ExecPushSeg(opcode, SegReg.ES),
            0x07 => ExecPopSeg(opcode, SegReg.ES),
            0x0E => ExecPushSeg(opcode, SegReg.CS),
            0x16 => ExecPushSeg(opcode, SegReg.SS),
            0x17 => ExecPopSeg(opcode, SegReg.SS),
            0x1E => ExecPushSeg(opcode, SegReg.DS),
            0x1F => ExecPopSeg(opcode, SegReg.DS),
            0x8F => ExecPopRm16(opcode),
            0xFE => ExecFeGroup(opcode),
            0xFF => ExecFfGroup(opcode),
            0x9C => ExecPushf(opcode),
            0x9D => ExecPopf(opcode),
            _ => null,
        };

        if (result is not null)
        {
            // For executed instructions the report carries the instruction's own address: the
            // state as it stood at entry (CS:IP on the first byte, prefixes included).
            return result.Value.Outcome == StepOutcome.Executed
                ? result.Value with { InstructionCs = before.CS, InstructionIp = before.IP }
                : result.Value;
        }

        // Halt with the state inspectable at the instruction: CS:IP restored, nothing modified (FR-037).
        _state = before;
        return new StepResult(
            StepOutcome.Halted, before.CS, before.IP,
            HaltReason.UnsupportedInstruction, FlagBit.None);
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
