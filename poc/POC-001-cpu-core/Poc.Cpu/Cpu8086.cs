namespace Poc.Cpu;

/// <summary>8086 FLAGS bits.</summary>
[Flags]
public enum FlagBit : ushort
{
    CF = 0x0001, PF = 0x0004, AF = 0x0010, ZF = 0x0040, SF = 0x0080,
    TF = 0x0100, IF = 0x0200, DF = 0x0400, OF = 0x0800,
}

/// <summary>Raised when execution reaches an opcode outside the supported subset (FR-037).</summary>
public sealed class UnsupportedInstructionException(ushort cs, ushort ip, byte[] bytes, string reason)
    : Exception($"Unsupported instruction at {cs:X4}:{ip:X4} [{Convert.ToHexString(bytes)}]: {reason}")
{
    public ushort Cs { get; } = cs;
    public ushort Ip { get; } = ip;
    public IReadOnlyList<byte> Bytes { get; } = bytes;
}

/// <summary>Immutable register snapshot, as a debugger view would consume it.</summary>
public readonly record struct CpuRegisters(
    ushort AX, ushort BX, ushort CX, ushort DX, ushort SP, ushort BP, ushort SI, ushort DI,
    ushort CS, ushort DS, ushort ES, ushort SS, ushort IP, ushort Flags);

/// <summary>
/// POC-001 managed 8086 core: a representative subset only (see docs/02-research/poc-001.md).
/// Real-mode, 1 MiB address space with 20-bit wrap; word operands wrap within the segment.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields",
    Justification = "POC-001: register file and memory are hot-path state; the product core API is decided by ADR.")]
public sealed class Cpu8086
{
    public const int MemorySize = 1 << 20;

    // Register files in 8086 encoding order.
    public const int AX = 0, CX = 1, DX = 2, BX = 3, SP = 4, BP = 5, SI = 6, DI = 7;
    public const int ES = 0, CS = 1, SS = 2, DS = 3;

    public readonly ushort[] Regs = new ushort[8];
    public readonly ushort[] Segs = new ushort[4];
    public ushort IP;
    public ushort Flags;
    public readonly byte[] Memory = new byte[MemorySize];

    /// <summary>Optional host hook for INT n. Returns true if the host handled it (no vectoring).</summary>
    public Func<Cpu8086, byte, bool>? InterruptHook { get; set; }

    /// <summary>When non-null, every memory write's physical address is appended (test oracle support).</summary>
    public List<int>? WriteLog { get; set; }

    public long InstructionCount { get; private set; }

    private int _segOverride;
    private ushort _instrCs, _instrIp;

    public CpuRegisters Snapshot() => new(
        Regs[AX], Regs[BX], Regs[CX], Regs[DX], Regs[SP], Regs[BP], Regs[SI], Regs[DI],
        Segs[CS], Segs[DS], Segs[ES], Segs[SS], IP, Flags);

    // ---------------------------------------------------------------- memory

    public static int Phys(ushort seg, ushort off) => ((seg << 4) + off) & (MemorySize - 1);

    public byte Read8(ushort seg, ushort off) => Memory[Phys(seg, off)];

    public ushort Read16(ushort seg, ushort off) =>
        (ushort)(Memory[Phys(seg, off)] | (Memory[Phys(seg, (ushort)(off + 1))] << 8));

    public void Write8(ushort seg, ushort off, byte value)
    {
        int p = Phys(seg, off);
        Memory[p] = value;
        WriteLog?.Add(p);
    }

    public void Write16(ushort seg, ushort off, ushort value)
    {
        Write8(seg, off, (byte)value);
        Write8(seg, (ushort)(off + 1), (byte)(value >> 8));
    }

    private byte Fetch8() => Read8(Segs[CS], IP++);

    private ushort Fetch16()
    {
        ushort lo = Fetch8();
        return (ushort)(lo | (Fetch8() << 8));
    }

    private void Push(ushort value)
    {
        Regs[SP] -= 2;
        Write16(Segs[SS], Regs[SP], value);
    }

    private ushort Pop()
    {
        ushort value = Read16(Segs[SS], Regs[SP]);
        Regs[SP] += 2;
        return value;
    }

    // ---------------------------------------------------------------- registers

    private byte GetReg8(int r) => (byte)(r < 4 ? Regs[r] : Regs[r - 4] >> 8);

    private void SetReg8(int r, byte v)
    {
        if (r < 4) Regs[r] = (ushort)((Regs[r] & 0xFF00) | v);
        else Regs[r - 4] = (ushort)((Regs[r - 4] & 0x00FF) | (v << 8));
    }

    private bool Get(FlagBit f) => (Flags & (ushort)f) != 0;

    private void Set(FlagBit f, bool on) => Flags = on ? (ushort)(Flags | (ushort)f) : (ushort)(Flags & ~(ushort)f);

    // ---------------------------------------------------------------- ModR/M

    private struct ModRm
    {
        public int Mod, Reg, Rm;
        public ushort Seg, Off;
        public readonly bool IsReg => Mod == 3;
    }

    private ModRm DecodeModRm()
    {
        byte b = Fetch8();
        var m = new ModRm { Mod = b >> 6, Reg = (b >> 3) & 7, Rm = b & 7 };
        if (m.IsReg) return m;

        int defSeg = DS;
        ushort ea;
        switch (m.Rm)
        {
            case 0: ea = (ushort)(Regs[BX] + Regs[SI]); break;
            case 1: ea = (ushort)(Regs[BX] + Regs[DI]); break;
            case 2: ea = (ushort)(Regs[BP] + Regs[SI]); defSeg = SS; break;
            case 3: ea = (ushort)(Regs[BP] + Regs[DI]); defSeg = SS; break;
            case 4: ea = Regs[SI]; break;
            case 5: ea = Regs[DI]; break;
            case 6:
                if (m.Mod == 0) { ea = Fetch16(); }
                else { ea = Regs[BP]; defSeg = SS; }
                break;
            default: ea = Regs[BX]; break;
        }
        if (m.Mod == 1) ea = (ushort)(ea + (sbyte)Fetch8());
        else if (m.Mod == 2) ea = (ushort)(ea + Fetch16());

        m.Seg = Segs[_segOverride >= 0 ? _segOverride : defSeg];
        m.Off = ea;
        return m;
    }

    private ushort ReadRm(in ModRm m, bool word) =>
        m.IsReg ? (word ? Regs[m.Rm] : GetReg8(m.Rm))
                : (word ? Read16(m.Seg, m.Off) : Read8(m.Seg, m.Off));

    private void WriteRm(in ModRm m, bool word, ushort v)
    {
        if (m.IsReg) { if (word) Regs[m.Rm] = v; else SetReg8(m.Rm, (byte)v); }
        else if (word) Write16(m.Seg, m.Off, v);
        else Write8(m.Seg, m.Off, (byte)v);
    }

    private ushort ReadReg(int r, bool word) => word ? Regs[r] : GetReg8(r);

    private void WriteReg(int r, bool word, ushort v)
    {
        if (word) Regs[r] = v; else SetReg8(r, (byte)v);
    }

    // ---------------------------------------------------------------- ALU (ADD, SUB, CMP)

    private enum AluOp { Add, Sub, Cmp }

    private ushort Alu(AluOp op, ushort a, ushort b, bool word)
    {
        uint mask = word ? 0xFFFFu : 0xFFu;
        uint sign = word ? 0x8000u : 0x80u;
        uint r;
        if (op == AluOp.Add)
        {
            r = (uint)a + b;
            Set(FlagBit.CF, r > mask);
            Set(FlagBit.OF, ((a ^ r) & (b ^ r) & sign) != 0);
        }
        else
        {
            r = (uint)a - b;
            Set(FlagBit.CF, a < b);
            Set(FlagBit.OF, ((a ^ b) & (a ^ r) & sign) != 0);
        }
        r &= mask;
        Set(FlagBit.AF, ((a ^ b ^ r) & 0x10) != 0);
        Set(FlagBit.ZF, r == 0);
        Set(FlagBit.SF, (r & sign) != 0);
        Set(FlagBit.PF, (System.Numerics.BitOperations.PopCount(r & 0xFF) & 1) == 0);
        return (ushort)r;
    }

    private void AluRm(AluOp op, bool word, bool toReg)
    {
        var m = DecodeModRm();
        ushort rm = ReadRm(m, word), reg = ReadReg(m.Reg, word);
        if (toReg)
        {
            ushort r = Alu(op, reg, rm, word);
            if (op != AluOp.Cmp) WriteReg(m.Reg, word, r);
        }
        else
        {
            ushort r = Alu(op, rm, reg, word);
            if (op != AluOp.Cmp) WriteRm(m, word, r);
        }
    }

    private void AluAcc(AluOp op, bool word)
    {
        ushort imm = word ? Fetch16() : Fetch8();
        ushort r = Alu(op, ReadReg(AX, word), imm, word);
        if (op != AluOp.Cmp) WriteReg(AX, word, r);
    }

    private void AluGroup(byte opcode)
    {
        bool word = (opcode & 1) != 0;
        var m = DecodeModRm();
        ushort imm = opcode switch
        {
            0x81 => Fetch16(),
            0x83 => (ushort)(sbyte)Fetch8(),
            _ => Fetch8(),
        };
        AluOp op = m.Reg switch
        {
            0 => AluOp.Add,
            5 => AluOp.Sub,
            7 => AluOp.Cmp,
            _ => throw Unsupported($"group {opcode:X2} /{m.Reg} is outside the POC subset"),
        };
        ushort r = Alu(op, ReadRm(m, word), imm, word);
        if (op != AluOp.Cmp) WriteRm(m, word, r);
    }

    // ---------------------------------------------------------------- control flow

    private bool Condition(int cc)
    {
        bool cf = Get(FlagBit.CF), zf = Get(FlagBit.ZF), sf = Get(FlagBit.SF), of = Get(FlagBit.OF);
        bool result = (cc >> 1) switch
        {
            0 => of,
            1 => cf,
            2 => zf,
            3 => cf || zf,
            4 => sf,
            5 => Get(FlagBit.PF),
            6 => sf != of,
            _ => zf || sf != of,
        };
        return (cc & 1) == 0 ? result : !result;
    }

    private void Interrupt(byte vector)
    {
        if (InterruptHook?.Invoke(this, vector) == true) return;
        Push(Flags);
        Set(FlagBit.IF, false);
        Set(FlagBit.TF, false);
        Push(Segs[CS]);
        Push(IP);
        IP = Read16(0, (ushort)(vector * 4));
        Segs[CS] = Read16(0, (ushort)(vector * 4 + 2));
    }

    private UnsupportedInstructionException Unsupported(string reason)
    {
        int len = (ushort)(IP - _instrIp);
        var bytes = new byte[Math.Max(len, 1)];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = Read8(_instrCs, (ushort)(_instrIp + i));
        // Leave CS:IP at the faulting instruction so the state stays inspectable (FR-037).
        Segs[CS] = _instrCs;
        IP = _instrIp;
        return new UnsupportedInstructionException(_instrCs, _instrIp, bytes, reason);
    }

    // ---------------------------------------------------------------- execution

    /// <summary>Executes exactly one instruction (including its prefixes).</summary>
    public void Step()
    {
        _instrCs = Segs[CS];
        _instrIp = IP;
        _segOverride = -1;

        byte op;
        while (true)
        {
            op = Fetch8();
            switch (op)
            {
                case 0x26: _segOverride = ES; continue;
                case 0x2E: _segOverride = CS; continue;
                case 0x36: _segOverride = SS; continue;
                case 0x3E: _segOverride = DS; continue;
                case 0xF2: case 0xF3: continue; // REP has no effect on the instructions in this subset
            }
            break;
        }

        switch (op)
        {
            case 0x00: case 0x01: case 0x02: case 0x03: AluRm(AluOp.Add, (op & 1) != 0, (op & 2) != 0); break;
            case 0x04: case 0x05: AluAcc(AluOp.Add, op == 0x05); break;
            case 0x28: case 0x29: case 0x2A: case 0x2B: AluRm(AluOp.Sub, (op & 1) != 0, (op & 2) != 0); break;
            case 0x2C: case 0x2D: AluAcc(AluOp.Sub, op == 0x2D); break;
            case 0x38: case 0x39: case 0x3A: case 0x3B: AluRm(AluOp.Cmp, (op & 1) != 0, (op & 2) != 0); break;
            case 0x3C: case 0x3D: AluAcc(AluOp.Cmp, op == 0x3D); break;

            case >= 0x50 and <= 0x57:
            {
                int r = op & 7;
                Regs[SP] -= 2;                         // 8086: PUSH SP pushes the decremented value
                Write16(Segs[SS], Regs[SP], Regs[r]);
                break;
            }
            case >= 0x58 and <= 0x5F: Regs[op & 7] = Pop(); break;

            case >= 0x70 and <= 0x7F:
            {
                sbyte rel = (sbyte)Fetch8();
                if (Condition(op & 0x0F)) IP = (ushort)(IP + rel);
                break;
            }

            case 0x80: case 0x81: case 0x82: case 0x83: AluGroup(op); break;

            case 0x88: case 0x89: case 0x8A: case 0x8B:
            {
                bool word = (op & 1) != 0;
                var m = DecodeModRm();
                if ((op & 2) != 0) WriteReg(m.Reg, word, ReadRm(m, word));
                else WriteRm(m, word, ReadReg(m.Reg, word));
                break;
            }

            case >= 0xB0 and <= 0xB7: SetReg8(op & 7, Fetch8()); break;
            case >= 0xB8 and <= 0xBF: Regs[op & 7] = Fetch16(); break;

            case 0xC3: IP = Pop(); break;

            case 0xC6: case 0xC7:
            {
                bool word = op == 0xC7;
                var m = DecodeModRm();
                ushort imm = word ? Fetch16() : Fetch8();
                WriteRm(m, word, imm);                  // reg field ignored: SingleStepTests /0-/7 all behave as /0 [Measured, POC-001]
                break;
            }

            case 0xCD: Interrupt(Fetch8()); break;

            case 0xE8:
            {
                ushort rel = Fetch16();
                Push(IP);
                IP = (ushort)(IP + rel);
                break;
            }
            case 0xE9: { ushort rel = Fetch16(); IP = (ushort)(IP + rel); break; }
            case 0xEB: { sbyte rel = (sbyte)Fetch8(); IP = (ushort)(IP + rel); break; }

            default: throw Unsupported($"opcode {op:X2} is outside the POC subset");
        }
        InstructionCount++;
    }
}
