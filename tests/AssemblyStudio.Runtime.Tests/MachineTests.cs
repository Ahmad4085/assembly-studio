using System.Text;
using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Runtime.Tests;

public class MachineTests
{
    private sealed class TestCpu : ICpu
    {
        public CpuState State { get; set; } = new(
            AX: 0, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002);

        public int StepCount { get; private set; }

        public void SetState(CpuState state) => State = state;

        public StepResult StepInstruction()
        {
            StepCount++;
            return new StepResult(StepOutcome.Executed, State.CS, State.IP, HaltReason.None, FlagBit.None);
        }
    }

    private sealed class TestConsole : IConsoleDevice
    {
        private readonly StringBuilder _sb = new();

        public void WriteOutput(byte value) => _sb.Append((char)value);

        public string TakeOutput()
        {
            var str = _sb.ToString();
            _sb.Clear();
            return str;
        }

        public bool TryReadInput(out byte value)
        {
            value = (byte)'Z';
            return true;
        }
    }

    private sealed class TestRouter : IInterruptRouter
    {
        public int InterceptCount { get; private set; }

        public StepResult? Intercept(byte vector, CpuState state)
        {
            InterceptCount++;
            return null;
        }
    }

    private sealed class TestOsServices : IOsServices
    {
        public StepResult? HandleService(byte vector, CpuState state, IMachine machine) => null;
    }

    [Fact]
    public void Machine_implements_IMachine_and_wires_all_components()
    {
        var cpu = new TestCpu();
        var mem = new RealModeMemory();
        var console = new TestConsole();
        var router = new TestRouter();
        var os = new TestOsServices();

        var machine = new Machine(cpu, mem, console, router, os);

        Assert.IsAssignableFrom<IMachine>(machine);
        Assert.Same(cpu, machine.Cpu);
        Assert.Same(mem, machine.Memory);
        Assert.Same(console, machine.Console);
        Assert.Same(router, machine.InterruptRouter);
        Assert.Same(os, machine.OsServices);
    }

    [Fact]
    public void Machine_defaults_memory_and_console_when_omitted()
    {
        var cpu = new TestCpu();
        var machine = new Machine(cpu);

        Assert.Same(cpu, machine.Cpu);
        Assert.NotNull(machine.Memory);
        Assert.IsType<RealModeMemory>(machine.Memory);
        Assert.NotNull(machine.Console);
        Assert.IsType<NullConsoleDevice>(machine.Console);
        Assert.Null(machine.InterruptRouter);
        Assert.Null(machine.OsServices);
    }

    [Fact]
    public void Machine_throws_ArgumentNullException_when_Cpu_is_null()
    {
        Assert.Throws<ArgumentNullException>("cpu", () => new Machine(null!));
    }

    [Fact]
    public void Machine_Step_delegates_to_Cpu()
    {
        var cpu = new TestCpu();
        var machine = new Machine(cpu);

        Assert.Equal(0, cpu.StepCount);

        var result = machine.Step();

        Assert.Equal(1, cpu.StepCount);
        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x0100, result.InstructionIp);
    }

    [Fact]
    public void NullConsoleDevice_behaves_as_safe_noop()
    {
        var console = new NullConsoleDevice();

        // WriteOutput does not throw
        console.WriteOutput((byte)'H');
        console.WriteOutput((byte)'i');

        // TakeOutput returns empty string
        Assert.Equal(string.Empty, console.TakeOutput());

        // TryReadInput returns false
        Assert.False(console.TryReadInput(out byte b));
        Assert.Equal(0, b);
    }
}
