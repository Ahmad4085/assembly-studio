using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

/// <summary>
/// String instructions and REP prefixes (TASK-017, FR-036, NFR-017):
/// MOVSB/MOVSW (A4/A5), CMPSB/CMPSW (A6/A7), STOSB/STOSW (AA/AB), LODSB/LODSW (AC/AD), SCASB/SCASW (AE/AF).
/// Prefixes: F2 (REPNZ / REPNE), F3 (REP / REPZ / REPE).
/// 
/// Semantics per Intel 8086 reference manual and SingleStepTests oracle:
/// - Destination: [ES:DI] is always addressed relative to segment ES; segment override prefixes are
///   ignored for destination operands.
/// - Source: [DS:SI] defaults to segment DS, but segment override prefixes (CS, DS, ES, SS) are honored.
/// - Direction: DF=0 increments SI/DI by 1 (byte) or 2 (word); DF=1 decrements SI/DI by 1 (byte) or 2 (word).
///   Addresses wrap at 16 bits within segment.
/// - Flags:
///   - MOVS, STOS, LODS modify no flags.
///   - CMPS and SCAS compute arithmetic flags (CF, AF, ZF, SF, PF, OF) via comparison subtraction.
/// - Loop and REP semantics:
///   - When no REP prefix is present, exactly one iteration executes; CX is neither checked nor modified.
///   - When a REP prefix (F2/F3) is present and CX is initially 0, the instruction is a no-op (advances past
///     the instruction; no registers, memory or flags are touched).
///   - For MOVS, STOS, and LODS: both F2 and F3 repeat while CX != 0, decrementing CX each iteration.
///   - For CMPS and SCAS:
///     - F3 (REPE / REPZ): loops while CX != 0 and ZF == 1.
///     - F2 (REPNE / REPNZ): loops while CX != 0 and ZF == 0.
/// 
/// Decision on per-iteration step semantics (TASK-017 requirement):
/// In <see cref="ICpu.StepInstruction"/>, an instruction with a REP prefix executes as a single, complete
/// instruction step. It loops through all iterations until termination (CX=0 or ZF condition met), advancing
/// IP past the instruction at completion. This matches x86 instruction-level semantics and SingleStepTests
/// oracle expectations.
/// </summary>
public sealed partial class Cpu8086
{
    private StepResult ExecMovs(PrefixInfo op, bool wide)
    {
        ushort endIp = (ushort)(op.OpcodeIp + 1);
        if (op.Rep != RepKind.None && _state.CX == 0)
        {
            return Executed(endIp);
        }

        var s = _state;
        int step = wide ? 2 : 1;
        int delta = (s.Flags & (ushort)FlagBit.DF) != 0 ? -step : step;
        ushort srcSeg = DataSegment(s, op.SegmentOverride);
        ushort dstSeg = s.ES;

        if (op.Rep == RepKind.None)
        {
            if (wide)
            {
                memory.Write16(dstSeg, s.DI, memory.Read16(srcSeg, s.SI));
            }
            else
            {
                memory.Write8(dstSeg, s.DI, memory.Read8(srcSeg, s.SI));
            }

            s = s with
            {
                SI = (ushort)(s.SI + delta),
                DI = (ushort)(s.DI + delta),
            };
        }
        else
        {
            while (s.CX != 0)
            {
                if (wide)
                {
                    memory.Write16(dstSeg, s.DI, memory.Read16(srcSeg, s.SI));
                }
                else
                {
                    memory.Write8(dstSeg, s.DI, memory.Read8(srcSeg, s.SI));
                }

                s = s with
                {
                    SI = (ushort)(s.SI + delta),
                    DI = (ushort)(s.DI + delta),
                    CX = (ushort)(s.CX - 1),
                };
            }
        }

        _state = s;
        return Executed(endIp);
    }

    private StepResult ExecCmps(PrefixInfo op, bool wide)
    {
        ushort endIp = (ushort)(op.OpcodeIp + 1);
        if (op.Rep != RepKind.None && _state.CX == 0)
        {
            return Executed(endIp);
        }

        var s = _state;
        int step = wide ? 2 : 1;
        int delta = (s.Flags & (ushort)FlagBit.DF) != 0 ? -step : step;
        ushort srcSeg = DataSegment(s, op.SegmentOverride);
        ushort dstSeg = s.ES;

        if (op.Rep == RepKind.None)
        {
            ushort left = wide ? memory.Read16(srcSeg, s.SI) : memory.Read8(srcSeg, s.SI);
            ushort right = wide ? memory.Read16(dstSeg, s.DI) : memory.Read8(dstSeg, s.DI);
            ushort newFlags = Arith(s, ArithKind.Cmp, wide, left, right, out _);

            s = s with
            {
                SI = (ushort)(s.SI + delta),
                DI = (ushort)(s.DI + delta),
                Flags = newFlags,
            };
        }
        else
        {
            while (s.CX != 0)
            {
                ushort left = wide ? memory.Read16(srcSeg, s.SI) : memory.Read8(srcSeg, s.SI);
                ushort right = wide ? memory.Read16(dstSeg, s.DI) : memory.Read8(dstSeg, s.DI);
                ushort newFlags = Arith(s, ArithKind.Cmp, wide, left, right, out _);

                s = s with
                {
                    SI = (ushort)(s.SI + delta),
                    DI = (ushort)(s.DI + delta),
                    CX = (ushort)(s.CX - 1),
                    Flags = newFlags,
                };

                if (s.CX == 0)
                {
                    break;
                }

                bool zf = (newFlags & (ushort)FlagBit.ZF) != 0;
                if (op.Rep == RepKind.Rep && !zf)
                {
                    break;
                }

                if (op.Rep == RepKind.Repne && zf)
                {
                    break;
                }
            }
        }

        _state = s;
        return Executed(endIp);
    }

    private StepResult ExecStos(PrefixInfo op, bool wide)
    {
        ushort endIp = (ushort)(op.OpcodeIp + 1);
        if (op.Rep != RepKind.None && _state.CX == 0)
        {
            return Executed(endIp);
        }

        var s = _state;
        int step = wide ? 2 : 1;
        int delta = (s.Flags & (ushort)FlagBit.DF) != 0 ? -step : step;
        ushort dstSeg = s.ES;

        if (op.Rep == RepKind.None)
        {
            if (wide)
            {
                memory.Write16(dstSeg, s.DI, s.AX);
            }
            else
            {
                memory.Write8(dstSeg, s.DI, (byte)s.AX);
            }

            s = s with
            {
                DI = (ushort)(s.DI + delta),
            };
        }
        else
        {
            while (s.CX != 0)
            {
                if (wide)
                {
                    memory.Write16(dstSeg, s.DI, s.AX);
                }
                else
                {
                    memory.Write8(dstSeg, s.DI, (byte)s.AX);
                }

                s = s with
                {
                    DI = (ushort)(s.DI + delta),
                    CX = (ushort)(s.CX - 1),
                };
            }
        }

        _state = s;
        return Executed(endIp);
    }

    private StepResult ExecLods(PrefixInfo op, bool wide)
    {
        ushort endIp = (ushort)(op.OpcodeIp + 1);
        if (op.Rep != RepKind.None && _state.CX == 0)
        {
            return Executed(endIp);
        }

        var s = _state;
        int step = wide ? 2 : 1;
        int delta = (s.Flags & (ushort)FlagBit.DF) != 0 ? -step : step;
        ushort srcSeg = DataSegment(s, op.SegmentOverride);

        if (op.Rep == RepKind.None)
        {
            s = wide
                ? s with { AX = memory.Read16(srcSeg, s.SI), SI = (ushort)(s.SI + delta) }
                : s with { AX = (ushort)((s.AX & 0xFF00) | memory.Read8(srcSeg, s.SI)), SI = (ushort)(s.SI + delta) };
        }
        else
        {
            while (s.CX != 0)
            {
                s = wide
                    ? s with { AX = memory.Read16(srcSeg, s.SI), SI = (ushort)(s.SI + delta), CX = (ushort)(s.CX - 1) }
                    : s with { AX = (ushort)((s.AX & 0xFF00) | memory.Read8(srcSeg, s.SI)), SI = (ushort)(s.SI + delta), CX = (ushort)(s.CX - 1) };
            }
        }

        _state = s;
        return Executed(endIp);
    }

    private StepResult ExecScas(PrefixInfo op, bool wide)
    {
        ushort endIp = (ushort)(op.OpcodeIp + 1);
        if (op.Rep != RepKind.None && _state.CX == 0)
        {
            return Executed(endIp);
        }

        var s = _state;
        int step = wide ? 2 : 1;
        int delta = (s.Flags & (ushort)FlagBit.DF) != 0 ? -step : step;
        ushort dstSeg = s.ES;

        if (op.Rep == RepKind.None)
        {
            ushort left = wide ? s.AX : (byte)s.AX;
            ushort right = wide ? memory.Read16(dstSeg, s.DI) : memory.Read8(dstSeg, s.DI);
            ushort newFlags = Arith(s, ArithKind.Cmp, wide, left, right, out _);

            s = s with
            {
                DI = (ushort)(s.DI + delta),
                Flags = newFlags,
            };
        }
        else
        {
            while (s.CX != 0)
            {
                ushort left = wide ? s.AX : (byte)s.AX;
                ushort right = wide ? memory.Read16(dstSeg, s.DI) : memory.Read8(dstSeg, s.DI);
                ushort newFlags = Arith(s, ArithKind.Cmp, wide, left, right, out _);

                s = s with
                {
                    DI = (ushort)(s.DI + delta),
                    CX = (ushort)(s.CX - 1),
                    Flags = newFlags,
                };

                if (s.CX == 0)
                {
                    break;
                }

                bool zf = (newFlags & (ushort)FlagBit.ZF) != 0;
                if (op.Rep == RepKind.Rep && !zf)
                {
                    break;
                }

                if (op.Rep == RepKind.Repne && zf)
                {
                    break;
                }
            }
        }

        _state = s;
        return Executed(endIp);
    }
}
