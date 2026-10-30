using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// DEF-008 (found by the TASK-092 reference consistency test): the undefined-flag mask reported per step (ADR-0003 §3,
/// FR-048) for the logic instructions (AF), the shifts (AF) and shifts and rotates by more than 1 (OF). Flag values are
/// unchanged by the fix; only the report is new.
/// </summary>
public class UndefinedFlagMaskTests
{
    private static readonly CpuState Initial = new(
        AX: 0x1234, BX: 0x00F0, CX: 0x0003, DX: 0x0005,
        SP: 0xFFFE, BP: 0x2000, SI: 0x0100, DI: 0x0200,
        CS: 0x0800, DS: 0x0A00, ES: 0x0B00, SS: 0x0C00,
        IP: 0x0100, Flags: 0x0202);

    private static StepResult Step(params byte[] code) => Step(Initial, code);

    private static StepResult Step(CpuState state, params byte[] code)
    {
        var memory = new FakeMemory();
        for (int i = 0; i < code.Length; i++)
        {
            memory[0x08100 + i] = code[i]; // CS 0800h, IP 0100h
        }

        var cpu = new Cpu8086(memory, null);
        cpu.SetState(state);
        StepResult result = cpu.StepInstruction();
        Assert.Equal(StepOutcome.Executed, result.Outcome);
        return result;
    }

    [Theory]
    [InlineData(new byte[] { 0x20, 0xD8 })]       // AND AL, BL
    [InlineData(new byte[] { 0x09, 0xD8 })]       // OR AX, BX
    [InlineData(new byte[] { 0x34, 0x0F })]       // XOR AL, 0Fh
    [InlineData(new byte[] { 0x80, 0xE0, 0x0F })] // AND AL, 0Fh (group 1)
    [InlineData(new byte[] { 0x84, 0xD8 })]       // TEST AL, BL
    [InlineData(new byte[] { 0xA9, 0x01, 0x00 })] // TEST AX, 1
    [InlineData(new byte[] { 0xF6, 0xC0, 0x01 })] // TEST AL, 1 (group 3)
    public void Logic_instructions_report_AF_undefined(byte[] code) => Assert.Equal(FlagBit.AF, Step(code).UndefinedFlags);

    [Theory]
    [InlineData(new byte[] { 0x00, 0xD8 })]       // ADD AL, BL
    [InlineData(new byte[] { 0x3C, 0x05 })]       // CMP AL, 5
    [InlineData(new byte[] { 0x80, 0xE8, 0x01 })] // SUB AL, 1 (group 1)
    public void Arithmetic_instructions_report_no_undefined_flags(byte[] code) => Assert.Equal(FlagBit.None, Step(code).UndefinedFlags);

    [Theory]
    [InlineData(new byte[] { 0xD0, 0xE0 }, FlagBit.AF)]                // SHL AL, 1
    [InlineData(new byte[] { 0xD1, 0xE8 }, FlagBit.AF)]                // SHR AX, 1
    [InlineData(new byte[] { 0xD0, 0xF8 }, FlagBit.AF)]                // SAR AL, 1
    [InlineData(new byte[] { 0xD2, 0xE0 }, FlagBit.AF | FlagBit.OF)]   // SHL AL, CL (CL = 3)
    [InlineData(new byte[] { 0xD0, 0xC0 }, FlagBit.None)]              // ROL AL, 1
    [InlineData(new byte[] { 0xD3, 0xC8 }, FlagBit.OF)]                // ROR AX, CL (CL = 3)
    [InlineData(new byte[] { 0xD2, 0xD0 }, FlagBit.OF)]                // RCL AL, CL (CL = 3)
    public void Shifts_report_AF_and_counts_above_1_report_OF_undefined(byte[] code, FlagBit undefined) =>
        Assert.Equal(undefined, Step(code).UndefinedFlags);

    [Fact]
    public void A_shift_by_a_count_of_0_changes_nothing_and_reports_nothing()
    {
        StepResult result = Step(Initial with { CX = 0 }, 0xD2, 0xE0); // SHL AL, CL with CL = 0

        Assert.Equal(FlagBit.None, result.UndefinedFlags);
    }
}
