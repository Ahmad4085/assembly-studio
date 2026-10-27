using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Hand tests for the TASK-012 data transfer instructions at the edges the oracle exercise sparsely
/// or the backlog calls out explicitly: PUSH SP, POP SP, segment-register moves, LAHF/SAHF,
/// PUSHF/POPF reserved bits, XLAT, LEA/LDS/LES, moffs, C6/C7 reg-field aliasing, XCHG, and 16-bit
/// word access wrapping (cpu-instruction-testing guide: deterministic, full-state assertions).
/// </summary>
public class DataTransferTests
{
    private static readonly Core.CpuState Initial = new(
        AX: 0x1234, BX: 0x1000, CX: 0xABCD, DX: 0x0005,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

    private static (Cpu8086 Cpu, FakeMemory Memory) NewCpu(params byte[] code)
    {
        var memory = new FakeMemory();
        int physical = 0x08000 + 0x0100; // CS 0x0800 (x16 = 0x8000) + IP 0x0100
        foreach (var b in code)
        {
            memory[physical & (FakeMemory.Size - 1)] = b;
            physical++;
        }
        var cpu = new Cpu8086(memory);
        cpu.SetState(Initial);
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

    // ---- PUSH SP / POP SP (8086 quirks, backlog test requirement) ---------------------------------

    [Fact]
    public void Push_sp_pushes_the_decremented_sp()
    {
        var (cpu, memory) = NewCpu(0x54); // PUSH SP
        var before = cpu.State;

        AssertExecuted(cpu, 0x0101);

        Assert.Equal(0xFFFC, cpu.State.SP);
        Assert.Equal(0xFFFC, memory.Read16(0x0C00, 0xFFFC)); // the NEW SP value was pushed
        Assert.Equal(before with { SP = 0xFFFC, IP = 0x0101 }, cpu.State); // everything else unchanged
    }

    [Fact]
    public void Pop_sp_loads_sp_with_the_popped_value()
    {
        var (cpu, memory) = NewCpu(0x5C); // POP SP
        memory.Write16(0x0C00, 0xFFFE, 0x1234);

        _ = cpu.StepInstruction();

        Assert.Equal(0x1234, cpu.State.SP); // the popped value, not popped + 2
    }

    [Fact]
    public void Pop_sp_via_8f_behaves_as_pop_rm16_and_loads_sp_with_the_popped_value()
    {
        var (cpu, memory) = NewCpu(0x8F, 0xE4); // POP SP (mod 11, reg 4 alias, rm 4 = SP)
        memory.Write16(0x0C00, 0xFFFE, 0x5678);

        _ = cpu.StepInstruction();

        Assert.Equal(0x5678, cpu.State.SP);
        Assert.Equal(0x1234, cpu.State.AX); // other registers untouched
    }

    // ---- segment-register moves -----------------------------------------------------------------

    [Fact]
    public void Mov_store_segment_register_to_memory_8c()
    {
        var (cpu, memory) = NewCpu(0x8C, 0x56, 0x10); // MOV [BP+0x10], SS (reg 2 = SS)

        _ = cpu.StepInstruction();

        Assert.Equal(0x0C00, memory.Read16(0x0C00, 0x2010)); // [BP+...] defaults to the SS segment
        Assert.Equal(0x0C00, cpu.State.SS);
        Assert.Equal(0x0103, cpu.State.IP);
    }

    [Fact]
    public void Mov_load_segment_register_from_memory_8e()
    {
        var (cpu, memory) = NewCpu(0x8E, 0x56, 0x10); // MOV SS, [BP+0x10] (reg 2 = SS)
        memory.Write16(0x0C00, 0x2010, 0x4242);

        _ = cpu.StepInstruction();

        Assert.Equal(0x4242, cpu.State.SS);
        Assert.Equal(0x1234, cpu.State.AX);
    }

    [Fact]
    public void Sreg_field_aliases_low_two_bits()
    {
        // MOV ES, BP: reg field 4 aliases to 0 (ES); reg field 7 would alias to 3 (DS).
        var (cpu, _) = NewCpu(0x8E, 0xE5); // mod 11, reg 4, rm 5 (BP)

        _ = cpu.StepInstruction();

        Assert.Equal(0x2000, cpu.State.ES);
        Assert.Equal(0x0A00, cpu.State.DS); // DS untouched
    }

    [Fact]
    public void Mov_store_cs_to_register_8c_works()
    {
        var (cpu, _) = NewCpu(0x8C, 0xC9); // MOV CX, CS (reg 1 = CS)

        _ = cpu.StepInstruction();

        Assert.Equal(0x0800, cpu.State.CX);
    }

    // ---- LAHF / SAHF (oracle-derived semantics) ---------------------------------------------------

    [Fact]
    public void Lahf_copies_the_flags_low_byte_verbatim()
    {
        // Flags low byte 0x02 (bits 3/5 are 0, bit 1 is 1) -> AH = 0x02.
        var (cpu, _) = NewCpu(0x9F);

        _ = cpu.StepInstruction();

        Assert.Equal(0x02, cpu.State.AX >> 8);
        Assert.Equal(0x34, cpu.State.AX & 0xFF);
    }

    [Fact]
    public void Sahf_sets_flags_low_byte_from_ah_with_8086_canonical_bits()
    {
        var (cpu, _) = NewCpu(0x9E);
        cpu.SetState(Initial with { AX = 0xAFFF, Flags = 0x0000 }); // AH = 0xAF

        _ = cpu.StepInstruction();

        // (AH & 0xD5) | 0x02 = 0x87: SF=1, ZF=0, AF=0, PF=1, CF=1; bit 1 forced 1, bits 3/5 forced 0.
        Assert.Equal(0x0087, cpu.State.Flags);
        Assert.Equal(0xAFFF, cpu.State.AX); // AH unchanged
    }

    // ---- PUSHF / POPF (8086 reserved-bit semantics) -------------------------------------------------

    [Fact]
    public void Pushf_pushes_flags_verbatim()
    {
        var (cpu, memory) = NewCpu(0x9C);

        _ = cpu.StepInstruction();

        Assert.Equal(0xFFFC, cpu.State.SP);
        Assert.Equal(0x0202, memory.Read16(0x0C00, 0xFFFC));
    }

    [Fact]
    public void Popf_forces_the_8086_reserved_bits()
    {
        var (cpu, memory) = NewCpu(0x9D);
        memory.Write16(0x0C00, 0xFFFE, 0x22D9); // popped word

        _ = cpu.StepInstruction();

        // (0x22D9 | 0xF002) & ~0x0028 = 0xF2D3: bits 12-15 and 1 -> 1, bits 3/5 -> 0.
        Assert.Equal(0xF2D3, cpu.State.Flags);
        Assert.Equal(0x0000, cpu.State.SP);
    }

    // ---- XLAT -----------------------------------------------------------------------------------------

    [Fact]
    public void Xlat_reads_ds_bx_plus_al()
    {
        var (cpu, memory) = NewCpu(0xD7);
        memory[0x0A000 + 0x1034] = 0x9A; // DS:[BX+AL] with BX=0x1000, AL=0x34 (Initial AX low byte)

        _ = cpu.StepInstruction();

        Assert.Equal(0x9A, cpu.State.AX & 0xFF);
        Assert.Equal(0x12, cpu.State.AX >> 8);
    }

    [Fact]
    public void Xlat_honours_the_segment_override()
    {
        var (cpu, memory) = NewCpu(0x26, 0xD7); // XLAT with ES override
        memory[0x0B000 + 0x1034] = 0x77; // ES:[BX+AL]

        _ = cpu.StepInstruction();

        Assert.Equal(0x77, cpu.State.AX & 0xFF);
    }

    // ---- LEA / LDS / LES ---------------------------------------------------------------------------

    [Fact]
    public void Lea_loads_the_effective_address_offset()
    {
        var (cpu, _) = NewCpu(0x8D, 0x40, 0x10); // LEA AX, [BX+SI+0x10]

        _ = cpu.StepInstruction();

        Assert.Equal(0x1110, cpu.State.AX); // 0x1000 + 0x0100 + 0x10
    }

    [Fact]
    public void Lea_register_form_halts_undefined()
    {
        var (cpu, _) = NewCpu(0x8D, 0xC0); // LEA AX, AX (mod 11) - undefined on the 8086
        var before = cpu.State;

        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedInstruction, result.HaltReason);
        Assert.Equal(before, cpu.State);
    }

    [Fact]
    public void Lds_loads_the_register_and_ds()
    {
        var (cpu, memory) = NewCpu(0xC5, 0x5E, 0x02); // LDS BX, [BP+2]
        memory.Write16(0x0C00, 0x2002, 0x0B00); // BX gets the word at the EA ([BP+...] → SS segment)
        memory.Write16(0x0C00, 0x2004, 0x1A00); // DS gets the word at EA+2

        _ = cpu.StepInstruction();

        Assert.Equal(0x0B00, cpu.State.BX);
        Assert.Equal(0x1A00, cpu.State.DS);
    }

    [Fact]
    public void Les_loads_the_register_and_es()
    {
        var (cpu, memory) = NewCpu(0xC4, 0x5E, 0x02); // LES BX, [BP+2]
        memory.Write16(0x0C00, 0x2002, 0x0B00);
        memory.Write16(0x0C00, 0x2004, 0x1A00);

        _ = cpu.StepInstruction();

        Assert.Equal(0x0B00, cpu.State.BX);
        Assert.Equal(0x1A00, cpu.State.ES);
        Assert.Equal(0x0A00, cpu.State.DS); // DS untouched
    }

    // ---- MOV moffs (A0-A3) ---------------------------------------------------------------------------

    [Fact]
    public void Mov_al_from_moffs_reads_ds_segment()
    {
        var (cpu, memory) = NewCpu(0xA0, 0x34, 0x12);
        memory[0x0A000 + 0x1234] = 0x66;

        _ = cpu.StepInstruction();

        Assert.Equal(0x66, cpu.State.AX & 0xFF);
        Assert.Equal(0x12, cpu.State.AX >> 8);
    }

    [Fact]
    public void Mov_store_ax_to_moffs_ds_default()
    {
        var (cpu, memory) = NewCpu(0xA3, 0x34, 0x12); // MOV [0x1234], AX, no override: DS

        _ = cpu.StepInstruction();

        Assert.Equal(0x1234, memory.Read16(0x0A00, 0x1234));
        Assert.Equal(0x0103, cpu.State.IP); // A3 consumes two immediate bytes
    }

    [Fact]
    public void Mov_al_to_moffs_honours_override()
    {
        var (cpu, memory) = NewCpu(0x26, 0xA2, 0x34, 0x12); // MOV ES:[0x1234], AL

        _ = cpu.StepInstruction();

        Assert.Equal(0x34, memory[0x0B000 + 0x1234]);
        Assert.Equal(0x0104, cpu.State.IP); // prefix + opcode + 2 immediate bytes
    }

    // ---- MOV reg, imm and r/m, imm --------------------------------------------------------------------

    [Fact]
    public void Mov_imm_to_8bit_and_16bit_registers()
    {
        var (cpu, _) = NewCpu(0xB7, 0x99); // MOV BH, 0x99

        _ = cpu.StepInstruction();

        Assert.Equal(0x9900, cpu.State.BX); // BX 0x1000 with BH = 0x99
    }

    [Fact]
    public void Mov_rm8_imm_ignores_the_reg_field()
    {
        // C6 /7 behaves as /0: MOV AL, 0x77 (mod 11, reg 7, rm 0).
        var (cpu, _) = NewCpu(0xC6, 0xF8, 0x77);

        _ = cpu.StepInstruction();

        Assert.Equal(0x1277, cpu.State.AX);
    }

    [Fact]
    public void Mov_rm16_imm_with_memory_operand()
    {
        var (cpu, memory) = NewCpu(0xC7, 0x06, 0x34, 0x12, 0x78, 0x56); // MOV WORD [0x1234], 0x5678

        _ = cpu.StepInstruction();

        Assert.Equal(0x5678, memory.Read16(0x0A00, 0x1234));
        Assert.Equal(0x0106, cpu.State.IP);
    }

    // ---- XCHG -------------------------------------------------------------------------------------------

    [Fact]
    public void Xchg_exchanges_register_and_memory()
    {
        var (cpu, memory) = NewCpu(0x87, 0x64, 0x10); // XCHG [SI+0x10], SP (reg 4 = SP)
        memory.Write16(0x0A00, 0x0110, 0x9ABC);

        _ = cpu.StepInstruction();

        Assert.Equal(0x9ABC, cpu.State.SP);
        Assert.Equal(0xFFFE, memory.Read16(0x0A00, 0x0110));
    }

    [Fact]
    public void Xchg_ax_reg_including_sp()
    {
        var (cpu, _) = NewCpu(0x94); // XCHG AX, SP

        _ = cpu.StepInstruction();

        Assert.Equal(0xFFFE, cpu.State.AX);
        Assert.Equal(0x1234, cpu.State.SP);
    }

    [Fact]
    public void Nop_is_a_state_unchanged_step()
    {
        var (cpu, memory) = NewCpu(0x90); // XCHG AX, AX = NOP (only IP advances)
        var before = cpu.State;

        _ = cpu.StepInstruction();

        Assert.Equal(before with { IP = 0x0101 }, cpu.State);
        Assert.Empty(memory.TakeJournal());
    }

    // ---- PUSH/POP segment registers and word wrap -----------------------------------------------------------

    [Fact]
    public void Push_pop_segment_registers()
    {
        var (cpu, memory) = NewCpu(0x06, 0x07); // PUSH ES; POP ES at next step
        var result = cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0xFFFC, cpu.State.SP);
        Assert.Equal(0x0B00, memory.Read16(0x0C00, 0xFFFC));

        // Second step: POP ES restores it.
        memory.Write16(0x0C00, 0xFFFC, 0x4321);
        _ = cpu.StepInstruction();
        Assert.Equal(0x4321, cpu.State.ES);
        Assert.Equal(0xFFFE, cpu.State.SP);
    }

    [Fact]
    public void Push_cs_stores_the_code_segment()
    {
        var (cpu, memory) = NewCpu(0x0E); // PUSH CS

        _ = cpu.StepInstruction();

        Assert.Equal(0x0800, memory.Read16(0x0C00, 0xFFFC));
    }

    [Fact]
    public void Word_access_at_offset_0xffff_wraps_within_the_segment()
    {
        var (cpu, memory) = NewCpu(0x8B, 0x07); // MOV AX, [BX] with BX = 0xFFFF
        cpu.SetState(Initial with { BX = 0xFFFF });
        memory[0x0A000 + 0xFFFF] = 0x78;
        memory[0x0A000] = 0x56; // next byte wraps to offset 0x0000

        _ = cpu.StepInstruction();

        Assert.Equal(0x5678, cpu.State.AX);
    }

    [Fact]
    public void Pop_to_memory_increments_sp_and_writes_the_operand()
    {
        var (cpu, memory) = NewCpu(0x8F, 0x44, 0x10); // POP WORD [SI+0x10]
        memory.Write16(0x0C00, 0xFFFE, 0xBEEF);

        _ = cpu.StepInstruction();

        Assert.Equal(0xBEEF, memory.Read16(0x0A00, 0x0110));
        Assert.Equal(0x0000, cpu.State.SP); // 0xFFFE + 2 wraps
    }
}
