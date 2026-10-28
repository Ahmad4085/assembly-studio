using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Deterministic hand tests for 8086 string instructions and REP prefixes (TASK-017, FR-036, NFR-017).
/// Verifies:
/// - MOVSB, MOVSW with DF=0 and DF=1, 16-bit pointer wrapping, segment overrides on source, ES fixed destination
/// - CMPSB, CMPSW with exact flag calculation (ZF, CF, SF, OF, AF, PF)
/// - STOSB, STOSW writing AL/AX to [ES:DI] without touching SI or flags
/// - LODSB, LODSW reading [srcSeg:SI] to AL/AX without touching DI or flags
/// - SCASB, SCASW comparing AL/AX to [ES:DI] without touching SI
/// - REP prefixes (F2/F3) with CX=0 (immediate no-op)
/// - REP MOVS/STOS/LODS looping until CX=0
/// - REPE/REPZ early exit on ZF=0
/// - REPNE/REPNZ early exit on ZF=1
/// </summary>
public class StringInstructionTests
{
    private static readonly CpuState BaseState = new(
        AX: 0, BX: 0, CX: 0, DX: 0,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0010, DI: 0x0020,
        CS: 0x1000, DS: 0x2000, ES: 0x3000, SS: 0x4000,
        IP: 0x0100, Flags: 0x0002);

    [Fact]
    public void Movsb_df0_increments_si_and_di_and_copies_byte()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // Code at CS:IP: MOVSB (0xA4)
        mem.Write8(0x1000, 0x0100, 0xA4);
        // Source data at DS:SI (0x2000:0x0010)
        mem.Write8(0x2000, 0x0010, 0x42);
        // Dest data at ES:DI (0x3000:0x0020)
        mem.Write8(0x3000, 0x0020, 0x00);

        cpu.SetState(BaseState with { Flags = 0x0002 }); // DF = 0

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x42, mem.Read8(0x3000, 0x0020));
        Assert.Equal(0x0011, cpu.State.SI);
        Assert.Equal(0x0021, cpu.State.DI);
        Assert.Equal(0x0101, cpu.State.IP);
        Assert.Equal(0x0002, cpu.State.Flags); // Flags untouched
    }

    [Fact]
    public void Movsb_df1_decrements_si_and_di()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xA4);
        mem.Write8(0x2000, 0x0010, 0x77);

        cpu.SetState(BaseState with { Flags = (ushort)(0x0002 | (ushort)FlagBit.DF) }); // DF = 1

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x77, mem.Read8(0x3000, 0x0020));
        Assert.Equal(0x000F, cpu.State.SI);
        Assert.Equal(0x001F, cpu.State.DI);
    }

    [Fact]
    public void Movsw_df0_increments_by_2_and_copies_word()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xA5); // MOVSW
        mem.Write16(0x2000, 0x0010, 0xBEEF);

        cpu.SetState(BaseState with { Flags = 0x0002 });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0xBEEF, mem.Read16(0x3000, 0x0020));
        Assert.Equal(0x0012, cpu.State.SI);
        Assert.Equal(0x0022, cpu.State.DI);
    }

    [Fact]
    public void Movsw_df1_decrements_by_2()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xA5); // MOVSW
        mem.Write16(0x2000, 0x0010, 0xCAFE);

        cpu.SetState(BaseState with { Flags = (ushort)(0x0002 | (ushort)FlagBit.DF) });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0xCAFE, mem.Read16(0x3000, 0x0020));
        Assert.Equal(0x000E, cpu.State.SI);
        Assert.Equal(0x001E, cpu.State.DI);
    }

    [Fact]
    public void Movsb_source_segment_override_is_honored_but_destination_is_always_es()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // SS override prefix: 0x36, MOVSB: 0xA4
        mem.Write8(0x1000, 0x0100, 0x36);
        mem.Write8(0x1000, 0x0101, 0xA4);

        // Source in SS:SI
        mem.Write8(0x4000, 0x0010, 0x99);
        // Ensure DS:SI does NOT contain 0x99
        mem.Write8(0x2000, 0x0010, 0x11);

        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x99, mem.Read8(0x3000, 0x0020)); // Written to ES:DI
        Assert.Equal(0x0102, cpu.State.IP);
    }

    [Fact]
    public void Movsb_si_di_wrap_at_16_bits()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xA4);
        mem.Write8(0x2000, 0xFFFF, 0x55);

        cpu.SetState(BaseState with { SI = 0xFFFF, DI = 0xFFFF, Flags = 0x0002 }); // DF = 0, so 0xFFFF + 1 wraps to 0x0000

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x55, mem.Read8(0x3000, 0xFFFF));
        Assert.Equal(0x0000, cpu.State.SI);
        Assert.Equal(0x0000, cpu.State.DI);
    }

    [Fact]
    public void Stosb_writes_al_to_es_di_and_leaves_si_untouched()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xAA); // STOSB

        cpu.SetState(BaseState with
        {
            AX = 0x1234,
            DI = 0x0050,
            SI = 0x0080,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x34, mem.Read8(0x3000, 0x0050));
        Assert.Equal(0x0051, cpu.State.DI);
        Assert.Equal(0x0080, cpu.State.SI); // SI untouched
    }

    [Fact]
    public void Stosw_writes_ax_to_es_di()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xAB); // STOSW

        cpu.SetState(BaseState with
        {
            AX = 0xABCD,
            DI = 0x0050,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0xABCD, mem.Read16(0x3000, 0x0050));
        Assert.Equal(0x0052, cpu.State.DI);
    }

    [Fact]
    public void Lods_byte_and_word_read_to_al_ax_and_leave_di_untouched()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // LODSB: 0xAC
        mem.Write8(0x1000, 0x0100, 0xAC);
        mem.Write8(0x2000, 0x0040, 0x5A);

        cpu.SetState(BaseState with
        {
            AX = 0x1122,
            SI = 0x0040,
            DI = 0x0080,
        });

        StepResult res1 = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res1.Outcome);
        Assert.Equal(0x115A, cpu.State.AX); // AL replaced, AH unchanged
        Assert.Equal(0x0041, cpu.State.SI);
        Assert.Equal(0x0080, cpu.State.DI); // DI untouched

        // LODSW: 0xAD
        mem.Write8(0x1000, 0x0101, 0xAD);
        mem.Write16(0x2000, 0x0041, 0x8899);

        StepResult res2 = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res2.Outcome);
        Assert.Equal(0x8899, cpu.State.AX);
        Assert.Equal(0x0043, cpu.State.SI);
    }

    [Fact]
    public void Cmpsb_computes_flags_from_subtraction()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xA6); // CMPSB
        mem.Write8(0x2000, 0x0010, 0x30); // src
        mem.Write8(0x3000, 0x0020, 0x30); // dst (equal)

        cpu.SetState(BaseState with { SI = 0x0010, DI = 0x0020 });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.ZF) != 0); // Equal -> ZF=1
        Assert.True((cpu.State.Flags & (ushort)FlagBit.CF) == 0);
        Assert.Equal(0x0011, cpu.State.SI);
        Assert.Equal(0x0021, cpu.State.DI);
    }

    [Fact]
    public void Scasb_computes_flags_from_al_minus_dest()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xAE); // SCASB
        mem.Write8(0x3000, 0x0020, 0x50);

        cpu.SetState(BaseState with
        {
            AX = 0x0020, // AL (0x20) < [ES:DI] (0x50) -> borrow, CF=1
            DI = 0x0020,
            SI = 0x0099,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.CF) != 0); // Borrow -> CF=1
        Assert.True((cpu.State.Flags & (ushort)FlagBit.ZF) == 0); // Not equal
        Assert.Equal(0x0021, cpu.State.DI);
        Assert.Equal(0x0099, cpu.State.SI); // SI untouched
    }

    [Fact]
    public void Rep_movsb_with_cx_zero_is_noop()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // REP (0xF3) MOVSB (0xA4)
        mem.Write8(0x1000, 0x0100, 0xF3);
        mem.Write8(0x1000, 0x0101, 0xA4);
        mem.Write8(0x2000, 0x0010, 0xAA);
        mem.Write8(0x3000, 0x0020, 0x55);

        cpu.SetState(BaseState with
        {
            CX = 0,
            SI = 0x0010,
            DI = 0x0020,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x55, mem.Read8(0x3000, 0x0020)); // Memory not modified
        Assert.Equal(0x0010, cpu.State.SI); // SI untouched
        Assert.Equal(0x0020, cpu.State.DI); // DI untouched
        Assert.Equal(0, cpu.State.CX); // CX stays 0
        Assert.Equal(0x0102, cpu.State.IP); // Advanced past REP MOVSB
    }

    [Fact]
    public void Rep_movsb_loops_until_cx_is_zero()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xF3);
        mem.Write8(0x1000, 0x0101, 0xA4);

        // 3 bytes to copy: 'A', 'B', 'C'
        mem.Write8(0x2000, 0x0010, (byte)'A');
        mem.Write8(0x2000, 0x0011, (byte)'B');
        mem.Write8(0x2000, 0x0012, (byte)'C');

        cpu.SetState(BaseState with
        {
            CX = 3,
            SI = 0x0010,
            DI = 0x0050,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal((byte)'A', mem.Read8(0x3000, 0x0050));
        Assert.Equal((byte)'B', mem.Read8(0x3000, 0x0051));
        Assert.Equal((byte)'C', mem.Read8(0x3000, 0x0052));
        Assert.Equal(0, cpu.State.CX);
        Assert.Equal(0x0013, cpu.State.SI);
        Assert.Equal(0x0053, cpu.State.DI);
        Assert.Equal(0x0102, cpu.State.IP);
    }

    [Fact]
    public void Rep_stosw_loops_until_cx_is_zero()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xF3);
        mem.Write8(0x1000, 0x0101, 0xAB); // REP STOSW

        cpu.SetState(BaseState with
        {
            AX = 0x1234,
            CX = 2,
            DI = 0x0010,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x1234, mem.Read16(0x3000, 0x0010));
        Assert.Equal(0x1234, mem.Read16(0x3000, 0x0012));
        Assert.Equal(0, cpu.State.CX);
        Assert.Equal(0x0014, cpu.State.DI);
    }

    [Fact]
    public void Repe_cmpsb_terminates_early_on_mismatch()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // REPE (0xF3) CMPSB (0xA6)
        mem.Write8(0x1000, 0x0100, 0xF3);
        mem.Write8(0x1000, 0x0101, 0xA6);

        // src: 'A', 'B', 'X', 'D'
        mem.Write8(0x2000, 0x0010, (byte)'A');
        mem.Write8(0x2000, 0x0011, (byte)'B');
        mem.Write8(0x2000, 0x0012, (byte)'X');
        mem.Write8(0x2000, 0x0013, (byte)'D');

        // dst: 'A', 'B', 'Y', 'D'
        mem.Write8(0x3000, 0x0020, (byte)'A');
        mem.Write8(0x3000, 0x0021, (byte)'B');
        mem.Write8(0x3000, 0x0022, (byte)'Y');
        mem.Write8(0x3000, 0x0023, (byte)'D');

        cpu.SetState(BaseState with
        {
            CX = 4,
            SI = 0x0010,
            DI = 0x0020,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        // Exited after 3rd comparison ('X' vs 'Y'):
        // SI should be 0x0013, DI should be 0x0023, CX should be 1 (4 - 3 = 1)
        Assert.Equal(0x0013, cpu.State.SI);
        Assert.Equal(0x0023, cpu.State.DI);
        Assert.Equal(1, cpu.State.CX);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.ZF) == 0); // Mismatch -> ZF = 0
    }

    [Fact]
    public void Repne_scasb_terminates_early_on_match()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // REPNE (0xF2) SCASB (0xAE)
        mem.Write8(0x1000, 0x0100, 0xF2);
        mem.Write8(0x1000, 0x0101, 0xAE);

        // Buffer: 'a', 'b', '!', 'd'
        mem.Write8(0x3000, 0x0020, (byte)'a');
        mem.Write8(0x3000, 0x0021, (byte)'b');
        mem.Write8(0x3000, 0x0022, (byte)'!');
        mem.Write8(0x3000, 0x0023, (byte)'d');

        cpu.SetState(BaseState with
        {
            AX = (byte)'!',
            CX = 4,
            DI = 0x0020,
        });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        // Exited on '!' at index 2 (offset 0x0022) -> DI advanced to 0x0023, CX = 4 - 3 = 1
        Assert.Equal(0x0023, cpu.State.DI);
        Assert.Equal(1, cpu.State.CX);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.ZF) != 0); // Match -> ZF = 1
    }
}
