using System.Numerics;
using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// Multiply, divide, BCD adjust and sign-extension instructions (TASK-015, FR-036/FR-038/FR-048):
/// MUL/IMUL/DIV/IDIV (F6/F7 arms 4-7), AAA/AAS/DAA/DAS (37/3F/27/2F), AAM/AAD (D4/D5) and
/// CBW/CWD (98/99). Each step reports the flags the instruction leaves undefined (ADR-0003 §3).
/// <para>
/// Division follows the 8086 microcode's shift-and-subtract loop (<see cref="Cord"/>), which
/// yields the real chip's flags. They matter when the division faults: the divide error pushes
/// FLAGS, so the oracle compares them byte for byte on the stack. A divide error (quotient
/// overflow, divisor 0, or AAM 0) raises interrupt 0 through the router; with no handler the CPU
/// vectors with the 8086 return address, which is the instruction <i>after</i> the faulting one.
/// IDIV rejects a quotient of -128/-32768 (the 8086 range is ±127/±32767), and a REP prefix negates
/// the IDIV quotient (undocumented, oracle-verified). AAA/AAS adjust AL and AH separately (no
/// carry from AL into AH).
/// </para>
/// </summary>
public sealed partial class Cpu8086
{
    private const FlagBit MulUndefined = FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF;
    private const FlagBit DivUndefined = FlagBit.OF | FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF | FlagBit.CF;
    private const FlagBit AsciiAdjustUndefined = FlagBit.OF | FlagBit.SF | FlagBit.ZF | FlagBit.PF;
    private const FlagBit AamAadUndefined = FlagBit.OF | FlagBit.AF | FlagBit.CF;

    private static ushort SubFlags(ushort flags, bool wide, ushort left, ushort right) =>
        Arith(new CpuState { Flags = flags }, ArithKind.Sub, wide, left, right, out _);

    private static ushort SzpFlags(ushort flags, byte value)
    {
        flags = SetFlag(flags, FlagBit.SF, (value & 0x80) != 0);
        flags = SetFlag(flags, FlagBit.ZF, value == 0);
        return SetFlag(flags, FlagBit.PF, BitOperations.PopCount(value) % 2 == 0);
    }

    /// <summary>
    /// The 8086 division microcode (CORD): divides high:low by <paramref name="divisor"/> one bit
    /// per iteration. Flags come from the entry compare and then from each trial subtraction made
    /// without a bit shifted out of the partial remainder; CF ends as the complement of the
    /// quotient's top bit. Returns <see langword="false"/> on overflow (high &gt;= divisor, which
    /// includes divisor 0), with the flags of the entry compare.
    /// </summary>
    private static bool Cord(ushort high, ushort low, ushort divisor, bool wide, ref ushort flags,
        out ushort quotient, out ushort remainder)
    {
        int bits = wide ? 16 : 8;
        uint mask = wide ? 0xFFFFu : 0xFFu;
        flags = SubFlags(flags, wide, high, divisor);
        if (high >= divisor)
        {
            quotient = 0;
            remainder = 0;
            return false;
        }

        // The quotient is shifted into tmpC as inverted bits: carry 1 = trial subtraction borrowed.
        uint tmpA = high, tmpC = low, carry = 1;
        for (int i = 0; i < bits; i++)
        {
            uint outC = (tmpC >> (bits - 1)) & 1;
            tmpC = ((tmpC << 1) | carry) & mask;
            uint shiftedOut = (tmpA >> (bits - 1)) & 1;
            tmpA = ((tmpA << 1) | outC) & mask;
            if (shiftedOut != 0)
            {
                tmpA = (tmpA - divisor) & mask;
                carry = 0;
            }
            else
            {
                flags = SubFlags(flags, wide, (ushort)tmpA, divisor);
                if (tmpA >= divisor)
                {
                    tmpA = (tmpA - divisor) & mask;
                    carry = 0;
                }
                else
                {
                    carry = 1;
                }
            }
        }

        tmpC = ((tmpC << 1) | carry) & mask;
        quotient = (ushort)(~tmpC & mask);
        remainder = (ushort)tmpA;
        flags = SetFlag(flags, FlagBit.CF, (quotient >> (bits - 1)) == 0);
        return true;
    }

    /// <summary>
    /// Divide error (interrupt 0). The router sees the state at the faulting instruction, unchanged;
    /// with no handler the CPU vectors, pushing the flags the division microcode produced and the
    /// address of the next instruction (8086 behavior).
    /// </summary>
    private StepResult DivideError(ushort nextIp, ushort flagsAtFault)
    {
        StepResult? handled = interruptRouter?.Intercept(0, _state);
        if (handled is not null)
        {
            return handled.Value;
        }

        VectorInterrupt(0, flagsAtFault, nextIp);
        return new StepResult(StepOutcome.Executed, 0, _state.IP, HaltReason.None, DivUndefined);
    }

    // ---- MUL / IMUL (F6/F7 arms 4/5) -----------------------------------------------------------------

    /// <summary>MUL/IMUL: AX = AL * rm8, or DX:AX = AX * rm16. CF = OF = the upper half is significant
    /// (MUL: non-zero; IMUL: not the sign extension of the lower half). SF/ZF/AF/PF are undefined and
    /// left untouched.</summary>
    private StepResult ExecMulCore(in ModRmInfo m, ushort cursor, bool wide, bool signed)
    {
        var s = _state;
        bool upperSignificant;
        if (wide)
        {
            ushort source = ReadRm16(s, m);
            uint product = signed ? (uint)((short)s.AX * (short)source) : (uint)s.AX * source;
            s = s with { AX = (ushort)product, DX = (ushort)(product >> 16) };
            upperSignificant = signed ? (int)product != (short)product : (product >> 16) != 0;
        }
        else
        {
            byte source = ReadRm8(s, m);
            ushort product = signed ? (ushort)((sbyte)s.AX * (sbyte)source) : (ushort)((byte)s.AX * source);
            s = s with { AX = product };
            upperSignificant = signed ? (short)product != (sbyte)product : (product >> 8) != 0;
        }

        ushort flags = SetFlag(s.Flags, FlagBit.CF, upperSignificant);
        _state = s with { Flags = SetFlag(flags, FlagBit.OF, upperSignificant) };
        return Executed(cursor) with { UndefinedFlags = MulUndefined };
    }

    // ---- DIV / IDIV (F6/F7 arms 6/7) -----------------------------------------------------------------

    /// <summary>DIV/IDIV: AX / rm8 (AL = quotient, AH = remainder) or DX:AX / rm16 (AX = quotient,
    /// DX = remainder). IDIV divides magnitudes, then applies the signs (the remainder takes the
    /// dividend's sign). All six arithmetic flags are undefined.</summary>
    private StepResult ExecDivCore(in ModRmInfo m, ushort cursor, bool wide, bool signed, bool negateQuotient)
    {
        var s = _state;
        int bits = wide ? 16 : 8;
        ushort mask = wide ? (ushort)0xFFFF : (ushort)0xFF;
        ushort sign = wide ? (ushort)0x8000 : (ushort)0x80;
        ushort divisor = wide ? ReadRm16(s, m) : ReadRm8(s, m);
        ushort high = wide ? s.DX : (ushort)(s.AX >> 8);
        ushort low = wide ? s.AX : (ushort)(s.AX & 0xFF);

        bool dividendNegative = signed && (high & sign) != 0;
        bool divisorNegative = signed && (divisor & sign) != 0;
        if (dividendNegative)
        {
            uint magnitude = ~(((uint)high << bits) | low) + 1;
            high = (ushort)((magnitude >> bits) & mask);
            low = (ushort)(magnitude & mask);
        }

        if (divisorNegative)
        {
            divisor = (ushort)(-divisor & mask);
        }

        ushort flags = s.Flags;
        if (!Cord(high, low, divisor, wide, ref flags, out ushort quotient, out ushort remainder)
            || (signed && (quotient & sign) != 0))
        {
            return DivideError(cursor, flags);
        }

        if (dividendNegative ^ divisorNegative ^ negateQuotient)
        {
            quotient = (ushort)(-quotient & mask);
        }

        if (dividendNegative)
        {
            remainder = (ushort)(-remainder & mask);
        }

        s = wide
            ? s with { AX = quotient, DX = remainder }
            : s with { AX = (ushort)((remainder << 8) | quotient) };
        _state = s with { Flags = flags };
        return Executed(cursor) with { UndefinedFlags = DivUndefined };
    }

    // ---- AAA / AAS (37 / 3F) -------------------------------------------------------------------------

    /// <summary>AAA/AAS: when AL's low nibble is above 9 or AF is set, AL += 6 / -= 6 and AH += 1 / -= 1
    /// (separately: no carry or borrow between them) and CF = AF = 1; otherwise CF = AF = 0. AL keeps
    /// only its low nibble. OF/SF/ZF/PF are undefined and left untouched.</summary>
    private StepResult ExecAsciiAdjust(PrefixInfo op, bool add)
    {
        var s = _state;
        byte al = (byte)s.AX;
        byte ah = (byte)(s.AX >> 8);
        bool adjust = (al & 0x0F) > 9 || (s.Flags & (ushort)FlagBit.AF) != 0;
        if (adjust)
        {
            al = (byte)(add ? al + 6 : al - 6);
            ah = (byte)(add ? ah + 1 : ah - 1);
        }

        ushort flags = SetFlag(s.Flags, FlagBit.AF, adjust);
        flags = SetFlag(flags, FlagBit.CF, adjust);
        _state = s with { AX = (ushort)((ah << 8) | (al & 0x0F)), Flags = flags };
        return Executed((ushort)(op.OpcodeIp + 1)) with { UndefinedFlags = AsciiAdjustUndefined };
    }

    // ---- DAA / DAS (27 / 2F) -------------------------------------------------------------------------

    /// <summary>DAA/DAS: decimal adjust AL after an addition/subtraction (Intel's algorithm). AF and CF
    /// record the adjustments; SF/ZF/PF come from the result. OF is undefined and left untouched.</summary>
    private StepResult ExecDecimalAdjust(PrefixInfo op, bool add)
    {
        var s = _state;
        byte oldAl = (byte)s.AX;
        bool oldCf = (s.Flags & (ushort)FlagBit.CF) != 0;
        bool oldAf = (s.Flags & (ushort)FlagBit.AF) != 0;
        int al = oldAl;
        bool cf = false;
        bool af = (al & 0x0F) > 9 || oldAf;
        if (af)
        {
            al = add ? al + 6 : al - 6;
            cf = oldCf; // 8086: the low-digit carry/borrow does not reach CF (oracle-verified for DAS)
            al &= 0xFF;
        }

        // 8086 quirk (oracle-verified): with AF set on entry the high-digit threshold is 0x9F, not 0x99.
        if (oldAl > (oldAf ? 0x9F : 0x99) || oldCf)
        {
            al = (add ? al + 0x60 : al - 0x60) & 0xFF;
            cf = true;
        }

        ushort flags = SetFlag(s.Flags, FlagBit.AF, af);
        flags = SetFlag(flags, FlagBit.CF, cf);
        _state = s with { AX = (ushort)((s.AX & 0xFF00) | al), Flags = SzpFlags(flags, (byte)al) };
        return Executed((ushort)(op.OpcodeIp + 1)) with { UndefinedFlags = FlagBit.OF };
    }

    // ---- AAM / AAD (D4 / D5, imm8 base) --------------------------------------------------------------

    /// <summary>AAM imm8: AH = AL / imm, AL = AL % imm through the division microcode; imm 0 raises a
    /// divide error. SF/ZF/PF come from AL; OF/AF/CF are undefined.</summary>
    private StepResult ExecAam(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        byte divisor = Fetch8(ref cursor);
        var s = _state;
        ushort flags = s.Flags;
        if (!Cord(0, (byte)s.AX, divisor, wide: false, ref flags, out ushort quotient, out ushort remainder))
        {
            return DivideError(cursor, flags);
        }

        _state = s with { AX = (ushort)((quotient << 8) | remainder), Flags = SzpFlags(flags, (byte)remainder) };
        return Executed(cursor) with { UndefinedFlags = AamAadUndefined };
    }

    /// <summary>AAD imm8: AL = AL + AH * imm (low byte), AH = 0. Flags are those of the final byte
    /// addition; SF/ZF/PF are defined, OF/AF/CF undefined.</summary>
    private StepResult ExecAad(PrefixInfo op)
    {
        ushort cursor = (ushort)(op.OpcodeIp + 1);
        byte factor = Fetch8(ref cursor);
        var s = _state;
        byte product = (byte)((s.AX >> 8) * factor);
        ushort flags = Arith(s, ArithKind.Add, wide: false, (byte)s.AX, product, out ushort result);
        _state = s with { AX = result, Flags = flags };
        return Executed(cursor) with { UndefinedFlags = AamAadUndefined };
    }

    // ---- CBW / CWD (98 / 99): no flags ---------------------------------------------------------------

    private StepResult ExecCbw(PrefixInfo op)
    {
        _state = _state with { AX = (ushort)(sbyte)_state.AX };
        return Executed((ushort)(op.OpcodeIp + 1));
    }

    private StepResult ExecCwd(PrefixInfo op)
    {
        _state = _state with { DX = (_state.AX & 0x8000) != 0 ? (ushort)0xFFFF : (ushort)0 };
        return Executed((ushort)(op.OpcodeIp + 1));
    }
}
