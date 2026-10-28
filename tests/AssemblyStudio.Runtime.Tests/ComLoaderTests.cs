using System.Text;
using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Loading;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Runtime.Tests;

public class ComLoaderTests
{
    private sealed class MockCpu : ICpu
    {
        public CpuState State { get; set; }
        public void SetState(CpuState state) => State = state;
        public StepResult StepInstruction() =>
            new(StepOutcome.Executed, State.CS, State.IP, HaltReason.None, FlagBit.None);
    }

    [Fact]
    public void AC_021_Program_loaded_with_documented_initial_state()
    {
        var mem = new RealModeMemory();
        var cpu = new MockCpu();
        var machine = new Machine(cpu, mem);

        byte[] payload = [0x90, 0xB8, 0x01, 0x00, 0xCD, 0x21]; // NOP; MOV AX, 1; INT 21h
        var result = ComLoader.Load(machine, payload);

        // 1. Program segment and entry point
        Assert.Equal(0x0800, result.ProgramSegment);
        Assert.Equal(0x0800, result.EntryCs);
        Assert.Equal(0x0100, result.EntryIp);
        Assert.Equal(payload.Length, result.ImageSize);

        // 2. Registers
        var s = cpu.State;
        Assert.Equal(0x0800, s.CS);
        Assert.Equal(0x0800, s.DS);
        Assert.Equal(0x0800, s.ES);
        Assert.Equal(0x0800, s.SS);
        Assert.Equal(0x0100, s.IP);
        Assert.Equal(0xFFFE, s.SP);

        Assert.Equal(0x0000, s.AX);
        Assert.Equal(0x0000, s.BX);
        Assert.Equal(0x0000, s.CX);
        Assert.Equal(0x0000, s.DX);
        Assert.Equal(0x0000, s.SI);
        Assert.Equal(0x0000, s.DI);
        Assert.Equal(0x0000, s.BP);
        Assert.Equal(0x0002, s.Flags); // 8086 reserved bit 1 set

        // 3. Image bytes at PSP:0100h
        for (int i = 0; i < payload.Length; i++)
        {
            Assert.Equal(payload[i], mem.Read8(0x0800, (ushort)(0x0100 + i)));
        }

        // 4. Word 0000h at SS:SP (0x0800:FFFEh)
        Assert.Equal(0x0000, mem.Read16(0x0800, 0xFFFE));

        // 5. Memory journal is clean for execution (FR-056)
        Assert.Empty(mem.CurrentJournal);
    }

    [Fact]
    public void PSP_byte_level_tests_against_research_table()
    {
        var mem = new RealModeMemory();
        ComLoader.Load(mem, cpu: null, image: [0x90]);

        const ushort psp = 0x0800;

        // 00h: CD 20 (INT 20h)
        Assert.Equal(0xCD, mem.Read8(psp, 0x00));
        Assert.Equal(0x20, mem.Read8(psp, 0x01));

        // 02h: Top of memory segment (0x1800 for 64 KiB block)
        Assert.Equal(0x1800, mem.Read16(psp, 0x02));

        // 04h: Reserved (0x00)
        Assert.Equal(0x00, mem.Read8(psp, 0x04));

        // 05h..09h: Far call to PSP:0050h (9A 50 00 00 08)
        Assert.Equal(0x9A, mem.Read8(psp, 0x05));
        Assert.Equal(0x0050, mem.Read16(psp, 0x06));
        Assert.Equal(0x0800, mem.Read16(psp, 0x08));

        // 0Ah..15h: Saved INT 22h, 23h, 24h vectors (0 when IVT unpopulated)
        Assert.Equal(0x0000, mem.Read16(psp, 0x0A));
        Assert.Equal(0x0000, mem.Read16(psp, 0x0C));
        Assert.Equal(0x0000, mem.Read16(psp, 0x0E));
        Assert.Equal(0x0000, mem.Read16(psp, 0x10));
        Assert.Equal(0x0000, mem.Read16(psp, 0x12));
        Assert.Equal(0x0000, mem.Read16(psp, 0x14));

        // 16h: Parent PSP (0x0800)
        Assert.Equal(0x0800, mem.Read16(psp, 0x16));

        // 18h..2Bh: Job file table (20 bytes)
        for (ushort i = 0; i < 5; i++)
        {
            Assert.Equal((byte)i, mem.Read8(psp, (ushort)(0x18 + i)));
        }
        for (ushort i = 5; i < 20; i++)
        {
            Assert.Equal(0xFF, mem.Read8(psp, (ushort)(0x18 + i)));
        }

        // 2Ch: Environment segment (0x0000)
        Assert.Equal(0x0000, mem.Read16(psp, 0x2C));

        // 2Eh..4Fh: Reserved (0x00)
        for (ushort i = 0x2E; i < 0x50; i++)
        {
            Assert.Equal(0x00, mem.Read8(psp, i));
        }

        // 50h..52h: CD 21 CB (INT 21h; RETF)
        Assert.Equal(0xCD, mem.Read8(psp, 0x50));
        Assert.Equal(0x21, mem.Read8(psp, 0x51));
        Assert.Equal(0xCB, mem.Read8(psp, 0x52));

        // 53h..5Bh: Reserved (0x00)
        for (ushort i = 0x53; i < 0x5C; i++)
        {
            Assert.Equal(0x00, mem.Read8(psp, i));
        }

        // 5Ch..6Bh: FCB 1 (zeroed)
        for (ushort i = 0x5C; i < 0x6C; i++)
        {
            Assert.Equal(0x00, mem.Read8(psp, i));
        }

        // 6Ch..7Fh: FCB 2 (zeroed)
        for (ushort i = 0x6C; i < 0x80; i++)
        {
            Assert.Equal(0x00, mem.Read8(psp, i));
        }

        // 80h..FFh: Empty command tail (00 0D followed by zeroes)
        Assert.Equal(0x00, mem.Read8(psp, 0x80));
        Assert.Equal(0x0D, mem.Read8(psp, 0x81));
        for (ushort i = 0x82; i <= 0xFF; i++)
        {
            Assert.Equal(0x00, mem.Read8(psp, i));
        }
    }

    [Fact]
    public void Command_tail_is_written_with_length_and_carriage_return()
    {
        var mem = new RealModeMemory();
        string tail = " FILE.TXT /W";

        ComLoader.Load(mem, cpu: null, image: [0x90], commandTail: tail);

        const ushort psp = 0x0800;
        Assert.Equal(tail.Length, mem.Read8(psp, 0x80));

        for (int i = 0; i < tail.Length; i++)
        {
            Assert.Equal((byte)tail[i], mem.Read8(psp, (ushort)(0x81 + i)));
        }

        Assert.Equal(0x0D, mem.Read8(psp, (ushort)(0x81 + tail.Length)));
        Assert.Equal(0x00, mem.Read8(psp, (ushort)(0x82 + tail.Length)));
    }

    [Fact]
    public void Command_tail_longer_than_126_chars_is_truncated()
    {
        var mem = new RealModeMemory();
        string longTail = new('A', 150);

        ComLoader.Load(mem, cpu: null, image: [0x90], commandTail: longTail);

        const ushort psp = 0x0800;
        Assert.Equal(126, mem.Read8(psp, 0x80));
        Assert.Equal((byte)'A', mem.Read8(psp, 0xFE));
        Assert.Equal(0x0D, mem.Read8(psp, 0xFF));
    }

    [Fact]
    public void Saved_IVT_vectors_copied_into_PSP_when_present()
    {
        var mem = new RealModeMemory();

        // Populate INT 22h, 23h, 24h in IVT
        mem.Write16(0x0000, 0x22 * 4, 0x1111);
        mem.Write16(0x0000, (ushort)(0x22 * 4 + 2), 0x2222);

        mem.Write16(0x0000, 0x23 * 4, 0x3333);
        mem.Write16(0x0000, (ushort)(0x23 * 4 + 2), 0x4444);

        mem.Write16(0x0000, 0x24 * 4, 0x5555);
        mem.Write16(0x0000, (ushort)(0x24 * 4 + 2), 0x6666);

        ComLoader.Load(mem, cpu: null, image: [0x90]);

        const ushort psp = 0x0800;
        Assert.Equal(0x1111, mem.Read16(psp, 0x0A));
        Assert.Equal(0x2222, mem.Read16(psp, 0x0C));
        Assert.Equal(0x3333, mem.Read16(psp, 0x0E));
        Assert.Equal(0x4444, mem.Read16(psp, 0x10));
        Assert.Equal(0x5555, mem.Read16(psp, 0x12));
        Assert.Equal(0x6666, mem.Read16(psp, 0x14));
    }

    [Fact]
    public void Near_RET_using_initial_stack_word_jumps_to_PSP_0000h_INT_20h()
    {
        var mem = new RealModeMemory();
        var cpu = new MockCpu();
        var machine = new Machine(cpu, mem);

        // Near RET opcode: C3
        ComLoader.Load(machine, [0xC3]);

        // Stack has 0000h at SS:SP
        ushort returnIp = mem.Read16(cpu.State.SS, cpu.State.SP);
        Assert.Equal(0x0000, returnIp);

        // At CS:0000h is INT 20h
        Assert.Equal(0xCD, mem.Read8(cpu.State.CS, returnIp));
        Assert.Equal(0x20, mem.Read8(cpu.State.CS, (ushort)(returnIp + 1)));
    }

    [Fact]
    public void Exceeding_max_com_image_size_throws_ArgumentException()
    {
        var mem = new RealModeMemory();
        byte[] oversized = new byte[ComLoader.MaxComImageSize + 1];

        var ex = Assert.Throws<ArgumentException>("image", () => ComLoader.Load(mem, cpu: null, oversized));
        Assert.Contains("exceeds maximum allowable size", ex.Message);
    }

    [Fact]
    public void Exactly_max_com_image_size_succeeds()
    {
        var mem = new RealModeMemory();
        byte[] maxImage = new byte[ComLoader.MaxComImageSize];
        maxImage[0] = 0xAA;
        maxImage[^1] = 0xBB;

        var result = ComLoader.Load(mem, cpu: null, maxImage);

        Assert.Equal(ComLoader.MaxComImageSize, result.ImageSize);
        Assert.Equal(0xAA, mem.Read8(0x0800, 0x0100));
        Assert.Equal(0xBB, mem.Read8(0x0800, 0xFFFD));
        Assert.Equal(0x0000, mem.Read16(0x0800, 0xFFFE));
    }

    [Fact]
    public void Custom_program_segment_configures_state_at_target_segment()
    {
        var mem = new RealModeMemory();
        var cpu = new MockCpu();

        ushort customSeg = 0x1200;
        var result = ComLoader.Load(mem, cpu, [0x90], programSegment: customSeg);

        Assert.Equal(customSeg, result.ProgramSegment);
        Assert.Equal(customSeg, cpu.State.CS);
        Assert.Equal(customSeg, cpu.State.DS);
        Assert.Equal(customSeg, cpu.State.ES);
        Assert.Equal(customSeg, cpu.State.SS);
        Assert.Equal(0x0100, cpu.State.IP);

        // PSP 00h is at customSeg:0000
        Assert.Equal(0xCD, mem.Read8(customSeg, 0x00));
        Assert.Equal(0x20, mem.Read8(customSeg, 0x01));

        // Call 5 targets customSeg:0050h
        Assert.Equal(customSeg, mem.Read16(customSeg, 0x08));
    }
}
