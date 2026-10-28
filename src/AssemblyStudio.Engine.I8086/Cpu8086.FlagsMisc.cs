using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// Flag manipulation, processor control, and miscellaneous instructions (TASK-018, FR-036, FR-037):
/// - Flag operations: CLC (F8), STC (F9), CMC (F5), CLD (FC), STD (FD), CLI (FA), STI (FB)
/// - Processor control: HLT (F4) halts execution (<see cref="HaltReason.Halt"/>)
/// - Synchronization: WAIT / FWAIT (9B) is a no-op on 8086 without an 8087 coprocessor (ADR-0003 §4)
/// - LOCK (F0) is captured and ignored as a prefix in <see cref="Decoder.ParsePrefixes"/>
/// - Coprocessor ESC 0..7 (D8..DF) and Port I/O (E4..E7, EC..EF) are unsupported in MVP and halt (FR-037)
/// </summary>
public sealed partial class Cpu8086
{
    private StepResult ExecClc(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags & ~(ushort)FlagBit.CF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecStc(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags | (ushort)FlagBit.CF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecCmc(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags ^ (ushort)FlagBit.CF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecCld(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags & ~(ushort)FlagBit.DF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecStd(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags | (ushort)FlagBit.DF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecCli(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags & ~(ushort)FlagBit.IF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecSti(PrefixInfo op)
    {
        _state = _state with { Flags = (ushort)(_state.Flags | (ushort)FlagBit.IF) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecWait(PrefixInfo op)
    {
        // 8086 with no 8087 coprocessor: WAIT / FWAIT is a no-op (ADR-0003 §4).
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecHlt(PrefixInfo op)
    {
        ushort endIp = (ushort)(op.OpcodeIp + 1);
        _state = _state with { IP = endIp };
        return new StepResult(StepOutcome.Halted, 0, 0, HaltReason.Halt, FlagBit.None);
    }
}
