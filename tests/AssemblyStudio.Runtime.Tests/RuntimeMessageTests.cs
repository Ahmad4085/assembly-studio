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
}
