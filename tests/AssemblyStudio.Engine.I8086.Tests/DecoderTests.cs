using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using Xunit;
using Decoder = AssemblyStudio.Engine.I8086.Decoder;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Hand tests for ModR/M effective-address calculation and fetch wraps (TASK-011 test
/// requirements; Intel 8086 reference manual, ModR/M addressing table).
/// </summary>
public class DecoderTests
{
    private static readonly Core.CpuState Base = new(
        AX: 0, BX: 0x1000, CX: 0, DX: 0,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0200);

    private static ModRmInfo Decode(byte modRm, SegReg? @override = null, params byte[] displacement) =>
        Decoder.DecodeModRm(modRm, Base, @override, displacement);

    /// <summary>Builds a FakeMemory with instruction bytes starting at Base.CS:Base.IP (0x0800:0x0100).</summary>
    private static FakeMemory MemoryWithInstructionAtIp(params byte[] bytes)
    {
        var memory = new FakeMemory();
        int physical = 0x08000 + 0x0100; // CS 0x0800 (x16 = 0x8000) + IP 0x0100
        foreach (var b in bytes)
        {
            memory[physical & (FakeMemory.Size - 1)] = b;
            physical++;
        }
        return memory;
    }

    // ---- mod 00: base/index combinations and their default segments --------------------

    [Theory]
    [InlineData(0, 0x1100u, 0x0A00u)] // [BX+SI]    DS
    [InlineData(1, 0x1200u, 0x0A00u)] // [BX+DI]    DS
    [InlineData(2, 0x2100u, 0x0C00u)] // [BP+SI]    SS
    [InlineData(3, 0x2200u, 0x0C00u)] // [BP+DI]    SS
    [InlineData(4, 0x0100u, 0x0A00u)] // [SI]       DS
    [InlineData(5, 0x0200u, 0x0A00u)] // [DI]       DS
    [InlineData(7, 0x1000u, 0x0A00u)] // [BX]       DS
    public void Mod00_rm_combinations_use_the_default_segment(int rm, ushort expectedOffset, ushort expectedSegment)
    {
        var m = Decode((byte)(rm));

        Assert.True(m.IsMemory);
        Assert.Equal(0, m.Mod);
        Assert.Equal(expectedOffset, m.EaOffset);
        Assert.Equal(expectedSegment, m.EaSegment);
    }

    [Fact]
    public void Mod00_rm6_is_a_direct_16_bit_displacement_in_ds()
    {
        // mod 00, rm 110: direct displacement (no BP), DS. Byte: 00 000 110 = 0x06.
        var m = Decode(0x06, displacement: [0x34, 0x12]);

        Assert.True(m.IsMemory);
        Assert.Equal(0x1234, m.EaOffset);
        Assert.Equal(0x0A00u, (uint)m.EaSegment);
    }

    // ---- mod 01: 8-bit displacement, sign-extended ------------------------------------

    [Fact]
    public void Mod01_adds_sign_extended_disp8()
    {
        // [BP+SI+disp8], SS default.
        var m = Decode(0x52, displacement: [0x10]);

        Assert.Equal(0x2110, m.EaOffset);
        Assert.Equal(0x0C00u, (uint)m.EaSegment);
    }

    [Fact]
    public void Mod01_negative_disp8_wraps_at_16_bits()
    {
        // [BX+disp8] with disp8 = -1 (0xFF): 0x1000 - 1 = 0x0FFF.
        var m = Decode(0x47, displacement: [0xFF]);

        Assert.Equal(0x0FFF, m.EaOffset);
    }

    [Fact]
    public void Mod01_disp8_min_wraps()
    {
        // [SI-128] (disp8 = 0x80): 0x0100 - 128 = 0x0080.
        var m = Decode(0x44, displacement: [0x80]);

        Assert.Equal(0x0080, m.EaOffset);
    }

    [Fact]
    public void Mod01_rm6_is_bp_plus_disp8_in_ss()
    {
        // mod 01, rm 110: [BP+disp8], SS.
        var m = Decode(0x46, displacement: [0x08]);

        Assert.Equal(0x2008, m.EaOffset);
        Assert.Equal(0x0C00u, (uint)m.EaSegment);
    }

    // ---- mod 10: 16-bit displacement --------------------------------------------------

    [Fact]
    public void Mod10_adds_disp16()
    {
        // [BX+disp16], DS.
        var m = Decode(0x87, displacement: [0x34, 0x12]);

        Assert.Equal(0x1000 + 0x1234, m.EaOffset);
        Assert.Equal(0x0A00u, (uint)m.EaSegment);
    }

    [Fact]
    public void Mod10_disp16_wraps_at_16_bits()
    {
        // [BX+SI+disp16] where the sum overflows: 0x1000 + 0x0100 + 0xF000 = 0x0100 (mod 0x10000).
        var m = Decode(0x80, displacement: [0x00, 0xF0]);

        Assert.Equal(0x0100, m.EaOffset);
    }

    [Fact]
    public void Mod10_rm6_is_bp_plus_disp16_in_ss()
    {
        var m = Decode(0x86, displacement: [0x00, 0x10]);

        Assert.Equal(0x2000 + 0x1000, m.EaOffset);
        Assert.Equal(0x0C00u, (uint)m.EaSegment);
    }

    // ---- segment overrides ------------------------------------------------------------

    [Theory]
    [InlineData(0x3E, 0x0A00u)] // DS:
    [InlineData(0x36, 0x0C00u)] // SS:
    [InlineData(0x26, 0x0B00u)] // ES:
    [InlineData(0x2E, 0x0800u)] // CS:
    public void Segment_override_replaces_the_default_segment(byte overridePrefix, ushort expectedSegment)
    {
        var memory = MemoryWithInstructionAtIp(overridePrefix, 0x00, 0x80, 0x34, 0x12);

        var prefix = Decoder.ParsePrefixes(Base, memory);

        // Prefix parsing consumed the override; decode ModR/M with it.
        var m = Decoder.DecodeModRm(0x80, Base, prefix.SegmentOverride, [0x34, 0x12]);

        Assert.Equal(expectedSegment, m.EaSegment);
    }

    // ---- mod 11: register operand -----------------------------------------------------

    [Fact]
    public void Mod11_is_a_register_operand()
    {
        var m = Decode(0xC8);

        Assert.False(m.IsMemory);
        Assert.Equal(3, m.Mod);
        Assert.Equal(1, m.Reg);
        Assert.Equal(0, m.Rm);
    }

    // ---- fetch wrap (16-bit IP) ---------------------------------------------------------

    [Fact]
    public void Prefix_parsing_wraps_the_fetch_at_ip_0xffff()
    {
        // Opcode CD at CS:IP 0xFFFF (physical 0x17FFF: CS 0x0800 x 16 = 0x8000, + 0xFFFF); its
        // imm8 is fetched from 0x0000 (physical 0x8000).
        var memory = new FakeMemory();
        memory[0x17FFF] = 0xCD;
        memory[0x08000] = 0x21;

        var prefix = Decoder.ParsePrefixes(Base with { IP = 0xFFFF }, memory);

        Assert.Equal(0xCD, prefix.Opcode);
        Assert.Equal(0xFFFF, prefix.OpcodeIp);
    }
}
