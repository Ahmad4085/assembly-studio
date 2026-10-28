using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Hand tests for TASK-016: conditional jumps (70-7F), JCXZ (E3), loops (E0-E2), unconditional
/// jumps (EB, E9, EA, FF /4, FF /5), calls (E8, 9A, FF /2, FF /3), returns (C2, C3, CA, CB),
/// interrupts (CC, CD, CE) with router interception vs IVT vectoring, and IRET (CF).
/// </summary>
public class ControlTransferTests
{
    private static readonly CpuState Initial = new(
        AX: 0x1234, BX: 0x2345, CX: 0x0005, DX: 0x4567,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

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

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(CpuState state, IInterruptRouter? router, params byte[] code)
    {
        var memory = new FakeMemory();
        int physical = (state.CS << 4) + state.IP;
        foreach (var b in code)
        {
            memory[physical & (FakeMemory.Size - 1)] = b;
            physical++;
        }
        var cpu = new Cpu8086(memory, router);
        cpu.SetState(state);
        return (cpu, memory);
    }

    [Theory]
    [InlineData(0x74, (ushort)FlagBit.ZF, true)]   // JZ: ZF=1 -> taken
    [InlineData(0x74, (ushort)0, false)]           // JZ: ZF=0 -> fallthrough
    [InlineData(0x75, (ushort)0, true)]            // JNZ: ZF=0 -> taken
    [InlineData(0x75, (ushort)FlagBit.ZF, false)]  // JNZ: ZF=1 -> fallthrough
    [InlineData(0x72, (ushort)FlagBit.CF, true)]   // JC: CF=1 -> taken
    [InlineData(0x72, (ushort)0, false)]           // JC: CF=0 -> fallthrough
    [InlineData(0x73, (ushort)0, true)]            // JNC: CF=0 -> taken
    [InlineData(0x73, (ushort)FlagBit.CF, false)]  // JNC: CF=1 -> fallthrough
    [InlineData(0x70, (ushort)FlagBit.OF, true)]   // JO: OF=1 -> taken
    [InlineData(0x70, (ushort)0, false)]           // JO: OF=0 -> fallthrough
    [InlineData(0x78, (ushort)FlagBit.SF, true)]   // JS: SF=1 -> taken
    [InlineData(0x78, (ushort)0, false)]           // JS: SF=0 -> fallthrough
    [InlineData(0x7A, (ushort)FlagBit.PF, true)]   // JP: PF=1 -> taken
    [InlineData(0x7A, (ushort)0, false)]           // JP: PF=0 -> fallthrough
    [InlineData(0x76, (ushort)FlagBit.CF, true)]   // JBE: CF=1 -> taken
    [InlineData(0x76, (ushort)FlagBit.ZF, true)]   // JBE: ZF=1 -> taken
    [InlineData(0x76, (ushort)0, false)]           // JBE: CF=0, ZF=0 -> fallthrough
    [InlineData(0x77, (ushort)0, true)]            // JA: CF=0, ZF=0 -> taken
    [InlineData(0x77, (ushort)FlagBit.CF, false)]  // JA: CF=1 -> fallthrough
    [InlineData(0x7C, (ushort)FlagBit.SF, true)]   // JL: SF=1, OF=0 -> taken
    [InlineData(0x7C, (ushort)(FlagBit.SF | FlagBit.OF), false)] // JL: SF=1, OF=1 -> fallthrough
    [InlineData(0x7D, (ushort)(FlagBit.SF | FlagBit.OF), true)]  // JGE: SF=1, OF=1 -> taken
    [InlineData(0x7E, (ushort)FlagBit.ZF, true)]   // JLE: ZF=1 -> taken
    [InlineData(0x7F, (ushort)0, true)]            // JG: ZF=0, SF=OF=0 -> taken
    public void Jcc_branch_decisions_match_flags_and_flags_remain_unchanged(byte opcode, ushort flagBits, bool shouldTake)
    {
        // 0x08 = displacement of +8 bytes (from 0x0102 -> 0x010A)
        var (cpu, _) = NewCpu(Initial with { Flags = (ushort)(0x0202 | flagBits) }, router: null, opcode, 0x08);

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x0800, result.InstructionCs);
        Assert.Equal(0x0100, result.InstructionIp);
        Assert.Equal((ushort)(0x0202 | flagBits), cpu.State.Flags); // Flags untouched

        ushort expectedIp = shouldTake ? (ushort)(0x0100 + 2 + 8) : (ushort)(0x0100 + 2);
        Assert.Equal(expectedIp, cpu.State.IP);
    }

    [Fact]
    public void Jcc_backward_jump_wraps_correctly()
    {
        // Backward jump by -6 bytes: 0xFE = -2, 0xFA = -6
        // IP at 0x0100, opcode length 2 -> nextIp = 0x0102 -> target = 0x0102 - 6 = 0x00FC
        var (cpu, _) = NewCpu(Initial with { Flags = (ushort)(0x0202 | (ushort)FlagBit.ZF) }, router: null, 0x74, 0xFA);

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x00FC, cpu.State.IP);
    }

    [Fact]
    public void Jcxz_jumps_when_cx_is_zero_and_falls_through_otherwise()
    {
        var (cpuZero, _) = NewCpu(Initial with { CX = 0 }, router: null, 0xE3, 0x10);
        var resZero = cpuZero.StepInstruction();
        Assert.Equal(0x0112, cpuZero.State.IP);

        var (cpuNonZero, _) = NewCpu(Initial with { CX = 1 }, router: null, 0xE3, 0x10);
        var resNonZero = cpuNonZero.StepInstruction();
        Assert.Equal(0x0102, cpuNonZero.State.IP);
    }

    [Fact]
    public void Loop_decrements_cx_and_jumps_until_cx_is_zero_without_affecting_flags()
    {
        var (cpu, _) = NewCpu(Initial with { CX = 2, Flags = 0x0246 }, router: null, 0xE2, 0x0A); // LOOP +10

        var res1 = cpu.StepInstruction();
        Assert.Equal(0x010C, cpu.State.IP);
        Assert.Equal(1, cpu.State.CX);
        Assert.Equal(0x0246, cpu.State.Flags); // Flags untouched!

        // Reset IP to test second iteration reaching zero
        cpu.SetState(cpu.State with { IP = 0x0100 });
        var res2 = cpu.StepInstruction();
        Assert.Equal(0x0102, cpu.State.IP); // Falls through on CX = 0
        Assert.Equal(0, cpu.State.CX);
        Assert.Equal(0x0246, cpu.State.Flags);
    }

    [Fact]
    public void Loope_and_loopne_respect_zf_condition()
    {
        // LOOPE (E1): jumps if CX != 0 and ZF == 1
        var (cpuLoopeZf0, _) = NewCpu(Initial with { CX = 3, Flags = 0x0202 }, router: null, 0xE1, 0x0A);
        cpuLoopeZf0.StepInstruction();
        Assert.Equal(0x0102, cpuLoopeZf0.State.IP); // ZF=0 -> falls through

        var (cpuLoopeZf1, _) = NewCpu(Initial with { CX = 3, Flags = (ushort)(0x0202 | (ushort)FlagBit.ZF) }, router: null, 0xE1, 0x0A);
        cpuLoopeZf1.StepInstruction();
        Assert.Equal(0x010C, cpuLoopeZf1.State.IP); // ZF=1 -> jumps

        // LOOPNE (E0): jumps if CX != 0 and ZF == 0
        var (cpuLoopneZf0, _) = NewCpu(Initial with { CX = 3, Flags = 0x0202 }, router: null, 0xE0, 0x0A);
        cpuLoopneZf0.StepInstruction();
        Assert.Equal(0x010C, cpuLoopneZf0.State.IP); // ZF=0 -> jumps

        var (cpuLoopneZf1, _) = NewCpu(Initial with { CX = 3, Flags = (ushort)(0x0202 | (ushort)FlagBit.ZF) }, router: null, 0xE0, 0x0A);
        cpuLoopneZf1.StepInstruction();
        Assert.Equal(0x0102, cpuLoopneZf1.State.IP); // ZF=1 -> falls through
    }

    [Fact]
    public void Unconditional_jumps_short_near_and_far()
    {
        // JMP short (EB 20): IP = 0x0102 + 0x20 = 0x0122
        var (cpuShort, _) = NewCpu(Initial, router: null, 0xEB, 0x20);
        cpuShort.StepInstruction();
        Assert.Equal(0x0122, cpuShort.State.IP);

        // JMP near (E9 00 10): disp16 = 0x1000 -> IP = 0x0103 + 0x1000 = 0x1103
        var (cpuNear, _) = NewCpu(Initial, router: null, 0xE9, 0x00, 0x10);
        cpuNear.StepInstruction();
        Assert.Equal(0x1103, cpuNear.State.IP);

        // JMP far (EA 34 12 78 56): IP = 0x1234, CS = 0x5678
        var (cpuFar, _) = NewCpu(Initial, router: null, 0xEA, 0x34, 0x12, 0x78, 0x56);
        cpuFar.StepInstruction();
        Assert.Equal(0x1234, cpuFar.State.IP);
        Assert.Equal(0x5678, cpuFar.State.CS);
    }

    [Fact]
    public void Near_call_and_ret_near()
    {
        // CALL near (E8 20 00): nextIp = 0x0103, target = 0x0103 + 0x20 = 0x0123
        var (cpu, memory) = NewCpu(Initial, router: null, 0xE8, 0x20, 0x00);

        cpu.StepInstruction();

        Assert.Equal(0x0123, cpu.State.IP);
        Assert.Equal(0xFFFC, cpu.State.SP);
        Assert.Equal(0x0103, memory.Read16(0x0C00, 0xFFFC)); // Return IP pushed

        // Now execute RET (C3) at target
        memory[0x08123] = 0xC3;
        cpu.StepInstruction();

        Assert.Equal(0x0103, cpu.State.IP);
        Assert.Equal(0xFFFE, cpu.State.SP); // Stack restored
    }

    [Fact]
    public void Near_call_and_ret_imm16_cleans_parameters()
    {
        var (cpu, memory) = NewCpu(Initial, router: null, 0xE8, 0x10, 0x00);
        cpu.StepInstruction();

        // Target: RET 0x0004 (C2 04 00)
        memory[0x08113] = 0xC2;
        memory[0x08114] = 0x04;
        memory[0x08115] = 0x00;

        cpu.StepInstruction();

        Assert.Equal(0x0103, cpu.State.IP);
        Assert.Equal(0x0002, cpu.State.SP); // 0xFFFC + 2 (pop) + 4 (imm) = 0x0002 (16-bit wrap)
    }

    [Fact]
    public void Far_call_and_retf()
    {
        // CALL far (9A 34 12 00 20): target = 2000:1234, nextIp = 0x0105
        var (cpu, memory) = NewCpu(Initial, router: null, 0x9A, 0x34, 0x12, 0x00, 0x20);

        cpu.StepInstruction();

        Assert.Equal(0x1234, cpu.State.IP);
        Assert.Equal(0x2000, cpu.State.CS);
        Assert.Equal(0xFFFA, cpu.State.SP);
        Assert.Equal(0x0105, memory.Read16(0x0C00, 0xFFFA)); // Return IP
        Assert.Equal(0x0800, memory.Read16(0x0C00, 0xFFFC)); // Return CS

        // Target: RETF (CB) at 2000:1234 (physical 0x21234)
        memory[0x21234] = 0xCB;
        cpu.StepInstruction();

        Assert.Equal(0x0105, cpu.State.IP);
        Assert.Equal(0x0800, cpu.State.CS);
        Assert.Equal(0xFFFE, cpu.State.SP);
    }

    [Fact]
    public void Indirect_call_and_jmp_via_ff_group()
    {
        // FF /2 CALL near BX: FF D3
        var (cpuCall, memCall) = NewCpu(Initial with { BX = 0x0400 }, router: null, 0xFF, 0xD3);
        cpuCall.StepInstruction();
        Assert.Equal(0x0400, cpuCall.State.IP);
        Assert.Equal(0x0102, memCall.Read16(0x0C00, 0xFFFC)); // Return IP

        // FF /4 JMP near BX: FF E3
        var (cpuJmp, _) = NewCpu(Initial with { BX = 0x0500 }, router: null, 0xFF, 0xE3);
        cpuJmp.StepInstruction();
        Assert.Equal(0x0500, cpuJmp.State.IP);

        // FF /3 CALL far [bx]: FF 1F (DS:BX = 0A00:2345)
        var (cpuFarCall, memFarCall) = NewCpu(Initial with { BX = 0x0200 }, router: null, 0xFF, 0x1F);
        memFarCall.Write16(0x0A00, 0x0200, 0x3344); // target IP
        memFarCall.Write16(0x0A00, 0x0202, 0x5566); // target CS
        cpuFarCall.StepInstruction();
        Assert.Equal(0x3344, cpuFarCall.State.IP);
        Assert.Equal(0x5566, cpuFarCall.State.CS);

        // FF /5 JMP far [bx]: FF 2F
        var (cpuFarJmp, memFarJmp) = NewCpu(Initial with { BX = 0x0300 }, router: null, 0xFF, 0x2F);
        memFarJmp.Write16(0x0A00, 0x0300, 0x7788);
        memFarJmp.Write16(0x0A00, 0x0302, 0x99AA);
        cpuFarJmp.StepInstruction();
        Assert.Equal(0x7788, cpuFarJmp.State.IP);
        Assert.Equal(0x99AA, cpuFarJmp.State.CS);

        // Far indirect with register operand (mod == 3) must halt UnsupportedInstruction
        var (cpuBadMod, _) = NewCpu(Initial, router: null, 0xFF, 0xDB); // FF /3 mod=3 (register)
        var badRes = cpuBadMod.StepInstruction();
        Assert.Equal(StepOutcome.Halted, badRes.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, badRes.HaltReason);
    }

    [Fact]
    public void Int3_intercepted_by_router_returns_router_result()
    {
        var customResult = new StepResult(StepOutcome.Halted, 0x0800, 0x0100, HaltReason.RuntimeError, FlagBit.None);
        var router = new RecordingRouter(customResult);
        var (cpu, _) = NewCpu(Initial, router, 0xCC);

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.RuntimeError, result.HaltReason);
        Assert.Equal((byte)3, router.SeenVector);
        Assert.Equal(0x0100, router.SeenState!.Value.IP);
    }

    [Fact]
    public void Int3_without_router_vectors_through_ivt_vector_3()
    {
        var (cpu, memory) = NewCpu(Initial, router: null, 0xCC);
        // Vector 3: IVT offset 3 * 4 = 12 (0x0C)
        memory[0x0000C] = 0x00; memory[0x0000D] = 0x40; // IP = 0x4000
        memory[0x0000E] = 0x00; memory[0x0000F] = 0x15; // CS = 0x1500

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x1500, cpu.State.CS);
        Assert.Equal(0x4000, cpu.State.IP);
        Assert.Equal(0x0101, memory.Read16(0x0C00, 0xFFF8)); // Return IP (after CC)
        Assert.Equal(0x0800, memory.Read16(0x0C00, 0xFFFA)); // CS
        Assert.Equal(0x0202, memory.Read16(0x0C00, 0xFFFC)); // FLAGS
    }

    [Fact]
    public void Into_vectors_on_overflow_and_falls_through_otherwise()
    {
        // OF = 0: falls through
        var (cpuNoOf, _) = NewCpu(Initial with { Flags = 0x0202 }, router: null, 0xCE);
        var resNoOf = cpuNoOf.StepInstruction();
        Assert.Equal(0x0101, cpuNoOf.State.IP);

        // OF = 1: vectors to vector 4 (IVT offset 4 * 4 = 16 = 0x10)
        var (cpuOf, memory) = NewCpu(Initial with { Flags = (ushort)(0x0202 | (ushort)FlagBit.OF) }, router: null, 0xCE);
        memory[0x00010] = 0x50; memory[0x00011] = 0x60; // IP = 0x6050
        memory[0x00012] = 0x70; memory[0x00013] = 0x80; // CS = 0x8070

        var resOf = cpuOf.StepInstruction();
        Assert.Equal(0x8070, cpuOf.State.CS);
        Assert.Equal(0x6050, cpuOf.State.IP);
    }

    [Fact]
    public void Iret_restores_ip_cs_and_flags_with_canonical_bits()
    {
        var (cpu, memory) = NewCpu(Initial with { SP = 0xFFF8 }, router: null, 0xCF);
        // Stack layout at SP 0xFFF8:
        // [0xFFF8] = IP = 0x1234
        // [0xFFFA] = CS = 0x5678
        // [0xFFFC] = FLAGS = 0x0246 (ZF, PF)
        memory.Write16(0x0C00, 0xFFF8, 0x1234);
        memory.Write16(0x0C00, 0xFFFA, 0x5678);
        memory.Write16(0x0C00, 0xFFFC, 0x0246);

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x1234, cpu.State.IP);
        Assert.Equal(0x5678, cpu.State.CS);
        Assert.Equal(0xFFFE, cpu.State.SP);
        // 8086 canonical flags: bits 12-15 and bit 1 are 1, bits 3 and 5 are 0
        Assert.Equal((ushort)((0x0246 | 0xF002) & ~0x0028), cpu.State.Flags);
    }
}
