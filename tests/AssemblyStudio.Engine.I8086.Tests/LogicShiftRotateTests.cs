using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Hand tests for the TASK-014 logic, TEST/NOT and shift/rotate instructions at the edges the
/// oracle exercises sparsely or the backlog calls out explicitly: CF/OF cleared by logic ops,
/// TEST discarding its result, NOT touching no flags, count=1 OF rules (oracle-verified:
/// ROL/RCL/SHL = MSB^CF, ROR = top two bits differ, RCR = MSB(orig)^MSB(result), SHR = MSB(orig),
/// SAR = 0), rotates leaving SF/ZF/PF/AF untouched, CL count 0 as a no-op, count>1 leaving OF
/// untouched, and the undocumented D0-D3 /6 SETMO (result all ones) (cpu-instruction-testing
/// guide: deterministic, full-state assertions).
/// </summary>
public class LogicShiftRotateTests
{
    private static readonly Core.CpuState Initial = new(
        AX: 0x0040, BX: 0x1000, CX: 0x0200, DX: 0x0005,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(params byte[] code) => NewCpu(Initial, code);

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(Core.CpuState state, params byte[] code)
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

    // ---- AND / OR / XOR -----------------------------------------------------------------------------

    [Fact]
    public void Or_al_bl_clears_cf_and_of()
    {
        var state = Initial with { AX = 0x0040, BX = 0x0001 };
        var (cpu, _) = NewCpu(state, 0x08, 0xD8); // OR AL, BL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0041, cpu.State.AX);
        // 0x40 | 0x01 = 0x41: CF=0, OF=0, SF=0, ZF=0; PF=1 (0x41 has two ones); AF undefined, untouched.
        Assert.Equal(0x0206, cpu.State.Flags);
    }

    [Fact]
    public void And_ax_imm_to_zero_sets_zf()
    {
        var state = Initial with { AX = 0x8001 };
        var (cpu, _) = NewCpu(state, 0x25, 0xFE, 0x7F); // AND AX, 7FFEh

        AssertExecuted(cpu, 0x0103);

        Assert.Equal(0x0000, cpu.State.AX);
        // ZF=1, PF=1 (0x00 -> even); CF=OF=SF=0.
        Assert.Equal(0x0246, cpu.State.Flags);
    }

    [Fact]
    public void Xor_al_al_clears_the_operand_and_sets_zf_pf()
    {
        var state = Initial with { AX = 0x00FF };
        var (cpu, _) = NewCpu(state, 0x30, 0xC0); // XOR AL, AL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0000, cpu.State.AX);
        Assert.Equal(0x0246, cpu.State.Flags);
    }

    // ---- TEST discards the result ---------------------------------------------------------------------

    [Fact]
    public void Test_al_imm_leaves_the_operand_untouched()
    {
        var state = Initial with { AX = 0x0080 };
        var (cpu, _) = NewCpu(state, 0xA8, 0x80); // TEST AL, 80h

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0080, cpu.State.AX);
        // 0x80 & 0x80 = 0x80: SF=1, ZF=0, PF=0 (odd parity), CF=OF=0.
        Assert.Equal(0x0282, cpu.State.Flags);
    }

    [Fact]
    public void Test_rm8_reg_against_memory()
    {
        var (cpu, memory) = NewCpu(Initial with { CX = 0x0010 }, 0x84, 0x0F); // TEST byte [BX], CL
        memory.Write8(0x0A00, 0x1000, 0x10);

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x10, memory.Read8(0x0A00, 0x1000)); // TEST writes nothing
        // 0x10 & 0x10 = 0x10: SF=0, ZF=0, PF=0 (odd), CF=OF=0 -> flags unchanged.
        Assert.Equal(0x0202, cpu.State.Flags);
    }

    [Fact]
    public void Test_rm16_imm_non_zero_result()
    {
        var (cpu, _) = NewCpu(Initial with { BX = 0x0001 }, 0xF7, 0x07, 0x01, 0x00); // TEST word [BX], 1
        // memory at [BX] is 0x0000 by default, so 0x0000 & 0x0001 = 0: ZF=1, PF=1.

        AssertExecuted(cpu, 0x0104);

        Assert.Equal(0x0246, cpu.State.Flags);
    }

    // ---- NOT touches no flags ---------------------------------------------------------------------------

    [Fact]
    public void Not_leaves_flags_untouched()
    {
        var state = Initial with { AX = 0x0000, Flags = 0x0293 }; // some flags set
        var (cpu, _) = NewCpu(state, 0xF7, 0xD0); // NOT AX

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0xFFFF, cpu.State.AX);
        Assert.Equal(0x0293, cpu.State.Flags); // bit for bit unchanged
    }

    // ---- Shifts: count = 1 OF rules ----------------------------------------------------------------------

    [Fact]
    public void Shl_sign_change_sets_of()
    {
        var (cpu, _) = NewCpu(Initial, 0xD0, 0xE0); // SHL AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0080, cpu.State.AX);
        // 0x40 << 1 = 0x80: CF=0, OF = MSB(result) ^ CF = 1, SF=1, PF=0 (odd), ZF=0.
        Assert.Equal(0x0A82, cpu.State.Flags);
    }

    [Fact]
    public void Shl_carry_out_wraps_to_zero()
    {
        var state = Initial with { AX = 0x0080 };
        var (cpu, _) = NewCpu(state, 0xD0, 0xE0); // SHL AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0000, cpu.State.AX);
        // CF=1, OF = 0 ^ 1 = 1, ZF=1, PF=1 (0x00 even); SF=0.
        Assert.Equal(0x0A47, cpu.State.Flags);
    }

    [Fact]
    public void Shr_of_comes_from_the_original_msb()
    {
        var state = Initial with { AX = 0x0081 };
        var (cpu, _) = NewCpu(state, 0xD0, 0xE8); // SHR AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0040, cpu.State.AX);
        // CF=1 (old LSB), OF = MSB(original 0x81) = 1, SF=0, ZF=0, PF=0 (odd).
        Assert.Equal(0x0A03, cpu.State.Flags);
    }

    [Fact]
    public void Sar_fills_with_the_sign()
    {
        var state = Initial with { AX = 0x00F0 };
        var (cpu, _) = NewCpu(state, 0xD0, 0xF8); // SAR AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x00F8, cpu.State.AX);
        // CF=0 (LSB of 0xF0), OF=0, SF=1, PF=0 (0xF8 has five ones -> odd), ZF=0.
        Assert.Equal(0x0282, cpu.State.Flags);
    }

    // ---- Rotates: only CF/OF change ----------------------------------------------------------------------

    [Fact]
    public void Rol_wraps_the_msb_into_lsb_and_cf()
    {
        var state = Initial with { AX = 0x0081 };
        var (cpu, _) = NewCpu(state, 0xD0, 0xC0); // ROL AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0003, cpu.State.AX);
        // CF=1, OF = MSB(result) ^ CF = 1; SF/ZF/PF/AF untouched (initial all clear).
        Assert.Equal(0x0A03, cpu.State.Flags);
    }

    [Fact]
    public void Ror_moves_lsb_into_msb_and_cf()
    {
        var state = Initial with { AX = 0x0001 };
        var (cpu, _) = NewCpu(state, 0xD0, 0xC8); // ROR AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0080, cpu.State.AX);
        // CF=1, OF = MSB(result) ^ bit6(result) = 1; SF/ZF/PF/AF untouched.
        Assert.Equal(0x0A03, cpu.State.Flags);
    }

    [Fact]
    public void Rcl_shifts_through_the_carry()
    {
        var state = Initial with { AX = 0x0040, Flags = 0x0203 }; // CF=1
        var (cpu, _) = NewCpu(state, 0xD0, 0xD0); // RCL AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0081, cpu.State.AX); // CF (1) rotated into bit 0
        // CF = old MSB = 0, OF = MSB(result) ^ CF = 1; SF/ZF/PF/AF untouched.
        Assert.Equal(0x0A02, cpu.State.Flags);
    }

    [Fact]
    public void Rcr_shifts_through_the_carry()
    {
        var state = Initial with { AX = 0x0040, Flags = 0x0203 }; // CF=1
        var (cpu, _) = NewCpu(state, 0xD0, 0xD8); // RCR AL, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x00A0, cpu.State.AX); // CF (1) rotated into bit 7, old LSB 0 into CF
        // CF = old LSB = 0, OF = MSB(original 0x40) ^ MSB(result 0xC0) = 1.
        Assert.Equal(0x0A02, cpu.State.Flags);
    }

    // ---- CL counts -----------------------------------------------------------------------------------------

    [Fact]
    public void Cl_count_zero_changes_nothing()
    {
        var state = Initial with { CX = 0x0000, AX = 0x0040 };
        var (cpu, _) = NewCpu(state, 0xD2, 0xE0); // SHL AL, CL
        var before = cpu.State;

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(before with { IP = 0x0102 }, cpu.State); // byte-identical except IP
    }

    [Fact]
    public void Cl_count_two_leaves_of_untouched()
    {
        var state = Initial with { CX = 0x0002, AX = 0x0040 };
        var (cpu, _) = NewCpu(state, 0xD2, 0xE0); // SHL AL, CL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0000, cpu.State.AX);
        // Two iterations: 0x40 -> 0x80 (CF=0) -> 0x00 (CF=1). OF undefined for count>1 and left
        // as it was (0); ZF=1, PF=1, SF=0.
        Assert.Equal(0x0247, cpu.State.Flags);
    }

    [Fact]
    public void Shr_word_form()
    {
        var state = Initial with { AX = 0x8001 };
        var (cpu, _) = NewCpu(state, 0xD1, 0xE8); // SHR AX, 1

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x4000, cpu.State.AX);
        // CF=1 (old LSB), OF = MSB(original) = 1, PF=1 (0x00 even), SF=0, ZF=0.
        Assert.Equal(0x0A07, cpu.State.Flags);
    }

    // ---- Undocumented SETMO (D0-D3 arm 6) ---------------------------------------------------------------------

    [Fact]
    public void Setmo_register_becomes_all_ones()
    {
        var state = Initial with { AX = 0x0023 };
        var (cpu, _) = NewCpu(state, 0xD0, 0xF0); // undocumented SETMO AL

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x00FF, cpu.State.AX);
        // Real-chip raw behavior: SF=1, PF=1 (0xFF even), ZF=CF=OF=0.
        Assert.Equal(0x0286, cpu.State.Flags);
    }

    [Fact]
    public void Setmo_word_becomes_all_ones()
    {
        var state = Initial with { SI = 0x1234 };
        var (cpu, _) = NewCpu(state, 0xD1, 0xF6); // undocumented SETMO SI

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0xFFFF, cpu.State.SI);
        Assert.Equal(0x0286, cpu.State.Flags);
    }

    [Fact]
    public void Setmo_with_cl_count_zero_is_a_no_op()
    {
        var state = Initial with { CX = 0x0000, AX = 0x0023 };
        var (cpu, _) = NewCpu(state, 0xD2, 0xF0); // undocumented SETMO AL, CL
        var before = cpu.State;

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(before with { IP = 0x0102 }, cpu.State);
    }

    // ---- FR-037: forms outside TASK-014 scope halt with a byte-identical state -----------------------------

    [Fact]
    public void F6_arm_1_undocumented_test_alias_halts_untouched()
    {
        var (cpu, _) = NewCpu(0xF6, 0xC8); // F6 /1 (undocumented TEST alias, not in scope)
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(before, cpu.State);
    }

    [Fact]
    public void F6_arm_4_mul_halts_untouched()
    {
        var (cpu, _) = NewCpu(0xF6, 0xE0); // F6 /4 (MUL arrives with a later task)
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(before, cpu.State);
    }
}
