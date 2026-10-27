namespace AssemblyStudio.Engine.I8086;

/// <summary>16-bit segment register, in the order of <see cref="Core.CpuState"/>'s segment fields.</summary>
internal enum SegReg : byte
{
    ES = 0,
    CS = 1,
    SS = 2,
    DS = 3,
}

/// <summary>REP prefix captured during decode (used by the string instructions, TASK-017).</summary>
internal enum RepKind : byte
{
    None = 0,
    /// <summary>F3 (REP / REPE).</summary>
    Rep,
    /// <summary>F2 (REPNE).</summary>
    Repne,
}

/// <summary>Result of parsing the instruction prefixes; <see cref="Opcode"/> terminates the prefix run.</summary>
/// <param name="Opcode">The opcode byte at <see cref="OpcodeIp"/> (prefixes excluded).</param>
/// <param name="OpcodeIp">IP of the opcode byte.</param>
/// <param name="SegmentOverride">A segment override prefix, or <see langword="null"/> (last one wins).</param>
/// <param name="Rep">A captured REP prefix, or <see cref="RepKind.None"/>.</param>
internal readonly record struct PrefixInfo(
    byte Opcode,
    ushort OpcodeIp,
    SegReg? SegmentOverride,
    RepKind Rep);

/// <summary>Decoded ModR/M operand (8086 table). <see cref="EaOffset"/> and <see cref="EaSegment"/>
/// are meaningful only when <see cref="IsMemory"/> (i.e. <c>mod != 11b</c>).</summary>
/// <param name="Mod">mod field.</param>
/// <param name="Reg">reg field.</param>
/// <param name="Rm">rm field.</param>
/// <param name="IsMemory"><see langword="true"/> when the rm operand is in memory (mod != 11b).</param>
/// <param name="EaOffset">16-bit effective address (wrapped).</param>
/// <param name="EaSegment">The segment register <i>value</i> the access uses: the default segment
/// for this rm/mod, or the override value when a segment override prefix is active.</param>
internal readonly record struct ModRmInfo(
    int Mod,
    int Reg,
    int Rm,
    bool IsMemory,
    ushort EaOffset,
    ushort EaSegment);

/// <summary>
/// Pure instruction-stream decoding (ADR-0003 §1): reads through <see cref="Core.IMemory"/> at a
/// 16-bit wrapping cursor and computes results without touching CPU state. Semantics follow the
/// Intel 8086 reference manual; validated against SingleStepTests in TASK-011..020.
/// </summary>
internal static class Decoder
{
    /// <summary>
    /// Parses prefixes and the opcode byte at <paramref name="state"/>'s CS:IP. Fetches wrap at
    /// 16 bits (IP is 16-bit, ADR-0003 / TASK-011 scope). <c>LOCK</c> (F0) is ignored (ADR-0003 §4);
    /// <c>F1</c> is not a valid 8086 prefix and is returned as the opcode (unsupported).
    /// </summary>
    public static PrefixInfo ParsePrefixes(Core.CpuState state, Core.IMemory memory)
    {
        ushort cursor = state.IP;
        SegReg? segmentOverride = null;
        RepKind rep = RepKind.None;

        for (; ; )
        {
            byte b = memory.Read8(state.CS, cursor);
            switch (b)
            {
                case 0x26: segmentOverride = SegReg.ES; break;
                case 0x2E: segmentOverride = SegReg.CS; break;
                case 0x36: segmentOverride = SegReg.SS; break;
                case 0x3E: segmentOverride = SegReg.DS; break;
                case 0xF0: break; // LOCK: ignored, ADR-0003 §4
                case 0xF2: rep = RepKind.Repne; break;
                case 0xF3: rep = RepKind.Rep; break;
                default:
                    return new PrefixInfo(b, cursor, segmentOverride, rep);
            }
            cursor = (ushort)(cursor + 1);
        }
    }

    /// <summary>
    /// Decodes one ModR/M operand per the 8086 table.
    /// </summary>
    /// <param name="modRmByte">The ModR/M byte.</param>
    /// <param name="state">CPU state supplying the base/index registers and segment values.</param>
    /// <param name="segmentOverride">Active segment override prefix, or <see langword="null"/>.</param>
    /// <param name="displacementBytes">Instruction-stream bytes following the ModR/M byte (up to 2
    /// are consumed; fewer is fine when the form has no displacement).</param>
    public static ModRmInfo DecodeModRm(
        byte modRmByte,
        Core.CpuState state,
        SegReg? segmentOverride,
        ReadOnlySpan<byte> displacementBytes)
    {
        int mod = modRmByte >> 6;
        int reg = (modRmByte >> 3) & 7;
        int rm = modRmByte & 7;

        if (mod == 3)
        {
            return new ModRmInfo(mod, reg, rm, IsMemory: false, 0, 0);
        }

        ushort ea;
        SegReg defaultSeg;
        switch (rm)
        {
            case 0: ea = (ushort)(state.BX + state.SI); defaultSeg = SegReg.DS; break;
            case 1: ea = (ushort)(state.BX + state.DI); defaultSeg = SegReg.DS; break;
            case 2: ea = (ushort)(state.BP + state.SI); defaultSeg = SegReg.SS; break;
            case 3: ea = (ushort)(state.BP + state.DI); defaultSeg = SegReg.SS; break;
            case 4: ea = state.SI; defaultSeg = SegReg.DS; break;
            case 5: ea = state.DI; defaultSeg = SegReg.DS; break;
            case 6:
                if (mod == 0)
                {
                    // mod 00, rm 110: direct 16-bit displacement, DS.
                    ea = displacementBytes.Length >= 2
                        ? (ushort)(displacementBytes[0] | (displacementBytes[1] << 8))
                        : default;
                    defaultSeg = SegReg.DS;
                }
                else
                {
                    ea = state.BP;
                    defaultSeg = SegReg.SS;
                }
                break;
            default: ea = state.BX; defaultSeg = SegReg.DS; break;
        }

        if (mod == 1)
        {
            ea = (ushort)(ea + (sbyte)displacementBytes[0]);
        }
        else if (mod == 2)
        {
            ea = displacementBytes.Length >= 2
                ? (ushort)(ea + (displacementBytes[0] | (displacementBytes[1] << 8)))
                : ea;
        }

        ushort segmentValue = segmentOverride.HasValue
            ? SegValue(state, segmentOverride.Value)
            : SegValue(state, defaultSeg);

        return new ModRmInfo(mod, reg, rm, IsMemory: true, ea, segmentValue);
    }

    private static ushort SegValue(Core.CpuState state, SegReg seg) => seg switch
    {
        SegReg.ES => state.ES,
        SegReg.CS => state.CS,
        SegReg.SS => state.SS,
        _ => state.DS,
    };
}
