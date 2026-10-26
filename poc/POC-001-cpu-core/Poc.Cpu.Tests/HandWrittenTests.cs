using Xunit;

namespace Poc.Cpu.Tests;

/// <summary>Spec-sourced cases from docs/sdlc/guides/cpu-instruction-testing.md, independent of the oracle data.</summary>
public class HandWrittenTests
{
    private static Cpu8086 CpuWithCode(params byte[] code)
    {
        var cpu = new Cpu8086();
        cpu.Segs[Cpu8086.CS] = cpu.Segs[Cpu8086.DS] = cpu.Segs[Cpu8086.ES] = cpu.Segs[Cpu8086.SS] = 0x1000;
        cpu.IP = 0x0100;
        cpu.Regs[Cpu8086.SP] = 0xFFFE;
        cpu.Flags = 0xF002; // 8086: bits 12-15 and bit 1 read as 1
        code.CopyTo(cpu.Memory, Cpu8086.Phys(0x1000, 0x0100));
        return cpu;
    }

    [Fact]
    public void Add_al_bl_7F_plus_01_sets_overflow_sign_and_aux_carry()   // AC-025 example
    {
        var cpu = CpuWithCode(0x00, 0xD8);                               // ADD AL, BL
        cpu.Regs[Cpu8086.AX] = 0x007F;
        cpu.Regs[Cpu8086.BX] = 0x0001;
        var before = cpu.Snapshot();

        cpu.Step();

        var after = cpu.Snapshot();
        Assert.Equal(0x0080, after.AX);
        var f = (FlagBit)after.Flags;
        Assert.True(f.HasFlag(FlagBit.OF));
        Assert.True(f.HasFlag(FlagBit.SF));
        Assert.True(f.HasFlag(FlagBit.AF));
        Assert.False(f.HasFlag(FlagBit.ZF));
        Assert.False(f.HasFlag(FlagBit.PF));                             // 0x80 has one set bit: odd parity
        Assert.False(f.HasFlag(FlagBit.CF));
        Assert.Equal(before with { AX = 0x0080, IP = 0x0102, Flags = after.Flags }, after); // nothing else changed
    }

    [Fact]
    public void Push_ax_writes_little_endian_word_below_sp()               // AC-026 example
    {
        var cpu = CpuWithCode(0x50);                                       // PUSH AX
        cpu.Regs[Cpu8086.AX] = 0x1234;

        cpu.Step();

        Assert.Equal(0xFFFC, cpu.Regs[Cpu8086.SP]);
        Assert.Equal(0x34, cpu.Read8(0x1000, 0xFFFC));
        Assert.Equal(0x12, cpu.Read8(0x1000, 0xFFFD));
    }

    [Fact]
    public void Unsupported_opcode_halts_with_location_and_bytes_and_keeps_state()   // FR-037, AC-028
    {
        var cpu = CpuWithCode(0x0F, 0xA2);                                 // not an 8086 instruction in this subset
        var before = cpu.Snapshot();

        var ex = Assert.Throws<UnsupportedInstructionException>(cpu.Step);

        Assert.Equal(0x1000, ex.Cs);
        Assert.Equal(0x0100, ex.Ip);
        Assert.Equal(new byte[] { 0x0F }, ex.Bytes);
        Assert.Equal(before, cpu.Snapshot());                              // state preserved for inspection
    }

    [Fact]
    public void Int_with_host_hook_is_handled_without_vectoring()          // DOS layer integration point
    {
        var cpu = CpuWithCode(0xCD, 0x21);                                 // INT 21h
        byte? seen = null;
        cpu.InterruptHook = (_, v) => { seen = v; return true; };

        cpu.Step();

        Assert.Equal((byte)0x21, seen);
        Assert.Equal(0x0102, cpu.IP);
        Assert.Equal(0xFFFE, cpu.Regs[Cpu8086.SP]);                        // nothing pushed
    }

    [Fact]
    public void Same_program_and_state_produce_identical_traces()          // NFR-016
    {
        byte[] loop = [0x05, 0x01, 0x00, 0x3B, 0xC3, 0x75, 0xF9];        // ADD AX,1; CMP AX,BX; JNE -7
        var traces = new List<CpuRegisters>[2];
        for (int run = 0; run < 2; run++)
        {
            var cpu = CpuWithCode(loop);
            cpu.Regs[Cpu8086.BX] = 50;
            traces[run] = [];
            for (int i = 0; i < 150; i++) { cpu.Step(); traces[run].Add(cpu.Snapshot()); }
        }
        Assert.Equal(traces[0], traces[1]);
    }
}
