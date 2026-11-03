using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Runtime.Tests;

/// <summary>
/// TASK-083: a halt's <see cref="StepResult"/> carries the runtime's message (code, what, where, next step; FR-038,
/// FR-040, FR-066), and the catalog has messages for an unsupported instruction, HLT and an engine fault.
/// </summary>
public sealed class RuntimeMessageTests
{
    private static readonly CpuState State = new(0x3D00, 0, 0, 0, 0xFFFE, 0, 0, 0, 0x0800, 0x0800, 0x0800, 0x0800, 0x0105, 0x0002);

    [Fact]
    public void A_CPU_exception_halt_carries_the_divide_error_message()
    {
        var router = new InterruptRouter(new RealModeMemory());

        StepResult result = router.Intercept(0, State)!.Value;

        Assert.Equal(HaltReason.RuntimeError, result.HaltReason);
        Assert.Equal(router.LastHalt!.ToMessage(), result.Message);
        Assert.Equal("RTE001", result.Message!.Code);
        Assert.StartsWith("Divide error", result.Message.What, StringComparison.Ordinal);
        Assert.Equal("at 0800:0105", result.Message.Where);
        Assert.False(string.IsNullOrWhiteSpace(result.Message.NextStep));
    }

    [Fact]
    public void An_unsupported_service_halt_names_the_interrupt_and_function()
    {
        var router = new InterruptRouter(new RealModeMemory()); // no OS services: every INT 21h function is unsupported

        StepResult result = router.Intercept(0x21, State)!.Value;

        Assert.Equal(HaltReason.UnsupportedService, result.HaltReason);
        Assert.Equal("RTE021", result.Message!.Code);
        Assert.Contains("INT 21h function 3Dh", result.Message.What, StringComparison.Ordinal);
    }

    [Fact]
    public void The_catalog_has_unsupported_instruction_HLT_and_engine_fault_messages_with_what_where_and_next()
    {
        RuntimeMessage[] messages =
        [
            RuntimeErrorPolicy.UnsupportedInstruction(0x0800, 0x0103, [0x0F, 0xA2]),
            RuntimeErrorPolicy.Hlt(0x0800, 0x0110),
            RuntimeErrorPolicy.EngineFault("InvalidOperationException: test"),
        ];

        Assert.Equal(["RTE010", "RTE011", "RTE090"], messages.Select(m => m.Code));
        Assert.Contains("0F A2", messages[0].What, StringComparison.Ordinal);
        Assert.Equal("at 0800:0103", messages[0].Where);
        Assert.All(messages, m => Assert.False(string.IsNullOrWhiteSpace(m.What) || string.IsNullOrWhiteSpace(m.Where) || string.IsNullOrWhiteSpace(m.NextStep)));
    }

    /// <summary>TASK-104 (NFR-032): the texts moved to <c>RuntimeMessages.resx</c> read exactly as they did in code.</summary>
    [Fact]
    public void TASK_104_every_runtime_message_reads_as_before_from_the_resources()
    {
        RuntimeMessage[] messages =
        [
            .. new byte[] { 0, 1, 2, 3, 4, 5 }.Select(v => RuntimeErrorPolicy.CreateCpuExceptionHalt(v, State).ToMessage()),
            .. new byte[] { 0x21, 0x10 }.Select(v => RuntimeErrorPolicy.CreateUnsupportedServiceHalt(v, State).ToMessage()),
            RuntimeErrorPolicy.UnsupportedInstruction(0x0800, 0x0103, [0x0F, 0xA2]),
            RuntimeErrorPolicy.Hlt(0x0800, 0x0110),
            RuntimeErrorPolicy.EngineFault("InvalidOperationException: test"),
        ];

        Assert.Equal(ExpectedTexts, messages.Select(m => $"{m.Code}|{m.What}|{m.Where}|{m.NextStep}"));
    }

    private static readonly string[] ExpectedTexts =
    [
        "RTE001|Divide error: division by zero or quotient overflow|at 0800:0105|Ensure the divisor is non-zero and that the quotient fits within the destination register.",
        "RTE002|Single-step trap (INT 1)|at 0800:0105|Clear the Trap Flag (TF) in FLAGS or install a custom INT 01h handler.",
        "RTE003|Non-maskable interrupt (INT 2)|at 0800:0105|Install a custom INT 02h handler or check hardware interrupt conditions.",
        "RTE004|Breakpoint interrupt (INT 3)|at 0800:0105|Install a custom INT 03h handler or remove the breakpoint instruction (0xCC).",
        "RTE005|Overflow trap (INTO with OF=1)|at 0800:0105|Install a custom INT 04h handler or check preceding arithmetic operations for signed overflow.",
        "RTE000|CPU exception (INT 05h)|at 0800:0105|Install a custom INT 05h vector handler.",
        "RTE021|INT 21h function 3Dh (AH=3Dh) is unsupported|at 0800:0105|Host file, device, process and network services are not available in the offline emulator (NFR-013). Use supported DOS functions (01h, 02h, 07h, 08h, 09h, 0Ah, 4Ch).",
        "RTE020|INT 10h function 3Dh (AH=3Dh) is unsupported|at 0800:0105|Install a program vector for INT 10h or use supported DOS services (INT 20h, INT 21h).",
        "RTE010|Unsupported instruction (bytes at CS:IP: 0F A2)|at 0800:0103|This instruction is outside the supported 8086 set. See Help for the supported instructions, change the code, then Reset.",
        "RTE011|The program executed HLT and the CPU stopped|at 0800:0110|End the program with INT 21h function 4Ch instead of HLT, then Reset.",
        "RTE090|Internal emulator error (InvalidOperationException: test)|in the emulator, not in your program|Reset to try again. The log has the details (Help ▸ About ▸ Diagnostics).",
    ];
}
