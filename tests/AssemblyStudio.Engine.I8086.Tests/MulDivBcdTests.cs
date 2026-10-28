using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Hand tests for the TASK-015 multiply, divide, BCD and conversion instructions: the divide error
/// (AC-031 engine part, both through the router and with real vectoring), the 8086 IDIV range and
/// its REP quirk, MUL/IMUL CF/OF, AAM 0, the 8086 AAA/DAA/DAS edge cases the oracle exposed,
/// CBW/CWD, and the undefined-flag masks reported per step (ADR-0003 §3; cpu-instruction-testing
/// guide: deterministic, full-state assertions).
/// </summary>
public class MulDivBcdTests
{
    private static readonly CpuState Initial = new(
        AX: 0x1234, BX: 0x0000, CX: 0xABCD, DX: 0x0005,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

    private const FlagBit AllArithmetic = FlagBit.OF | FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF | FlagBit.CF;

    /// <summary>Stub router that records the interception and answers with a fixed result.</summary>
    private sealed class RecordingRouter(StepResult? answer) : IInterruptRouter
    {
        public CpuState? SeenState;
        public byte? SeenVector;

        public StepResult? Intercept(byte vector, CpuState state)
        {
            SeenVector = vector;
            SeenState = state;
            return answer;
        }
    }

    private static readonly StepResult RuntimeErrorHalt =
        new(StepOutcome.Halted, 0x0800, 0x0100, HaltReason.RuntimeError, FlagBit.None);

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(CpuState state, IInterruptRouter? router, params byte[] code)
    {
        var memory = new FakeMemory();
        int physical = 0x08000 + 0x0100; // CS 0x0800 (x16 = 0x8000) + IP 0x0100
        foreach (var b in code)
        {
            memory[physical & (FakeMemory.Size - 1)] = b;
            physical++;
        }
        var cpu = new Cpu8086(memory, router);
        cpu.SetState(state);
        return (cpu, memory);
    }

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(CpuState state, params byte[] code) => NewCpu(state, null, code);

    private static StepResult AssertExecuted(Cpu8086 cpu, ushort expectedEndIp)
    {
        var result = cpu.StepInstruction();
        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(HaltReason.None, result.HaltReason);
        Assert.Equal(0x0100, result.InstructionIp);
        Assert.Equal(expectedEndIp, cpu.State.IP);
        return result;
    }

    // ---- Divide error (AC-031 engine part) -------------------------------------------------------------

    [Fact]
    public void Ac_031_div_bl_by_zero_raises_interrupt_0_through_the_router_with_state_untouched()
    {
        var router = new RecordingRouter(RuntimeErrorHalt);
        var (cpu, memory) = NewCpu(Initial, router, 0xF6, 0xF3); // DIV BL, BL = 0

        var result = cpu.StepInstruction();

        Assert.Equal((byte)0, router.SeenVector);
        Assert.Equal(Initial, router.SeenState); // CS:IP still on the DIV
        Assert.Equal(RuntimeErrorHalt, result);
        Assert.Equal(Initial, cpu.State);
        Assert.Empty(memory.TakeJournal());
    }

    [Fact]
    public void Div_by_zero_without_handler_vectors_with_microcode_flags_and_next_ip()
    {
        var (cpu, memory) = NewCpu(Initial, 0xF6, 0xF3); // DIV BL, BL = 0
        memory[0] = 0x78; memory[1] = 0x56; memory[2] = 0x34; memory[3] = 0x12; // IVT[0] = 1234:5678

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x0100, result.InstructionIp);
        Assert.Equal(AllArithmetic, result.UndefinedFlags);
        // Pushed FLAGS = the entry compare AH - divisor (12h - 0: PF=1, the rest clear) over 0202h.
        Assert.Equal(Initial with { CS = 0x1234, IP = 0x5678, SP = 0xFFF8, Flags = 0x0006 }, cpu.State);
        int stack = (0x0C00 << 4) + 0xFFF8;
        Assert.Equal(0x0102, memory[stack] | (memory[stack + 1] << 8));     // return IP: after the DIV (8086)
        Assert.Equal(0x0800, memory[stack + 2] | (memory[stack + 3] << 8)); // CS
        Assert.Equal(0x0206, memory[stack + 4] | (memory[stack + 5] << 8)); // FLAGS
    }

    [Fact]
    public void Idiv_quotient_of_minus_128_is_a_divide_error_on_the_8086()
    {
        var router = new RecordingRouter(RuntimeErrorHalt);
        var state = Initial with { AX = 0xFF00, BX = 0x0002 }; // -256 / 2
        var (cpu, _) = NewCpu(state, router, 0xF6, 0xFB);      // IDIV BL

        var result = cpu.StepInstruction();

        Assert.Equal((byte)0, router.SeenVector);
        Assert.Equal(HaltReason.RuntimeError, result.HaltReason);
        Assert.Equal(state, cpu.State);
    }

    [Fact]
    public void Aam_0_is_a_divide_error()
    {
        var router = new RecordingRouter(RuntimeErrorHalt);
        var (cpu, _) = NewCpu(Initial, router, 0xD4, 0x00); // AAM 0

        cpu.StepInstruction();

        Assert.Equal((byte)0, router.SeenVector);
        Assert.Equal(Initial, cpu.State);
    }

    // ---- DIV / IDIV results -------------------------------------------------------------------------

    [Fact]
    public void Div_word_puts_quotient_in_ax_and_remainder_in_dx()
    {
        var (cpu, _) = NewCpu(Initial with { DX = 0x0001, AX = 0x0007, BX = 0x0010 }, 0xF7, 0xF3); // DIV BX

        var result = AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x1000, cpu.State.AX); // 10007h / 10h
        Assert.Equal(0x0007, cpu.State.DX);
        Assert.Equal(AllArithmetic, result.UndefinedFlags);
    }

    [Fact]
    public void Idiv_word_remainder_takes_the_dividend_sign()
    {
        var (cpu, _) = NewCpu(Initial with { DX = 0xFFFF, AX = 0xFFF9, BX = 0x0002 }, 0xF7, 0xFB); // IDIV BX: -7 / 2

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0xFFFD, cpu.State.AX); // -3
        Assert.Equal(0xFFFF, cpu.State.DX); // -1
    }

    [Theory]
    [InlineData(false, 0x0103)] // 7 / 2: AL = 3, AH = 1
    [InlineData(true, 0x01FD)]  // REP prefix: quotient negated (undocumented 8086, oracle-verified)
    public void Idiv_byte_rep_prefix_negates_the_quotient(bool rep, ushort expectedAx)
    {
        var state = Initial with { AX = 0x0007, BX = 0x0002 };
        var (cpu, _) = rep ? NewCpu(state, 0xF3, 0xF6, 0xFB) : NewCpu(state, 0xF6, 0xFB);

        AssertExecuted(cpu, rep ? (ushort)0x0103 : (ushort)0x0102);

        Assert.Equal(expectedAx, cpu.State.AX);
    }

    // ---- MUL / IMUL --------------------------------------------------------------------------------

    [Fact]
    public void Mul_byte_sets_cf_of_when_ah_is_non_zero()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x0080, BX = 0x0002 }, 0xF6, 0xE3); // MUL BL

        var result = AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0100, cpu.State.AX);
        Assert.Equal(0x0A03, cpu.State.Flags); // CF=OF=1 over 0202h; SF/ZF/AF/PF untouched
        Assert.Equal(FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF, result.UndefinedFlags);
    }

    [Fact]
    public void Mul_word_writes_dx_ax()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0xFFFF, BX = 0xFFFF }, 0xF7, 0xE3); // MUL BX

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0001, cpu.State.AX);
        Assert.Equal(0xFFFE, cpu.State.DX);
        Assert.Equal(0x0A03, cpu.State.Flags);
    }

    [Fact]
    public void Imul_byte_clears_cf_of_when_ah_is_the_sign_extension()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x00FF, BX = 0x0002, Flags = 0x0A03 }, 0xF6, 0xEB); // IMUL BL: -1 * 2

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0xFFFE, cpu.State.AX);
        Assert.Equal(0x0202, cpu.State.Flags);
    }

    // ---- BCD adjust ---------------------------------------------------------------------------------

    [Fact]
    public void Aam_10_splits_al_into_unpacked_digits()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x003F }, 0xD4, 0x0A); // AAM: 63

        var result = AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0603, cpu.State.AX);
        Assert.Equal(0, cpu.State.Flags & (ushort)(FlagBit.SF | FlagBit.ZF));
        Assert.NotEqual(0, cpu.State.Flags & (ushort)FlagBit.PF); // 03h: even parity
        Assert.Equal(FlagBit.OF | FlagBit.AF | FlagBit.CF, result.UndefinedFlags);
    }

    [Fact]
    public void Aad_10_combines_unpacked_digits()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x0607 }, 0xD5, 0x0A); // AAD: 6*10 + 7

        AssertExecuted(cpu, 0x0102);

        Assert.Equal(0x0043, cpu.State.AX);
    }

    [Fact]
    public void Aaa_adjusts_al_and_ah_separately_on_the_8086()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x00FB }, 0x37); // AAA: AL nibble B > 9

        var result = AssertExecuted(cpu, 0x0101);

        // AL = FBh + 6 = 01h (byte), AH = 0 + 1: 0101h (a 286 adds 106h to AX and gets 0201h).
        Assert.Equal(0x0101, cpu.State.AX);
        Assert.Equal(0x0213, cpu.State.Flags); // CF=AF=1
        Assert.Equal(FlagBit.OF | FlagBit.SF | FlagBit.ZF | FlagBit.PF, result.UndefinedFlags);
    }

    [Fact]
    public void Aas_with_af_set_adjusts_even_a_low_nibble_below_10()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x0535, Flags = 0x0213 }, 0x3F); // AAS: nibble 5, AF=1

        AssertExecuted(cpu, 0x0101);

        Assert.Equal(0x040F, cpu.State.AX); // AL = 35h - 6 = 2Fh -> 0Fh, AH = 05h - 1
        Assert.Equal(0x0213, cpu.State.Flags);
    }

    [Fact]
    public void Daa_with_af_set_uses_the_8086_9f_threshold()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x009A, Flags = 0x0212 }, 0x27); // DAA: AL=9Ah, AF=1, CF=0

        var result = AssertExecuted(cpu, 0x0101);

        // Low digit: 9Ah + 6 = A0h. 9Ah is not above 9Fh, so no high-digit adjust: CF stays 0.
        Assert.Equal(0x00A0, cpu.State.AX);
        Assert.Equal(0x0296, cpu.State.Flags); // AF=1, SF=1, PF=1 (A0h), CF=ZF=0
        Assert.Equal(FlagBit.OF, result.UndefinedFlags);
    }

    [Fact]
    public void Das_low_digit_borrow_does_not_set_cf()
    {
        var (cpu, _) = NewCpu(Initial with { AX = 0x0000, Flags = 0x0212 }, 0x2F); // DAS: AL=00h, AF=1

        AssertExecuted(cpu, 0x0101);

        Assert.Equal(0x00FA, cpu.State.AX);
        Assert.Equal(0x0296, cpu.State.Flags); // AF=1, SF=1, PF=1, CF=0
    }

    // ---- CBW / CWD ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(0x1280, 0xFF80)]
    [InlineData(0xFF7F, 0x007F)]
    public void Cbw_sign_extends_al(ushort ax, ushort expectedAx)
    {
        var (cpu, _) = NewCpu(Initial with { AX = ax }, 0x98);

        var result = AssertExecuted(cpu, 0x0101);

        Assert.Equal(Initial with { AX = expectedAx, IP = 0x0101 }, cpu.State);
        Assert.Equal(FlagBit.None, result.UndefinedFlags);
    }

    [Theory]
    [InlineData(0x8000, 0xFFFF)]
    [InlineData(0x7FFF, 0x0000)]
    public void Cwd_sign_extends_ax_into_dx(ushort ax, ushort expectedDx)
    {
        var (cpu, _) = NewCpu(Initial with { AX = ax }, 0x99);

        AssertExecuted(cpu, 0x0101);

        Assert.Equal(Initial with { AX = ax, DX = expectedDx, IP = 0x0101 }, cpu.State);
    }
}
