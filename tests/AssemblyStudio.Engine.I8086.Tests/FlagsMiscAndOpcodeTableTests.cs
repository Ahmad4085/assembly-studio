using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Tests for TASK-018 (FR-036, FR-037, FR-045, NFR-017):
/// - Flag operations: CLC, STC, CMC, CLD, STD, CLI, STI
/// - Processor control: HLT halts with HaltReason.Halt
/// - Synchronization: WAIT (0x9B) is a no-op
/// - Exhaustive 256-opcode table test: each opcode either executes or halts UnsupportedInstruction
/// - ModR/M reg arm coverage for all group opcodes (80-83, D0-D3, F6-F7, FE, FF)
/// - Coprocessor ESC (D8-DF) and Port I/O (E4-E7, EC-EF) explicitly unsupported
/// </summary>
public class FlagsMiscAndOpcodeTableTests
{
    private static readonly CpuState BaseState = new(
        AX: 0x1234, BX: 0x5678, CX: 0x9ABC, DX: 0xDEF0,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x1000, DS: 0x2000, ES: 0x3000, SS: 0x4000,
        IP: 0x0100, Flags: 0x0002);

    [Fact]
    public void Clc_clears_carry_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xF8); // CLC
        cpu.SetState(BaseState with { Flags = (ushort)(0x0002 | (ushort)FlagBit.CF) });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.CF) == 0);
        Assert.Equal(0x0101, cpu.State.IP);
    }

    [Fact]
    public void Stc_sets_carry_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xF9); // STC
        cpu.SetState(BaseState with { Flags = 0x0002 });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.CF) != 0);
        Assert.Equal(0x0101, cpu.State.IP);
    }

    [Fact]
    public void Cmc_complements_carry_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xF5); // CMC
        mem.Write8(0x1000, 0x0101, 0xF5); // CMC

        // First CMC: 0 -> 1
        cpu.SetState(BaseState with { Flags = 0x0002 });
        StepResult res1 = cpu.StepInstruction();
        Assert.Equal(StepOutcome.Executed, res1.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.CF) != 0);

        // Second CMC: 1 -> 0
        StepResult res2 = cpu.StepInstruction();
        Assert.Equal(StepOutcome.Executed, res2.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.CF) == 0);
    }

    [Fact]
    public void Cld_clears_direction_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xFC); // CLD
        cpu.SetState(BaseState with { Flags = (ushort)(0x0002 | (ushort)FlagBit.DF) });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.DF) == 0);
        Assert.Equal(0x0101, cpu.State.IP);
    }

    [Fact]
    public void Std_sets_direction_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xFD); // STD
        cpu.SetState(BaseState with { Flags = 0x0002 });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.DF) != 0);
        Assert.Equal(0x0101, cpu.State.IP);
    }

    [Fact]
    public void Cli_clears_interrupt_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xFA); // CLI
        cpu.SetState(BaseState with { Flags = (ushort)(0x0002 | (ushort)FlagBit.IF) });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.IF) == 0);
        Assert.Equal(0x0101, cpu.State.IP);
    }

    [Fact]
    public void Sti_sets_interrupt_flag()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xFB); // STI
        cpu.SetState(BaseState with { Flags = 0x0002 });

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.True((cpu.State.Flags & (ushort)FlagBit.IF) != 0);
        Assert.Equal(0x0101, cpu.State.IP);
    }

    [Fact]
    public void Wait_is_a_noop()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0x9B); // WAIT
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, res.Outcome);
        Assert.Equal(0x0101, cpu.State.IP);
        Assert.Equal(BaseState.AX, cpu.State.AX);
        Assert.Equal(BaseState.Flags, cpu.State.Flags);
    }

    [Fact]
    public void Hlt_halts_with_halt_reason_and_advances_ip()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, 0xF4); // HLT
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, res.Outcome);
        Assert.Equal(HaltReason.Halt, res.HaltReason);
        Assert.Equal(0x1000, res.InstructionCs);
        Assert.Equal(0x0100, res.InstructionIp);
        Assert.Equal(0x0101, cpu.State.IP); // IP advanced past HLT
    }

    [Theory]
    [InlineData(0xD8)]
    [InlineData(0xD9)]
    [InlineData(0xDA)]
    [InlineData(0xDB)]
    [InlineData(0xDC)]
    [InlineData(0xDD)]
    [InlineData(0xDE)]
    [InlineData(0xDF)]
    public void Coprocessor_esc_instructions_halt_unsupported(byte escOpcode)
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, escOpcode);
        mem.Write8(0x1000, 0x0101, 0x00); // ModR/M
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, res.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, res.HaltReason);
        Assert.Equal(BaseState, cpu.State); // State preserved byte-identical (FR-037)
    }

    [Theory]
    [InlineData(0xE4)]
    [InlineData(0xE5)]
    [InlineData(0xE6)]
    [InlineData(0xE7)]
    [InlineData(0xEC)]
    [InlineData(0xED)]
    [InlineData(0xEE)]
    [InlineData(0xEF)]
    public void Port_io_instructions_halt_unsupported(byte portOpcode)
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, portOpcode);
        mem.Write8(0x1000, 0x0101, 0x80); // imm8 for fixed forms
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, res.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, res.HaltReason);
        Assert.Equal(BaseState, cpu.State);
    }

    [Theory]
    [InlineData(0x0F)]
    [InlineData(0x60)]
    [InlineData(0x68)]
    [InlineData(0x6F)]
    [InlineData(0xC0)]
    [InlineData(0xC1)]
    [InlineData(0xC8)]
    [InlineData(0xC9)]
    [InlineData(0xD6)]
    [InlineData(0xF1)]
    public void Undocumented_and_alias_opcodes_halt_unsupported(byte op)
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        mem.Write8(0x1000, 0x0100, op);
        mem.Write8(0x1000, 0x0101, 0xC0); // mod=11 rm=0 reg=0 if ModR/M consumed
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, res.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, res.HaltReason);
        Assert.Equal(BaseState, cpu.State);
    }

    [Fact]
    public void Group3_f6_f7_arm1_test_alias_halts_unsupported()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // F6 /1: ModR/M byte with reg=1 (mod=11, reg=001, rm=000 -> 0xC8)
        mem.Write8(0x1000, 0x0100, 0xF6);
        mem.Write8(0x1000, 0x0101, 0xC8);
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, res.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, res.HaltReason);
        Assert.Equal(BaseState, cpu.State);
    }

    [Fact]
    public void Group4_fe_arms_2_to_7_halt_unsupported()
    {
        for (int arm = 2; arm <= 7; arm++)
        {
            var mem = new FakeMemory();
            var cpu = new Cpu8086(mem);

            // FE /arm: mod=11, reg=arm, rm=0 -> (0xC0 | (arm << 3))
            byte modRm = (byte)(0xC0 | (arm << 3));
            mem.Write8(0x1000, 0x0100, 0xFE);
            mem.Write8(0x1000, 0x0101, modRm);
            cpu.SetState(BaseState);

            StepResult res = cpu.StepInstruction();

            Assert.Equal(StepOutcome.Halted, res.Outcome);
            Assert.Equal(HaltReason.UnsupportedInstruction, res.HaltReason);
            Assert.Equal(BaseState, cpu.State);
        }
    }

    [Fact]
    public void Group5_ff_arm_7_halts_unsupported()
    {
        var mem = new FakeMemory();
        var cpu = new Cpu8086(mem);

        // FF /7: mod=11, reg=7, rm=0 -> (0xC0 | (7 << 3)) = 0xF8
        mem.Write8(0x1000, 0x0100, 0xFF);
        mem.Write8(0x1000, 0x0101, 0xF8);
        cpu.SetState(BaseState);

        StepResult res = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, res.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, res.HaltReason);
        Assert.Equal(BaseState, cpu.State);
    }

    [Fact]
    public void Opcode_table_contains_all_256_opcodes()
    {
        Assert.Equal(256, OpcodeTable.Entries.Count);
        for (int i = 0; i < 256; i++)
        {
            OpcodeEntry entry = OpcodeTable.Get((byte)i);
            Assert.Equal((byte)i, entry.Opcode);
            Assert.False(string.IsNullOrWhiteSpace(entry.Mnemonic));
            Assert.False(string.IsNullOrWhiteSpace(entry.Notes));
        }
    }

    [Fact]
    public void Exhaustive_table_agreement_with_cpu_step_instruction()
    {
        // For every opcode: test that the engine behavior matches the table status!
        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            OpcodeEntry entry = OpcodeTable.Get(opcode);

            if (entry.Status == OpcodeStatus.Prefix)
            {
                Assert.True(OpcodeTable.IsPrefix(opcode));
                continue;
            }

            var mem = new FakeMemory();
            var cpu = new Cpu8086(mem);

            // Place opcode and safe operand bytes following it.
            // LEA (8D), LES (C4), LDS (C5) require memory forms (mod != 11); register forms (mod=11) are invalid per 8086 spec.
            byte modRm = opcode is 0x8D or 0xC4 or 0xC5 ? (byte)0x00 : (byte)0xC0;
            mem.Write8(0x1000, 0x0100, opcode);
            mem.Write8(0x1000, 0x0101, modRm);
            mem.Write8(0x1000, 0x0102, 0x00);
            mem.Write8(0x1000, 0x0103, 0x00);
            mem.Write8(0x1000, 0x0104, 0x00);

            // Set state with safe pointers so any memory/stack access doesn't fail
            cpu.SetState(BaseState with
            {
                SP = 0x8000,
                CX = 1, // Avoid CX=0 early exit for strings/loops
                SI = 0x0100,
                DI = 0x0200,
            });

            StepResult res = cpu.StepInstruction();

            if (entry.Status == OpcodeStatus.Unsupported)
            {
                Assert.True(
                    res.Outcome == StepOutcome.Halted && res.HaltReason == HaltReason.UnsupportedInstruction,
                    $"Opcode 0x{opcode:X2} ({entry.Mnemonic}) is classified as Unsupported, but resulted in {res.Outcome}/{res.HaltReason}");
            }
            else if (entry.Status == OpcodeStatus.Supported)
            {
                Assert.False(
                    res.HaltReason == HaltReason.UnsupportedInstruction,
                    $"Opcode 0x{opcode:X2} ({entry.Mnemonic}) is classified as Supported, but engine halted with UnsupportedInstruction");
            }
        }
    }
}
