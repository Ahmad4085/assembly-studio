using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Engine skeleton behavior (TASK-011): unsupported-opcode halt with unchanged state (AC-028 engine
/// part, FR-037), prefix handling, and the interrupt interception contract (ADR-0003 §2/ADR-0004 §3).
/// Deterministic: fixed bytes, no wall clock (cpu-instruction-testing guide).
/// </summary>
public class Cpu8086Tests
{
    private static readonly Core.CpuState Initial = new(
        AX: 0x1234, BX: 0x1000, CX: 0x0005, DX: 0x0004,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

    /// <summary>Stub router that records the interception call.</summary>
    private sealed class RecordingRouter(StepResult? answer) : IInterruptRouter
    {
        public Core.CpuState? SeenState;
        public byte? SeenVector;

        public StepResult? Intercept(byte vector, Core.CpuState state)
        {
            SeenVector = vector;
            SeenState = state;
            return answer;
        }
    }

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(byte[] code, IInterruptRouter? router = null)
    {
        var memory = new FakeMemory();
        int physical = 0x08000 + 0x0100; // CS 0x0800 (x16 = 0x8000) + IP 0x0100
        foreach (var b in code)
        {
            memory[physical & (FakeMemory.Size - 1)] = b;
            physical++;
        }
        var cpu = new Cpu8086(memory, router);
        cpu.SetState(Initial);
        return (cpu, memory);
    }

    // ---- unsupported opcodes halt with the state unchanged (FR-037, AC-028 engine part) ----

    [Theory]
    [InlineData(0x60)] // PUSHA alias
    [InlineData(0x6F)] // OUTSD alias
    [InlineData(0xC0)] // shift imm8 alias
    [InlineData(0xC1)]
    [InlineData(0xC8)] // ENTER alias
    [InlineData(0xC9)] // LEAVE alias
    [InlineData(0xD6)] // SALC (undocumented)
    [InlineData(0x0F)] // POP CS (alias, ADR-0003 §4)
    public void Alias_or_unsupported_opcode_halts_with_byte_identical_state(byte opcode)
    {
        var (cpu, memory) = NewCpu([opcode]);
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(0x0800, result.InstructionCs);
        Assert.Equal(0x0100, result.InstructionIp);
        Assert.Equal(before, cpu.State); // CS:IP back on the instruction; nothing else touched
        Assert.Empty(memory.TakeJournal()); // no memory writes
    }

    [Fact]
    public void Prefixes_before_an_unsupported_opcode_do_not_move_state()
    {
        // LOCK + DS override + an alias opcode: prefix consumption must leave the state unchanged.
        var (cpu, memory) = NewCpu([0xF0, 0x3E, 0x60]);
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(0x0100, result.InstructionIp);
        Assert.Equal(before, cpu.State);
        Assert.Empty(memory.TakeJournal());
    }

    [Fact]
    public void Unsupported_opcode_at_ip_0xffff_halts_at_0xffff()
    {
        var memory = new FakeMemory();
        memory[0x17FFF] = 0x60; // CS 0x0800, IP 0xFFFF (physical = 0x8000 + 0xFFFF)
        var cpu = new Cpu8086(memory);
        cpu.SetState(Initial with { IP = 0xFFFF });
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(0xFFFF, result.InstructionIp);
        Assert.Equal(before, cpu.State);
    }

    // ---- LOCK prefix is ignored (ADR-0003 §4) ------------------------------------------

    [Fact]
    public void Lock_prefix_is_ignored()
    {
        // LOCK + INT 21h with a handled router: the LOCK prefix must not disturb the interception.
        var handled = new StepResult(StepOutcome.Halted, 0x0800, 0x0100, HaltReason.UnsupportedService, FlagBit.None);
        var router = new RecordingRouter(handled);
        var (cpu, _) = NewCpu([0xF0, 0xCD, 0x21], router);

        var result = cpu.StepInstruction();

        Assert.Equal(handled, result);
        Assert.Equal((byte)0x21, router.SeenVector);
    }

    // ---- interrupt interception contract (ADR-0003 §2, ADR-0004 §3) ---------------------

    [Fact]
    public void Router_sees_the_vector_and_the_state_with_csip_on_the_int()
    {
        var router = new RecordingRouter(null);
        var (cpu, _) = NewCpu([0xCD, 0x21], router);

        _ = cpu.StepInstruction();

        Assert.Equal<byte>(0x21, router.SeenVector!.Value);
        Assert.Equal(0x0800, router.SeenState!.Value.CS);
        Assert.Equal(0x0100, router.SeenState!.Value.IP); // still on the INT instruction
    }

    [Fact]
    public void Handled_interception_is_returned_unchanged()
    {
        var handled = new StepResult(StepOutcome.Halted, 0x0800, 0x0100, HaltReason.UnsupportedService, FlagBit.None);
        var router = new RecordingRouter(handled);
        var (cpu, _) = NewCpu([0xCD, 0x21], router);

        var result = cpu.StepInstruction();

        Assert.Equal(handled, result);
        Assert.Equal(Initial, cpu.State); // the engine adds nothing on a handled path
    }

    [Fact]
    public void Waiting_for_input_is_passed_through_and_csip_stays_on_the_int()
    {
        var waiting = new StepResult(StepOutcome.WaitingForInput, 0x0800, 0x0100, HaltReason.None, FlagBit.None);
        var router = new RecordingRouter(waiting);
        var (cpu, _) = NewCpu([0xCD, 0x21], router);

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.WaitingForInput, result.Outcome);
        Assert.Equal(0x0800, cpu.State.CS);
        Assert.Equal(0x0100, cpu.State.IP); // re-executes when input arrives (ADR-0004 §5)
    }

    [Fact]
    public void Null_router_vectors_through_the_ivt()
    {
        // No router: every interrupt vectors normally.
        var (cpu, memory) = NewCpu([0xCD, 0x21], router: null);
        memory[0x00084] = 0x00; memory[0x00085] = 0x30; // IVT[0x21] offset = 0x3000 (0x21 * 4 = 0x84)
        memory[0x00086] = 0x00; memory[0x00087] = 0x12; // IVT[0x21] segment = 0x1200

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(HaltReason.None, result.HaltReason);
        Assert.Equal(0x1200, cpu.State.CS);
        Assert.Equal(0x3000, cpu.State.IP);
        // IF and TF cleared; other flag bits preserved (real 8086 INT semantics).
        Assert.Equal(0x0000, (ushort)(cpu.State.Flags & (ushort)(FlagBit.IF | FlagBit.TF)));
        Assert.Equal(0x0002, (ushort)(cpu.State.Flags & ~(ushort)(FlagBit.IF | FlagBit.TF)));

        // The stack received FLAGS, CS, return address (SP: 0xFFFE - 6 = 0xFFF8).
        Assert.Equal(0xFFF8, cpu.State.SP);
        Assert.Equal(0x0102, memory.Read16(0x0C00, 0xFFF8)); // return IP (after CD 21)
        Assert.Equal(0x0800, memory.Read16(0x0C00, 0xFFFA)); // pushed CS
        Assert.Equal(0x0202, memory.Read16(0x0C00, 0xFFFC)); // pushed FLAGS
    }

    [Fact]
    public void Vectoring_pushes_wrap_the_stack_at_16_bits()
    {
        // SP near zero: pushes wrap; every pushed byte lands inside the 1 MiB space.
        var (cpu, memory) = NewCpu([0xCD, 0x21], router: null);
        memory[0x00084] = 0x00; memory[0x00085] = 0x30;
        memory[0x00086] = 0x00; memory[0x00087] = 0x12;
        cpu.SetState(Initial with { SP = 0x0002 });

        _ = cpu.StepInstruction();

        Assert.Equal(0xFFFC, cpu.State.SP);
        Assert.Equal(0x0102, memory.Read16(0x0C00, 0xFFFC));
        Assert.Equal(0x0800, memory.Read16(0x0C00, 0xFFFE));
        Assert.Equal(0x0202, memory.Read16(0x0C00, 0x0000)); // FLAGS wrapped past segment end
    }
}
