using System.Numerics;
using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// Arithmetic instructions (TASK-013, FR-036/FR-048): ADD/ADC/SUB/SBB/CMP in rm/r, acc-imm and
/// immediate-group forms, INC/DEC (register and r/m), NEG — exact flags per the Intel 8086 manual:
/// CF carry/borrow out; OF signed overflow; AF carry/borrow out of bit 3; SF the top bit; ZF on a
/// zero result; PF even parity of the low 8 bits. INC/DEC leave CF unchanged; NEG sets CF unless
/// the operand is 0 and overflows only on the most negative value; CMP computes SUB flags and
/// discards the result. TF/IF/DF and the reserved bits pass through; every flag an instruction
/// here touches is defined (the undefined-flags report stays <see cref="FlagBit.None"/>,
/// ADR-0003 §3). The OR/AND/XOR arms appear here only because the 80–83 immediate groups select
/// them by reg field and are in TASK-013 scope; the rm/r logic families are TASK-014.
/// </summary>
public sealed partial class Cpu8086
{
    /// <summary>Operation selector: the reg field of the 80/81/82/83 immediate groups.</summary>
    private enum ArithKind : byte
    {
        Add = 0, Or = 1, Adc = 2, Sbb = 3, And = 4, Sub = 5, Xor = 6, Cmp = 7,
    }

    private static ushort SetFlag(ushort flags, FlagBit bit, bool value) =>
        value ? (ushort)(flags | (ushort)bit) : (ushort)(flags & ~(ushort)bit);

    /// <summary>Computes result = left OP right with exact 8086 flags; returns the new FLAGS word
    /// and yields the (width-masked) result for the caller to store (CMP callers discard it).</summary>
    private static ushort Arith(in CpuState s, ArithKind kind, bool wide, ushort left, ushort right, out ushort result)
    {
        ushort flags = s.Flags;
        ushort sign = wide ? (ushort)0x8000 : (ushort)0x80;
        ushort mask = wide ? (ushort)0xFFFF : (ushort)0xFF;
        bool carryIn = kind is ArithKind.Adc or ArithKind.Sbb && (s.Flags & (ushort)FlagBit.CF) != 0;

        switch (kind)
        {
            case ArithKind.Add or ArithKind.Adc:
            {
                uint sum = (uint)left + right + (carryIn ? 1u : 0u);
                result = (ushort)(sum & mask);
                flags = SetFlag(flags, FlagBit.CF, sum > mask);
                flags = SetFlag(flags, FlagBit.AF, ((left ^ right ^ result) & 0x10) != 0);
                flags = SetFlag(flags, FlagBit.OF, (~(left ^ right) & (left ^ result) & sign) != 0);
                break;
            }
            case ArithKind.Sub or ArithKind.Sbb or ArithKind.Cmp:
            {
                uint borrow = carryIn ? 1u : 0u;
                result = (ushort)(((uint)left - right - borrow) & mask);
                flags = SetFlag(flags, FlagBit.CF, (uint)left < right + borrow);
                flags = SetFlag(flags, FlagBit.AF, ((left ^ right ^ result) & 0x10) != 0);
                flags = SetFlag(flags, FlagBit.OF, ((left ^ right) & (left ^ result) & sign) != 0);
                break;
            }
            default:
            {
                // Or/And/Xor: CF=OF=0; AF is undefined on the 8086 and left untouched.
                result = (ushort)((kind switch
                {
                    ArithKind.Or => left | right,
                    ArithKind.And => left & right,
                    _ => left ^ right,
                }) & mask);
                flags = SetFlag(flags, FlagBit.CF, false);
                flags = SetFlag(flags, FlagBit.OF, false);
                break;
            }
        }

        flags = SetFlag(flags, FlagBit.SF, (result & sign) != 0);
        flags = SetFlag(flags, FlagBit.ZF, result == 0);
        flags = SetFlag(flags, FlagBit.PF, BitOperations.PopCount((uint)(byte)result) % 2 == 0);
        return flags;
    }

    /// <summary>Sets every INC/DEC flag except CF, which passes through untouched.</summary>
    private static ushort IncDecFlags(ushort flags, bool wide, ushort value, ushort result, bool isInc)
    {
        ushort sign = wide ? (ushort)0x8000 : (ushort)0x80;
        flags = SetFlag(flags, FlagBit.AF, ((value ^ result) & 0x10) != 0);
        flags = SetFlag(flags, FlagBit.OF, result == (isInc ? sign : (ushort)(sign - 1)));
        flags = SetFlag(flags, FlagBit.SF, (result & sign) != 0);
        flags = SetFlag(flags, FlagBit.ZF, result == 0);
        flags = SetFlag(flags, FlagBit.PF, BitOperations.PopCount((uint)(byte)result) % 2 == 0);
        return flags;
    }

    // ---- ADD/ADC/SUB/SBB/CMP between r/m and reg (bases 00/10/18/28/38, arms 0-3) ------------------

    private StepResult ExecArithRmR(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        bool wide = (op.Opcode & 1) != 0;
        bool toReg = (op.Opcode & 2) != 0; // arms 2/3 store into the reg operand
        var kind = (ArithKind)(op.Opcode >> 3);
        var s = _state;
        ushort left = wide ? (toReg ? GetReg16(s, m.Reg) : ReadRm16(s, m))
                           : (toReg ? GetReg8(s, m.Reg) : ReadRm8(s, m));
        ushort right = wide ? (toReg ? ReadRm16(s, m) : GetReg16(s, m.Reg))
                            : (toReg ? ReadRm8(s, m) : GetReg8(s, m.Reg));
        ushort flags = Arith(s, kind, wide, left, right, out ushort result);
        s = s with { Flags = flags };
        if (kind != ArithKind.Cmp)
        {
            if (toReg)
            {
                s = wide ? WithReg16(s, m.Reg, result) : WithReg8(s, m.Reg, (byte)result);
            }
            else if (wide)
            {
                WriteRm16(ref s, m, result);
            }
            else
            {
                WriteRm8(ref s, m, (byte)result);
            }
        }
        _state = s;
        return Executed(cursor);
    }

    // ---- ADD/ADC/SUB/SBB/CMP with immediate against AL/AX (arms 4/5 of the same families) ------------

    private StepResult ExecArithAccImm(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        bool wide = (op.Opcode & 1) != 0;
        var s = _state;
        ushort left = wide ? s.AX : (ushort)(s.AX & 0xFF);
        ushort right = wide ? Fetch16(ref cursor) : Fetch8(ref cursor);
        ushort flags = Arith(s, (ArithKind)(op.Opcode >> 3), wide, left, right, out ushort result);
        if ((ArithKind)(op.Opcode >> 3) != ArithKind.Cmp)
        {
            s = s with
            {
                Flags = flags,
                AX = wide ? result : (ushort)((s.AX & 0xFF00) | result),
            };
        }
        else
        {
            s = s with { Flags = flags };
        }
        _state = s;
        return Executed(cursor);
    }

    // ---- Immediate groups: 80/82 (rm8, imm8; 82 is the undocumented alias of 80),
    // 81 (rm16, imm16), 83 (rm16, sign-extended imm8). The reg field selects the operation. ----------

    private StepResult ExecArithGroupImm(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        bool wide = op.Opcode is 0x81 or 0x83;
        ushort right;
        if (op.Opcode == 0x83)
        {
            right = (ushort)unchecked((sbyte)Fetch8(ref cursor));
        }
        else
        {
            right = wide ? Fetch16(ref cursor) : Fetch8(ref cursor);
        }

        var s = _state;
        ushort left = wide ? ReadRm16(s, m) : ReadRm8(s, m);
        var kind = (ArithKind)m.Reg;
        ushort flags = Arith(s, kind, wide, left, right, out ushort result);
        s = s with { Flags = flags };
        if (kind != ArithKind.Cmp)
        {
            if (wide)
            {
                WriteRm16(ref s, m, result);
            }
            else
            {
                WriteRm8(ref s, m, (byte)result);
            }
        }
        _state = s;
        return Executed(cursor);
    }

    // ---- INC/DEC r16 (40-4F) --------------------------------------------------------------------------

    private StepResult ExecIncDecReg16(PrefixInfo op)
    {
        int index = op.Opcode & 7;
        bool isInc = (op.Opcode & 8) == 0;
        var s = _state;
        ushort value = GetReg16(s, index);
        ushort result = isInc ? (ushort)(value + 1) : (ushort)(value - 1);
        s = WithReg16(s, index, result);
        s = s with { Flags = IncDecFlags(s.Flags, wide: true, value, result, isInc) };
        _state = s;
        return Executed((ushort)(op.OpcodeIp + 1));
    }
    /// <summary>INC/DEC r/m core: ModR/M already decoded (reg field 0 = INC, 1 = DEC).</summary>
    private StepResult ExecIncDecRmCore(in ModRmInfo m, ushort cursor, bool wide, bool isInc)
    {
        var s = _state;
        ushort value = wide ? ReadRm16(s, m) : ReadRm8(s, m);
        ushort mask = wide ? (ushort)0xFFFF : (ushort)0xFF;
        ushort result = isInc ? (ushort)((value + 1) & mask) : (ushort)((value - 1) & mask);
        if (wide)
        {
            WriteRm16(ref s, m, result);
        }
        else
        {
            WriteRm8(ref s, m, (byte)result);
        }
        s = s with { Flags = IncDecFlags(s.Flags, wide, value, result, isInc) };
        _state = s;
        return Executed(cursor);
    }

    // ---- FE group: only /0 (INC rm8) and /1 (DEC rm8) are defined on the 8086 ------------------------

    private StepResult ExecFeGroup(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        return m.Reg switch
        {
            0 => ExecIncDecRmCore(m, cursor, wide: false, isInc: true),
            1 => ExecIncDecRmCore(m, cursor, wide: false, isInc: false),
            _ => HaltUnsupported(),
        };
    }

    // ---- FF group: /0 INC rm16, /1 DEC rm16, /6 PUSH rm16 (TASK-012); the rest halt unsupported -------

    private StepResult ExecFfGroup(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        switch (m.Reg)
        {
            case 0: return ExecIncDecRmCore(m, cursor, wide: true, isInc: true);
            case 1: return ExecIncDecRmCore(m, cursor, wide: true, isInc: false);
            case 6:
                var s = _state;
                // The 8086 PUSH SP quirk applies to the register form of FF /6 as well.
                ushort value = ReadRm16(s, m);
                if (!m.IsMemory && m.Rm == 4)
                {
                    value = (ushort)(s.SP - 2);
                }
                Push(ref s, value);
                _state = s;
                return Executed(cursor);
            default: return HaltUnsupported();
        }
    }

    // ---- NEG (F6/F7 /3): result = 0 - r/m ---------------------------------------------------------------

    private StepResult ExecNeg(PrefixInfo op, bool wide)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        if (m.Reg != 3)
        {
            return HaltUnsupported();
        }

        var s = _state;
        ushort value = wide ? ReadRm16(s, m) : ReadRm8(s, m);
        ushort sign = wide ? (ushort)0x8000 : (ushort)0x80;
        ushort result = (ushort)((0 - value) & (wide ? 0xFFFF : 0xFF));
        if (wide)
        {
            WriteRm16(ref s, m, result);
        }
        else
        {
            WriteRm8(ref s, m, (byte)result);
        }

        ushort flags = s.Flags;
        flags = SetFlag(flags, FlagBit.CF, value != 0);
        flags = SetFlag(flags, FlagBit.OF, value == sign);
        flags = SetFlag(flags, FlagBit.AF, (value & 0xF) != 0);
        flags = SetFlag(flags, FlagBit.SF, (result & sign) != 0);
        flags = SetFlag(flags, FlagBit.ZF, result == 0);
        flags = SetFlag(flags, FlagBit.PF, BitOperations.PopCount((uint)(byte)result) % 2 == 0);
        s = s with { Flags = flags };
        _state = s;
        return Executed(cursor);
    }
}
