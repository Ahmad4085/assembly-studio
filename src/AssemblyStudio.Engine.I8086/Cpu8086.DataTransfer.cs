using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// Data transfer instructions (TASK-012, FR-036): MOV (all forms incl. segment registers and
/// moffs), XCHG, LEA/LDS/LES, XLAT, LAHF/SAHF, PUSH/POP (reg, seg, rm), PUSHF/POPF.
/// Semantics follow the Intel 8086 reference manual, with the oracle-confirmed 8086 quirks:
/// PUSH SP pushes the decremented SP, POP SP loads SP with the popped value, PUSHF pushes FLAGS
/// verbatim, POPF forces the reserved bits to the 8086 canonical form, and the 8C/8E segment
/// register field and the 8F/C6/C7 reg fields alias to their /0 behaviour. No flags change on any
/// instruction here except SAHF/POPF, which define every flag bit (no undefined flags, ADR-0003 §3).
/// </summary>
public sealed partial class Cpu8086
{
    // ---- instruction-stream fetches (16-bit wrap; CS never changes in these instructions) ----

    private byte Fetch8(ref ushort cursor)
    {
        byte value = memory.Read8(_state.CS, cursor);
        cursor++;
        return value;
    }

    private ushort Fetch16(ref ushort cursor)
    {
        byte low = Fetch8(ref cursor);
        byte high = Fetch8(ref cursor);
        return (ushort)(low | (high << 8));
    }

    /// <summary>
    /// Decodes the ModR/M byte (and its displacement) at the cursor, advancing the cursor past it.
    /// </summary>
    private ModRmInfo ReadModRm(ref ushort cursor, SegReg? segmentOverride)
    {
        byte modRm = Fetch8(ref cursor);
        int mod = modRm >> 6;

        Span<byte> displacement = stackalloc byte[2];
        int count = 0;
        if (mod == 1)
        {
            displacement[0] = Fetch8(ref cursor);
            count = 1;
        }
        else if (mod == 2 || (mod == 0 && (modRm & 7) == 6))
        {
            displacement[0] = Fetch8(ref cursor);
            displacement[1] = Fetch8(ref cursor);
            count = 2;
        }

        return Decoder.DecodeModRm(modRm, _state, segmentOverride, displacement[..count]);
    }

    // ---- register access -----------------------------------------------------------------------

    private static ushort GetReg16(in CpuState s, int index) => index switch
    {
        0 => s.AX, 1 => s.CX, 2 => s.DX, 3 => s.BX,
        4 => s.SP, 5 => s.BP, 6 => s.SI, _ => s.DI,
    };

    private static CpuState WithReg16(in CpuState s, int index, ushort value) => index switch
    {
        0 => s with { AX = value }, 1 => s with { CX = value }, 2 => s with { DX = value }, 3 => s with { BX = value },
        4 => s with { SP = value }, 5 => s with { BP = value }, 6 => s with { SI = value }, _ => s with { DI = value },
    };

    private static byte GetReg8(in CpuState s, int index) => index switch
    {
        0 => (byte)s.AX, 1 => (byte)s.CX, 2 => (byte)s.DX, 3 => (byte)s.BX,
        4 => (byte)(s.AX >> 8), 5 => (byte)(s.CX >> 8), 6 => (byte)(s.DX >> 8), _ => (byte)(s.BX >> 8),
    };

    private static CpuState WithReg8(in CpuState s, int index, byte value) => index switch
    {
        0 => s with { AX = (ushort)((s.AX & 0xFF00) | value) },
        1 => s with { CX = (ushort)((s.CX & 0xFF00) | value) },
        2 => s with { DX = (ushort)((s.DX & 0xFF00) | value) },
        3 => s with { BX = (ushort)((s.BX & 0xFF00) | value) },
        4 => s with { AX = (ushort)((s.AX & 0x00FF) | (value << 8)) },
        5 => s with { CX = (ushort)((s.CX & 0x00FF) | (value << 8)) },
        6 => s with { DX = (ushort)((s.DX & 0x00FF) | (value << 8)) },
        _ => s with { BX = (ushort)((s.BX & 0x00FF) | (value << 8)) },
    };

    // ---- memory / rm access ----------------------------------------------------------------------

    private byte ReadMem8(in ModRmInfo m) => memory.Read8(m.EaSegment, m.EaOffset);

    private ushort ReadMem16(in ModRmInfo m) => memory.Read16(m.EaSegment, m.EaOffset);

    private byte ReadRm8(in CpuState s, in ModRmInfo m) => m.IsMemory ? ReadMem8(m) : GetReg8(s, m.Rm);

    private ushort ReadRm16(in CpuState s, in ModRmInfo m) => m.IsMemory ? ReadMem16(m) : GetReg16(s, m.Rm);

    private void WriteRm8(ref CpuState s, in ModRmInfo m, byte value)
    {
        if (m.IsMemory)
        {
            memory.Write8(m.EaSegment, m.EaOffset, value);
        }
        else
        {
            s = WithReg8(s, m.Rm, value);
        }
    }

    private void WriteRm16(ref CpuState s, in ModRmInfo m, ushort value)
    {
        if (m.IsMemory)
        {
            memory.Write16(m.EaSegment, m.EaOffset, value);
        }
        else
        {
            s = WithReg16(s, m.Rm, value);
        }
    }

    // ---- stack -----------------------------------------------------------------------------------

    private void Push(ref CpuState s, ushort value)
    {
        s = s with { SP = (ushort)(s.SP - 2) };
        memory.Write16(s.SS, s.SP, value);
    }

    private ushort Pop(ref CpuState s)
    {
        ushort value = memory.Read16(s.SS, s.SP);
        s = s with { SP = (ushort)(s.SP + 2) };
        return value;
    }

    /// <summary>Value of a segment register, in ES/CS/SS/DS order (the 8086 Sreg encoding).</summary>
    private static ushort SegValue(in CpuState s, SegReg seg) => seg switch
    {
        SegReg.ES => s.ES,
        SegReg.CS => s.CS,
        SegReg.SS => s.SS,
        _ => s.DS,
    };

    /// <summary>Sets a segment register by ES/CS/SS/DS order.</summary>
    private static CpuState WithSeg(in CpuState s, SegReg seg, ushort value) => seg switch
    {
        SegReg.ES => s with { ES = value },
        SegReg.CS => s with { CS = value },
        SegReg.SS => s with { SS = value },
        _ => s with { DS = value },
    };

    /// <summary>The segment value a data access uses: the override, or the DS default.</summary>
    private static ushort DataSegment(in CpuState s, SegReg? overrideSeg) =>
        overrideSeg.HasValue ? SegValue(s, overrideSeg.Value) : s.DS;

    private StepResult Executed(ushort endIp)
    {
        // The step is complete: IP advances past the instruction. The result's instruction address
        // (start, including prefixes) is normalized by StepInstruction from the pre-step state.
        _state = _state with { IP = endIp };
        return new StepResult(StepOutcome.Executed, 0, endIp, HaltReason.None, FlagBit.None);
    }

    // ---- MOV r/m,reg and reg,r/m -----------------------------------------------------------------

    private StepResult ExecMovRm8FromR8(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        WriteRm8(ref s, m, GetReg8(s, m.Reg));
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecMovRm16FromR16(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        WriteRm16(ref s, m, GetReg16(s, m.Reg));
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecMovR8FromRm8(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        s = WithReg8(s, m.Reg, ReadRm8(s, m));
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecMovR16FromRm16(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        s = WithReg16(s, m.Reg, ReadRm16(s, m));
        _state = s;
        return Executed(cursor);
    }

    // ---- MOV with segment registers ---------------------------------------------------------------
    // The 8086 aliases the Sreg field: only the low 2 bits are decoded (ES=0, CS=1, SS=2, DS=3).

    private StepResult ExecMovRm16FromSreg(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        WriteRm16(ref s, m, SegValue(s, (SegReg)(m.Reg & 3)));
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecMovSregFromRm16(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        s = WithSeg(s, (SegReg)(m.Reg & 3), ReadRm16(s, m));
        _state = s;
        return Executed(cursor);
    }

    // ---- MOV with moffs (A0–A3) --------------------------------------------------------------------

    private StepResult ExecMovMoffs(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ushort offset = Fetch16(ref cursor);
        var s = _state;
        ushort segment = DataSegment(s, op.SegmentOverride);

        switch (op.Opcode)
        {
            case 0xA0: s = WithReg8(s, 0, memory.Read8(segment, offset)); break;
            case 0xA1: s = s with { AX = memory.Read16(segment, offset) }; break;
            case 0xA2: memory.Write8(segment, offset, (byte)s.AX); break;
            default: memory.Write16(segment, offset, s.AX); break;
        }

        _state = s;
        return Executed(cursor);
    }

    // ---- MOV reg, imm -------------------------------------------------------------------------------

    private StepResult ExecMovReg8FromImm8(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        byte imm = Fetch8(ref cursor);
        _state = WithReg8(_state, op.Opcode & 7, imm);
        return Executed(cursor);
    }

    private StepResult ExecMovReg16FromImm16(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ushort imm = Fetch16(ref cursor);
        _state = WithReg16(_state, op.Opcode & 7, imm);
        return Executed(cursor);
    }

    // ---- MOV r/m, imm (C6/C7: every reg field behaves as /0) -----------------------------------------

    private StepResult ExecMovRm8FromImm8(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        byte imm = Fetch8(ref cursor);
        var s = _state;
        WriteRm8(ref s, m, imm);
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecMovRm16FromImm16(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        ushort imm = Fetch16(ref cursor);
        var s = _state;
        WriteRm16(ref s, m, imm);
        _state = s;
        return Executed(cursor);
    }

    // ---- XCHG ---------------------------------------------------------------------------------------

    private StepResult ExecXchgRm8R8(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        byte left = ReadRm8(s, m);
        byte right = GetReg8(s, m.Reg);
        s = WithReg8(s, m.Reg, left);
        WriteRm8(ref s, m, right);
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecXchgRm16R16(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        var s = _state;
        ushort left = ReadRm16(s, m);
        ushort right = GetReg16(s, m.Reg);
        s = WithReg16(s, m.Reg, left);
        WriteRm16(ref s, m, right);
        _state = s;
        return Executed(cursor);
    }

    private StepResult ExecXchgAxReg(PrefixInfo op)
    {
        int index = op.Opcode & 7;
        var s = _state;
        ushort ax = s.AX;
        s = WithReg16(s, index, ax);
        _state = WithReg16(s, 0, GetReg16(_state, index));
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    // ---- LEA / LDS / LES (memory forms only; mod=11 is undefined on the 8086) -------------------------

    private StepResult ExecLea(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        if (m.Mod == 3)
        {
            return HaltUnsupported();
        }

        _state = WithReg16(_state, m.Reg, m.EaOffset);
        return Executed(cursor);
    }

    private StepResult ExecLesOrLds(PrefixInfo op, bool isLes)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        if (m.Mod == 3)
        {
            return HaltUnsupported();
        }

        var s = _state;
        s = WithReg16(s, m.Reg, ReadMem16(m));
        s = isLes
            ? s with { ES = memory.Read16(m.EaSegment, (ushort)(m.EaOffset + 2)) }
            : s with { DS = memory.Read16(m.EaSegment, (ushort)(m.EaOffset + 2)) };
        _state = s;
        return Executed(cursor);
    }

    // ---- XLAT -----------------------------------------------------------------------------------------

    private StepResult ExecXlat(PrefixInfo op)
    {
        var s = _state;
        ushort offset = (ushort)(s.BX + (s.AX & 0xFF));
        ushort segment = DataSegment(s, op.SegmentOverride);
        _state = WithReg8(s, 0, memory.Read8(segment, offset));
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    // ---- LAHF / SAHF ------------------------------------------------------------------------------------

    private StepResult ExecSahf(PrefixInfo op)
    {
        // SF:ZF:0:AF:0:PF:1:CF from AH bits 7:6:5:4:3:2:1:0 (8086 canonical bit values preserved).
        byte ah = (byte)(_state.AX >> 8);
        _state = _state with { Flags = (ushort)((_state.Flags & 0xFF00) | (ah & 0xD5) | 0x02) };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecLahf(PrefixInfo op)
    {
        byte low = (byte)_state.Flags;
        _state = WithReg8(_state, 4, low);
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    // ---- PUSH / POP registers (50–5F; POP SP loads SP with the popped value) ------------------------------

    private StepResult ExecPushReg(PrefixInfo op)
    {
        int index = op.Opcode & 7;
        var s = _state;
        // 8086 PUSH SP pushes the NEW (decremented) SP value.
        ushort value = index == 4 ? (ushort)(s.SP - 2) : GetReg16(s, index);
        Push(ref s, value);
        _state = s;
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecPopReg(PrefixInfo op)
    {
        int index = op.Opcode & 7;
        var s = _state;
        ushort value = Pop(ref s);
        _state = index == 4 ? s with { SP = value } : WithReg16(s, index, value);
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    // ---- PUSH / POP segment registers ------------------------------------------------------------------

    private StepResult ExecPushSeg(PrefixInfo op, SegReg seg)
    {
        var s = _state;
        Push(ref s, SegValue(s, seg));
        _state = s;
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecPopSeg(PrefixInfo op, SegReg seg)
    {
        var s = _state;
        ushort value = Pop(ref s);
        _state = WithSeg(s, seg, value);
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    // ---- POP r/m16 (8F: every reg field behaves as /0) ------------------------------------------------------

    private StepResult ExecPopRm16(PrefixInfo op)
    {
        var s = _state;
        ushort value = Pop(ref s);
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        WriteRm16(ref s, m, value);
        _state = s;
        return Executed(cursor);
    }

    // ---- PUSH/POP r/m16 via the FF group (/6 PUSH; /0 /1 INC/DEC live in Cpu8086.Arithmetic.cs) -------

    private StepResult ExecPushf(PrefixInfo op)
    {
        var s = _state;
        Push(ref s, s.Flags); // 8086 PUSHF pushes FLAGS verbatim (oracle-confirmed).
        _state = s;
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecPopf(PrefixInfo op)
    {
        var s = _state;
        ushort popped = Pop(ref s);
        // 8086 POPF: bits 12–15 and bit 1 read back as 1, bits 3 and 5 as 0 (oracle-confirmed).
        s = s with { Flags = (ushort)(((popped | 0xF002) & ~0x0028) & 0xFFFF) };
        _state = s;
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult HaltUnsupported() =>
        new(StepOutcome.Halted, _state.CS, _state.IP, HaltReason.UnsupportedInstruction, FlagBit.None);
}
