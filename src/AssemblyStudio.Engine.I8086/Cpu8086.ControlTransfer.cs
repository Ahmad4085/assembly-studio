using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086;

public sealed partial class Cpu8086
{
    // ---- Conditional Jumps (70–7F): rel8 displacement, flags unchanged ---------------------------------

    private StepResult ExecJcc(PrefixInfo op)
    {
        byte code = op.Opcode;
        ushort nextIp = (ushort)(op.OpcodeIp + 2);
        sbyte disp = (sbyte)memory.Read8(_state.CS, (ushort)(op.OpcodeIp + 1));
        ushort flags = _state.Flags;

        bool cf = (flags & (ushort)FlagBit.CF) != 0;
        bool zf = (flags & (ushort)FlagBit.ZF) != 0;
        bool sf = (flags & (ushort)FlagBit.SF) != 0;
        bool of = (flags & (ushort)FlagBit.OF) != 0;
        bool pf = (flags & (ushort)FlagBit.PF) != 0;

        bool take = (code & 0x0F) switch
        {
            0x0 => of,                        // JO
            0x1 => !of,                       // JNO
            0x2 => cf,                        // JB / JC / JNAE
            0x3 => !cf,                       // JNB / JNC / JAE
            0x4 => zf,                        // JZ / JE
            0x5 => !zf,                       // JNZ / JNE
            0x6 => cf || zf,                  // JBE / JNA
            0x7 => !cf && !zf,                // JA / JNBE
            0x8 => sf,                        // JS
            0x9 => !sf,                       // JNS
            0xA => pf,                        // JP / JPE
            0xB => !pf,                       // JNP / JPO
            0xC => sf != of,                  // JL / JNGE
            0xD => sf == of,                  // JGE / JNL
            0xE => zf || (sf != of),          // JLE / JNG
            0xF => !zf && (sf == of),         // JG / JNLE
            _ => false,
        };

        if (take)
        {
            ushort targetIp = (ushort)(nextIp + disp);
            _state = _state with { IP = targetIp };
            return Executed(targetIp);
        }

        return Executed(nextIp);
    }

    // ---- JCXZ (E3) and Loops (E0–E2): rel8 displacement, flags unchanged -------------------------------

    private StepResult ExecJcxz(PrefixInfo op)
    {
        ushort nextIp = (ushort)(op.OpcodeIp + 2);
        sbyte disp = (sbyte)memory.Read8(_state.CS, (ushort)(op.OpcodeIp + 1));

        if (_state.CX == 0)
        {
            ushort targetIp = (ushort)(nextIp + disp);
            _state = _state with { IP = targetIp };
            return Executed(targetIp);
        }

        return Executed(nextIp);
    }

    private StepResult ExecLoop(PrefixInfo op)
    {
        byte code = op.Opcode;
        ushort nextIp = (ushort)(op.OpcodeIp + 2);
        sbyte disp = (sbyte)memory.Read8(_state.CS, (ushort)(op.OpcodeIp + 1));

        ushort newCx = (ushort)(_state.CX - 1);
        _state = _state with { CX = newCx };

        bool zf = (_state.Flags & (ushort)FlagBit.ZF) != 0;

        bool take = code switch
        {
            0xE0 => newCx != 0 && !zf, // LOOPNZ / LOOPNE
            0xE1 => newCx != 0 && zf,  // LOOPZ / LOOPE
            0xE2 => newCx != 0,        // LOOP
            _ => false,
        };

        if (take)
        {
            ushort targetIp = (ushort)(nextIp + disp);
            _state = _state with { IP = targetIp };
            return Executed(targetIp);
        }

        return Executed(nextIp);
    }

    // ---- Unconditional Jumps (EB, E9, EA) ----------------------------------------------------------------

    private StepResult ExecJmpShort(PrefixInfo op)
    {
        ushort nextIp = (ushort)(op.OpcodeIp + 2);
        sbyte disp = (sbyte)memory.Read8(_state.CS, (ushort)(op.OpcodeIp + 1));
        ushort targetIp = (ushort)(nextIp + disp);
        _state = _state with { IP = targetIp };
        return Executed(targetIp);
    }

    private StepResult ExecJmpNear(PrefixInfo op)
    {
        ushort nextIp = (ushort)(op.OpcodeIp + 3);
        ushort disp = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 1));
        ushort targetIp = (ushort)(nextIp + (short)disp);
        _state = _state with { IP = targetIp };
        return Executed(targetIp);
    }

    private StepResult ExecJmpFar(PrefixInfo op)
    {
        ushort targetIp = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 1));
        ushort targetCs = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 3));
        _state = _state with { CS = targetCs, IP = targetIp };
        return Executed(targetIp);
    }

    // ---- Direct Calls (E8, 9A) --------------------------------------------------------------------------

    private StepResult ExecCallNear(PrefixInfo op)
    {
        ushort nextIp = (ushort)(op.OpcodeIp + 3);
        ushort disp = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 1));
        ushort targetIp = (ushort)(nextIp + (short)disp);

        var s = _state;
        Push(ref s, nextIp);
        s = s with { IP = targetIp };
        _state = s;
        return Executed(targetIp);
    }

    private StepResult ExecCallFar(PrefixInfo op)
    {
        ushort nextIp = (ushort)(op.OpcodeIp + 5);
        ushort targetIp = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 1));
        ushort targetCs = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 3));

        var s = _state;
        Push(ref s, s.CS);
        Push(ref s, nextIp);
        s = s with { CS = targetCs, IP = targetIp };
        _state = s;
        return Executed(targetIp);
    }

    // ---- Returns (C2, C3, CA, CB) -----------------------------------------------------------------------

    private StepResult ExecRetNear(PrefixInfo op)
    {
        var s = _state;
        ushort newIp = Pop(ref s);
        s = s with { IP = newIp };
        _state = s;
        return Executed(newIp);
    }

    private StepResult ExecRetNearImm(PrefixInfo op)
    {
        ushort imm = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 1));
        var s = _state;
        ushort newIp = Pop(ref s);
        s = s with { IP = newIp, SP = (ushort)(s.SP + imm) };
        _state = s;
        return Executed(newIp);
    }

    private StepResult ExecRetFar(PrefixInfo op)
    {
        var s = _state;
        ushort newIp = Pop(ref s);
        ushort newCs = Pop(ref s);
        s = s with { CS = newCs, IP = newIp };
        _state = s;
        return Executed(newIp);
    }

    private StepResult ExecRetFarImm(PrefixInfo op)
    {
        ushort imm = memory.Read16(_state.CS, (ushort)(op.OpcodeIp + 1));
        var s = _state;
        ushort newIp = Pop(ref s);
        ushort newCs = Pop(ref s);
        s = s with { CS = newCs, IP = newIp, SP = (ushort)(s.SP + imm) };
        _state = s;
        return Executed(newIp);
    }

    // ---- Interrupts (CC, CE, CF) ------------------------------------------------------------------------

    private StepResult ExecInt3(PrefixInfo op)
    {
        CpuState atInt = _state;
        ushort returnIp = (ushort)(op.OpcodeIp + 1);

        StepResult? handled = interruptRouter?.Intercept(3, atInt);
        if (handled is not null)
        {
            return handled.Value;
        }

        VectorInterrupt(3, atInt.Flags, returnIp);
        return new StepResult(StepOutcome.Executed, op.OpcodeIp, op.OpcodeIp, HaltReason.None, FlagBit.None);
    }

    private StepResult ExecInto(PrefixInfo op)
    {
        CpuState atInt = _state;
        ushort returnIp = (ushort)(op.OpcodeIp + 1);

        if ((atInt.Flags & (ushort)FlagBit.OF) != 0)
        {
            StepResult? handled = interruptRouter?.Intercept(4, atInt);
            if (handled is not null)
            {
                return handled.Value;
            }

            VectorInterrupt(4, atInt.Flags, returnIp);
            return new StepResult(StepOutcome.Executed, op.OpcodeIp, op.OpcodeIp, HaltReason.None, FlagBit.None);
        }

        return Executed(returnIp);
    }

    private StepResult ExecIret(PrefixInfo op)
    {
        var s = _state;
        ushort newIp = Pop(ref s);
        ushort newCs = Pop(ref s);
        ushort rawFlags = Pop(ref s);

        // Canonical 8086 flags format per POPF: bits 12–15 and 1 are 1, bits 3 and 5 are 0.
        ushort canonical = (ushort)(((rawFlags | 0xF002) & ~0x0028) & 0xFFFF);
        s = s with { CS = newCs, IP = newIp, Flags = canonical };
        _state = s;
        return Executed(newIp);
    }
}
