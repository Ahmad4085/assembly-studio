using System.Numerics;
using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// Logic, TEST/NOT and shift/rotate instructions (TASK-014, FR-036/FR-048): AND/OR/XOR in rm/r and
/// acc-imm forms (the 80-83 group arms arrived with TASK-013, and this file's rm/r families reuse
/// that shared <see cref="Arith"/> core), TEST (84/85, A8/A9, F6/F7 arm 0 — AND semantics, result
/// discarded), NOT (F6/F7 arm 2 — no flags), and the shifts/rotates D0-D3 with 8086 count
/// semantics: the count is not masked (masking arrived with the 186), a count of 0 changes
/// nothing at all, and OF is defined only for count 1 (left untouched otherwise — the oracle masks
/// exclude it). Rotates touch only CF (and OF for count 1); AF is undefined for every logic and
/// shift instruction and is left untouched. Arm 6 of D0-D3 is the undocumented 8086 SETMO,
/// implemented as the oracle records it: the operand becomes all ones (0xFF/0xFFFF), no-op at
/// count 0; the official metadata reports its flags undefined, but our values match the real
/// chip's raw behavior (SF=1, ZF=0, PF=1, CF=OF=AF=0).
/// </summary>
public sealed partial class Cpu8086
{
    /// <summary>TEST flags: the AND flag set (CF=OF=0, SF/ZF/PF from the result, AF untouched)
    /// with the result discarded.</summary>
    private static ushort TestFlags(in CpuState s, bool wide, ushort left, ushort right) =>
        Arith(s, ArithKind.And, wide, left, right, out _);

    // ---- TEST r/m,reg (84/85) ----------------------------------------------------------------------

    private StepResult ExecTestRmR(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        bool wide = op.Opcode == 0x85;
        var s = _state;
        ushort left = wide ? ReadRm16(s, m) : ReadRm8(s, m);
        ushort right = wide ? GetReg16(s, m.Reg) : GetReg8(s, m.Reg);
        _state = s with { Flags = TestFlags(s, wide, left, right) };
        return Executed(cursor);
    }

    // ---- TEST AL/AX, imm (A8/A9) ---------------------------------------------------------------------

    private StepResult ExecTestAccImm(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        bool wide = op.Opcode == 0xA9;
        var s = _state;
        ushort left = wide ? s.AX : (ushort)(s.AX & 0xFF);
        ushort right = wide ? Fetch16(ref cursor) : Fetch8(ref cursor);
        _state = s with { Flags = TestFlags(s, wide, left, right) };
        return Executed(cursor);
    }

    /// <summary>TEST r/m,imm core: ModR/M already decoded (arm 0; the undocumented /1 alias is not
    /// in the approved scope and halts).</summary>
    private StepResult ExecTestRmImmCore(in ModRmInfo m, ushort cursor, bool wide)
    {
        ushort right = wide ? Fetch16(ref cursor) : Fetch8(ref cursor);
        var s = _state;
        ushort left = wide ? ReadRm16(s, m) : ReadRm8(s, m);
        _state = s with { Flags = TestFlags(s, wide, left, right) };
        return Executed(cursor);
    }

    // ---- NOT (F6/F7 arm 2): no flags ----------------------------------------------------------------

    private StepResult ExecNotCore(in ModRmInfo m, ushort cursor, bool wide)
    {
        var s = _state;
        if (wide)
        {
            WriteRm16(ref s, m, (ushort)~ReadRm16(s, m));
        }
        else
        {
            WriteRm8(ref s, m, (byte)~ReadRm8(s, m));
        }
        _state = s;
        return Executed(cursor);
    }

    /// <summary>F6/F7 group dispatcher: 0 = TEST rm,imm; 2 = NOT; 3 = NEG (Cpu8086.Arithmetic.cs);
    /// 4-7 = MUL/IMUL/DIV/IDIV (Cpu8086.MulDiv.cs; a REP prefix negates the IDIV quotient). The
    /// undocumented /1 TEST alias is not in the approved scope and halts.</summary>
    private StepResult ExecF6F7Group(PrefixInfo op, bool wide)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        return m.Reg switch
        {
            0 => ExecTestRmImmCore(m, cursor, wide),
            2 => ExecNotCore(m, cursor, wide),
            3 => ExecNegCore(m, cursor, wide),
            4 => ExecMulCore(m, cursor, wide, signed: false),
            5 => ExecMulCore(m, cursor, wide, signed: true),
            6 => ExecDivCore(m, cursor, wide, signed: false, negateQuotient: false),
            7 => ExecDivCore(m, cursor, wide, signed: true, negateQuotient: op.Rep != RepKind.None),
            _ => HaltUnsupported(),
        };
    }

    // ---- Shifts and rotates (D0 rm8/1, D1 rm16/1, D2 rm8/CL, D3 rm16/CL) ------------------------------

    private StepResult ExecShiftGroup(PrefixInfo op, bool wide)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        ModRmInfo m = ReadModRm(ref cursor, op.SegmentOverride);
        bool countIsCl = op.Opcode is 0xD2 or 0xD3;
        byte count = countIsCl ? (byte)_state.CX : (byte)1;
        var s = _state;
        ushort value = wide ? ReadRm16(s, m) : ReadRm8(s, m);

        // Count 0: nothing changes, not even flags.
        if (count == 0)
        {
            return Executed(cursor);
        }

        ushort mask = wide ? (ushort)0xFFFF : (ushort)0xFF;
        ushort sign = wide ? (ushort)0x8000 : (ushort)0x80;

        // Undocumented SETMO (arm 6): the operand becomes all ones; count>0 ignores the count value.
        if (m.Reg == 6)
        {
            ushort flags = SetFlag(s.Flags, FlagBit.SF, true);
            flags = SetFlag(flags, FlagBit.ZF, false);
            flags = SetFlag(flags, FlagBit.PF, true);
            flags = SetFlag(flags, FlagBit.CF, false);
            flags = SetFlag(flags, FlagBit.OF, false);
            flags = SetFlag(flags, FlagBit.AF, false);
            if (wide)
            {
                WriteRm16(ref s, m, mask);
            }
            else
            {
                WriteRm8(ref s, m, (byte)mask);
            }
            _state = s with { Flags = flags };
            return Executed(cursor);
        }

        ushort result = value;
        bool cf = (s.Flags & (ushort)FlagBit.CF) != 0;
        bool of = false;

        switch (m.Reg)
        {
            case 0: // ROL
                for (int i = 0; i < count; i++)
                {
                    cf = (result & sign) != 0;
                    result = (ushort)(((result << 1) | (cf ? 1 : 0)) & mask);
                }
                if (count == 1)
                {
                    of = ((result & sign) != 0) ^ cf;
                }
                break;
            case 1: // ROR
                for (int i = 0; i < count; i++)
                {
                    cf = (result & 1) != 0;
                    result = (ushort)((result >> 1) | (cf ? sign : 0));
                }
                if (count == 1)
                {
                    of = ((result & sign) != 0) ^ ((result & (sign >> 1)) != 0);
                }
                break;
            case 2: // RCL through the carry (9/17-bit ring)
                for (int i = 0; i < count; i++)
                {
                    bool msb = (result & sign) != 0;
                    result = (ushort)(((result << 1) | (cf ? 1 : 0)) & mask);
                    cf = msb;
                }
                if (count == 1)
                {
                    of = ((result & sign) != 0) ^ cf;
                }
                break;
            case 3: // RCR through the carry (9/17-bit ring)
                for (int i = 0; i < count; i++)
                {
                    bool lsb = (result & 1) != 0;
                    result = (ushort)(((result >> 1) | (cf ? sign : 0)) & mask);
                    cf = lsb;
                }
                if (count == 1)
                {
                    of = ((value & sign) != 0) ^ ((result & sign) != 0);
                }
                break;
            case 4: // SHL/SAL
                for (int i = 0; i < count; i++)
                {
                    cf = (result & sign) != 0;
                    result = (ushort)((result << 1) & mask);
                }
                if (count == 1)
                {
                    of = ((result & sign) != 0) ^ cf;
                }
                break;
            case 5: // SHR
                for (int i = 0; i < count; i++)
                {
                    cf = (result & 1) != 0;
                    result = (ushort)(result >> 1);
                }
                if (count == 1)
                {
                    of = (value & sign) != 0;
                }
                break;
            default: // SAR (arm 7): sign fill; OF=0 for count 1.
                for (int i = 0; i < count; i++)
                {
                    cf = (result & 1) != 0;
                    result = (ushort)(((result >> 1) | (result & sign)) & mask);
                }
                break;
        }

        ushort outFlags = SetFlag(s.Flags, FlagBit.CF, cf);
        if (count == 1)
        {
            outFlags = SetFlag(outFlags, FlagBit.OF, of);
        }
        if (m.Reg >= 4)
        {
            outFlags = SetFlag(outFlags, FlagBit.SF, (result & sign) != 0);
            outFlags = SetFlag(outFlags, FlagBit.ZF, result == 0);
            outFlags = SetFlag(outFlags, FlagBit.PF, BitOperations.PopCount((uint)(byte)result) % 2 == 0);
        }

        if (wide)
        {
            WriteRm16(ref s, m, result);
        }
        else
        {
            WriteRm8(ref s, m, (byte)result);
        }
        _state = s with { Flags = outFlags };
        return Executed(cursor);
    }
}
