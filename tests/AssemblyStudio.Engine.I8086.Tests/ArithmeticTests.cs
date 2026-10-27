using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Hand tests for the TASK-013 arithmetic instructions at the edges the backlog calls out
/// explicitly (AC-025) and the flag rules the oracle exercises sparsely: ADC/SBB carry chains,
/// INC/DEC leaving CF untouched, NEG of 0 and of the most negative value, CMP discarding the
/// result, 83 sign-extended immediates, and the FR-037 halt with byte-identical state for the
/// FE/FF forms not in scope (cpu-instruction-testing guide: deterministic, full-state assertions).
/// </summary>
public class ArithmeticTests
{
    private static readonly Core.CpuState Initial = new(
        AX: 0x7F01, BX: 0x0180, CX: 0xABCD, DX: 0x0005,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

    /// <summary>Sets one register field of <see cref="Initial"/> without touching anything else.</summary>
    private static CpuState WithReg(ushort ax = 0x7F01, ushort bx = 0x0180, ushort flags = 0x0202) =>
        Initial with { AX = ax, BX = bx, Flags = flags };

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(params byte[] code) => NewCpu(Initial, code);

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(CpuState state, params byte[] code)
    {
        var memory = new FakeMemory();
        int physical = 0x08000 + 0x0100; // CS 0x0800 (x16 = 0x8000) + IP 0x0100
        foreach (var b in code)
        {
            memory[physical & (FakeMemory.Size - 1)] = b;
            physical++;
        }
        var cpu = new Cpu8086(memory);
        cpu.SetState(state);
        return (cpu, memory);
    }

    private static void AssertExecuted(Cpu8086 cpu, ushort expectedEndIp)
    {
        var result = cpu.StepInstruction();
        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(HaltReason.None, result.HaltReason);
        Assert.Equal(0x0100, result.InstructionIp);
        Assert.Equal(expectedEndIp, cpu.State.IP);
    }

    // ---- AC-025: add al, bl with AL=7Fh, BL=01h -> AL=80h, OF/SF/AF set, PF/CF clear ---------------

    [Fact]
    public void Ac_025_add_al_bl_7f_plus_1_shows_signed_overflow()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x007F, bx: 0x0001), 0x00, 0xD8); // ADD AL, BL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0080, cpu.State.AX);
        // 7Fh + 01h = 80h: OF=1 (signed overflow), SF=1, ZF=0, AF=1, PF=0 (odd parity), CF=0.
        Assert.Equal(0x0A92, cpu.State.Flags);
    }

    // ---- ADC / SBB carry and borrow chains -----------------------------------------------------------

    [Fact]
    public void Adc_al_0_with_carry_set_wraps_to_zero_and_carries_out()
    {
        var state = WithReg(ax: 0x00FF, flags: 0x0203); // CF=1
        var (cpu, _) = NewCpu(state, 0x14, 0x00); // ADC AL, 0

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0000, cpu.State.AX);
        // 0xFF + 0 + 1 = 0x100: AL=0, CF=1, ZF=1, AF=1, PF=1, SF=0, OF=0.
        Assert.Equal(0x0257, cpu.State.Flags);
    }

    [Fact]
    public void Sbb_ax_ax_with_carry_set_produces_ffff_and_borrows()
    {
        var state = WithReg(ax: 0x0000, flags: 0x0203); // CF=1
        var (cpu, _) = NewCpu(state, 0x1B, 0xC0); // SBB AX, AX

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0xFFFF, cpu.State.AX);
        // 0 - 0 - 1 = 0xFFFF: CF=1, SF=1, AF=1, PF=1, ZF=0, OF=0.
        Assert.Equal(0x0297, cpu.State.Flags);
    }

    [Fact]
    public void Sbb_without_carry_computes_a_plain_subtraction()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0005, bx: 0x0002), 0x1B, 0xC3); // SBB AX, BX (CF=0)

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0003, cpu.State.AX);
        // 5 - 2 = 3: PF=1 (even parity of 0x03); every other touched flag clear.
        Assert.Equal(0x0206, cpu.State.Flags);
    }

    // ---- INC/DEC: CF passes through untouched (backlog flag rule) -------------------------------------

    [Fact]
    public void Inc_ax_wrapping_to_zero_sets_zf_but_leaves_cf_untouched()
    {
        var state = WithReg(ax: 0xFFFF, flags: 0x0203); // CF=1
        var (cpu, _) = NewCpu(state, 0x40); // INC AX

        AssertExecuted(cpu, 0x0101);

        Assert.Equal(0x0000, cpu.State.AX);
        // ZF=1, AF=1, PF=1; CF stays 1; SF=0, OF=0.
        Assert.Equal(0x0257, cpu.State.Flags);
    }

    [Fact]
    public void Dec_ax_from_8000_sets_of_but_leaves_cf_untouched()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x8000), 0x48); // DEC AX

        AssertExecuted(cpu, 0x0101);

        Assert.Equal(0x7FFF, cpu.State.AX);
        // 0x8000 - 1 = 0x7FFF: OF=1, AF=1, PF=1; CF stays 0.
        Assert.Equal(0x0A16, cpu.State.Flags);
    }

    // ---- NEG -------------------------------------------------------------------------------------------

    [Fact]
    public void Neg_of_zero_clears_cf()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0000), 0xF6, 0xD8); // NEG AL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0000, cpu.State.AX);
        // 0 - 0 = 0: CF=0, ZF=1, PF=1, no others.
        Assert.Equal(0x0246, cpu.State.Flags);
    }

    [Fact]
    public void Neg_of_80h_wraps_and_sets_of()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0080), 0xF6, 0xD8); // NEG AL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0080, cpu.State.AX); // 0x80 negated wraps back to 0x80
        // CF=1, OF=1, SF=1, ZF=0; AF=0 (low nibble of 0x80 is 0), PF=0 (odd parity).
        Assert.Equal(0x0A83, cpu.State.Flags);
    }

    [Fact]
    public void Neg_of_nonzero_clears_cf_and_sets_borrow_flags()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0005), 0xF7, 0xD8); // NEG AX

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0xFFFB, cpu.State.AX);
        // CF=1, SF=1, AF=1 (low-nibble borrow), PF=0 (0xFB has 7 ones -> odd).
        Assert.Equal(0x0293, cpu.State.Flags);
    }

    // ---- CMP discards the result -----------------------------------------------------------------------

    [Fact]
    public void Cmp_ax_bx_leaves_the_operands_untouched()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0005, bx: 0x0005), 0x3B, 0xC3); // CMP AX, BX

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0005, cpu.State.AX);
        Assert.Equal(0x0005, cpu.State.BX);
        Assert.Equal(0x0246, cpu.State.Flags); // equal: ZF=1, PF=1
    }

    [Fact]
    public void Cmp_al_imm_discards_the_result()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x00F6), 0x3C, 0x7F); // CMP AL, 7Fh

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x00F6, cpu.State.AX); // AL untouched
        // 0xF6 - 0x7F = 0x77: signed overflow (OF=1), AF=1, PF=1 (even parity of 0x77); CF=0.
        Assert.Equal(0x0A16, cpu.State.Flags);
    }

    // ---- Immediate groups -------------------------------------------------------------------------------

    [Fact]
    public void Group_83_sign_extends_the_imm8()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0005), 0x83, 0xC0, 0xFF); // ADD AX, -1

        AssertExecuted(cpu, 0x0103);

        Assert.Equal(0x0004, cpu.State.AX); // 5 + (-1) = 4, not 5 + 0xFF
        // AF=1 (low-nibble borrow), PF=0 (0x04 has 1 bit), CF=1 (5 + 0xFFFF carries out), OF=0.
        Assert.Equal(0x0213, cpu.State.Flags);
    }

    [Fact]
    public void Group_83_cmp_compares_against_the_sign_extended_value()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0000), 0x83, 0xF8, 0xFF); // CMP AX, -1

        AssertExecuted(cpu, 0x0103);

        Assert.Equal(0x0000, cpu.State.AX); // CMP leaves the operand
        // 0 - 0xFFFF: CF=1 (unsigned borrow), AF=1, PF=0 (odd parity of 1); OF=0 (0-(-1) doesn't overflow).
        Assert.Equal(0x0213, cpu.State.Flags);
    }

    [Fact]
    public void Group_81_sub_writes_the_result_to_memory()
    {
        var (cpu, memory) = NewCpu(WithReg(bx: 0x1000), 0x81, 0x2F, 0x01, 0x00); // SUB word [BX], 1
        memory.Write16(0x0A00, 0x1000, 0x0000);

        AssertExecuted(cpu, 0x0104);

        Assert.Equal(0xFFFF, memory.Read16(0x0A00, 0x1000));
        // 0 - 1 = 0xFFFF: CF=1, SF=1, AF=1, PF=1, ZF=0, OF=0.
        Assert.Equal(0x0297, cpu.State.Flags);
    }

    [Fact]
    public void Group_80_add_writes_the_result_to_memory()
    {
        var (cpu, memory) = NewCpu(WithReg(bx: 0x1000), 0x80, 0x07, 0x01); // ADD byte [BX], 1
        memory.Write8(0x0A00, 0x1000, 0x7F);

        AssertExecuted(cpu, 0x0103);

        Assert.Equal(0x80, memory.Read8(0x0A00, 0x1000));
        // Same flag picture as AC-025: OF=1, SF=1, AF=1, PF=0, CF=0.
        Assert.Equal(0x0A92, cpu.State.Flags);
    }

    // ---- 16-bit edges -----------------------------------------------------------------------------------

    [Fact]
    public void Add_ax_imm_crossing_to_8000_sets_of_and_sf()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0001), 0x05, 0xFF, 0x7F); // ADD AX, 7FFFh

        AssertExecuted(cpu, 0x0103);

        Assert.Equal(0x8000, cpu.State.AX);
        // 1 + 0x7FFF = 0x8000: OF=1, SF=1, AF=1, PF=1, CF=0, ZF=0.
        Assert.Equal(0x0A96, cpu.State.Flags);
    }

    [Fact]
    public void Sub_ax_imm_to_zero_sets_zf()
    {
        var (cpu, _) = NewCpu(WithReg(ax: 0x0001), 0x2D, 0x01, 0x00); // SUB AX, 1

        AssertExecuted(cpu, 0x0103);

        Assert.Equal(0x0000, cpu.State.AX);
        // ZF=1, PF=1; CF=0, OF=0, SF=0, AF=0 (1^1^0 & 0x10 = 0).
        Assert.Equal(0x0246, cpu.State.Flags);
    }

    // ---- FR-037: FE/FF forms outside TASK-013 scope halt with a byte-identical state ---------------------

    [Fact]
    public void Fe_arm_2_halts_with_state_untouched()
    {
        var (cpu, _) = NewCpu(0xFE, 0xD0); // FE /2 (NOT rm8 arrives with TASK-014)
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(before, cpu.State); // byte-identical: CS:IP restored, nothing modified
    }

    [Fact]
    public void Ff_arm_7_halts_with_state_untouched()
    {
        var (cpu, _) = NewCpu(0xFF, 0x3F); // FF /7 (IDIV rm16 arrives with a later task)
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(before, cpu.State);
    }

    // ---- segment override on an rm operand ------------------------------------------------------------

    [Fact]
    public void Group_80_honours_a_segment_override()
    {
        var (cpu, memory) = NewCpu(WithReg(bx: 0x1000), 0x26, 0x80, 0x07, 0x01); // ADD byte ES:[BX], 1
        memory.Write8(0x0B00, 0x1000, 0x00);

        AssertExecuted(cpu, 0x0104);

        Assert.Equal(0x01, memory.Read8(0x0B00, 0x1000)); // ES-segment byte incremented
        Assert.Equal(0x00, memory.Read8(0x0A00, 0x1000)); // the DS byte is untouched
        // 0x00 + 1 = 0x01: no flag set (no carry out of bit 3, odd parity of 1).
        Assert.Equal(0x0202, cpu.State.Flags);
    }
}
