using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Runtime.Tests;

public class InterruptRouterTests
{
    private sealed class TestCpu(IMemory memory, IInterruptRouter? router) : ICpu
    {
        public CpuState State { get; private set; } = new(
            AX: 0, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002);

        public void SetState(CpuState state) => State = state;

        public StepResult StepInstruction()
        {
            byte op = memory.Read8(State.CS, State.IP);

            // 0xCD: INT imm8
            if (op == 0xCD)
            {
                byte vector = memory.Read8(State.CS, (ushort)(State.IP + 1));
                StepResult? intercepted = router?.Intercept(vector, State);
                if (intercepted is not null)
                {
                    return intercepted.Value;
                }

                // Vector normally: push Flags, CS, return IP; clear IF/TF; load from IVT
                ushort returnIp = (ushort)(State.IP + 2);
                ushort sp = (ushort)(State.SP - 2);
                memory.Write16(State.SS, sp, State.Flags);
                sp = (ushort)(sp - 2);
                memory.Write16(State.SS, sp, State.CS);
                sp = (ushort)(sp - 2);
                memory.Write16(State.SS, sp, returnIp);

                ushort targetIp = memory.Read16(0x0000, (ushort)(vector * 4));
                ushort targetCs = memory.Read16(0x0000, (ushort)(vector * 4 + 2));

                State = State with
                {
                    SP = sp,
                    CS = targetCs,
                    IP = targetIp,
                    Flags = (ushort)(State.Flags & ~(ushort)(FlagBit.IF | FlagBit.TF))
                };

                return new StepResult(StepOutcome.Executed, State.CS, returnIp, HaltReason.None, FlagBit.None);
            }

            // 0xCF: IRET
            if (op == 0xCF)
            {
                ushort sp = State.SP;
                ushort returnIp = memory.Read16(State.SS, sp);
                ushort returnCs = memory.Read16(State.SS, (ushort)(sp + 2));
                ushort flags = memory.Read16(State.SS, (ushort)(sp + 4));

                State = State with
                {
                    SP = (ushort)(sp + 6),
                    CS = returnCs,
                    IP = returnIp,
                    Flags = flags
                };
                return new StepResult(StepOutcome.Executed, returnCs, returnIp, HaltReason.None, FlagBit.None);
            }

            // 0xF6 /7: DIV BL simulation for divide error test
            if (op == 0xF6 && memory.Read8(State.CS, (ushort)(State.IP + 1)) == 0xF3)
            {
                byte divisor = (byte)State.BX;
                if (divisor == 0)
                {
                    StepResult? intercepted = router?.Intercept(0, State);
                    if (intercepted is not null)
                    {
                        return intercepted.Value;
                    }

                    // Vector normally
                    ushort nextIp = (ushort)(State.IP + 2);
                    ushort sp = (ushort)(State.SP - 2);
                    memory.Write16(State.SS, sp, State.Flags);
                    sp = (ushort)(sp - 2);
                    memory.Write16(State.SS, sp, State.CS);
                    sp = (ushort)(sp - 2);
                    memory.Write16(State.SS, sp, nextIp);

                    ushort targetIp = memory.Read16(0x0000, 0);
                    ushort targetCs = memory.Read16(0x0000, 2);

                    State = State with
                    {
                        SP = sp,
                        CS = targetCs,
                        IP = targetIp,
                        Flags = (ushort)(State.Flags & ~(ushort)(FlagBit.IF | FlagBit.TF))
                    };
                    return new StepResult(StepOutcome.Executed, State.CS, nextIp, HaltReason.None, FlagBit.None);
                }

                State = State with { IP = (ushort)(State.IP + 2) };
                return new StepResult(StepOutcome.Executed, State.CS, State.IP, HaltReason.None, FlagBit.None);
            }

            // 0xB8: MOV AX, imm16
            if (op == 0xB8)
            {
                ushort imm = memory.Read16(State.CS, (ushort)(State.IP + 1));
                State = State with { AX = imm, IP = (ushort)(State.IP + 3) };
                return new StepResult(StepOutcome.Executed, State.CS, State.IP, HaltReason.None, FlagBit.None);
            }

            // Default NOP advance
            State = State with { IP = (ushort)(State.IP + 1) };
            return new StepResult(StepOutcome.Executed, State.CS, State.IP, HaltReason.None, FlagBit.None);
        }
    }

    private static (TestCpu Cpu, RealModeMemory Memory, InterruptRouter Router, Machine Machine) CreateTestMachine(
        IOsServices? osServices = null)
    {
        var memory = new RealModeMemory();
        var router = new InterruptRouter(memory, osServices);
        var cpu = new TestCpu(memory, router);
        var machine = new Machine(cpu, memory, interruptRouter: router, osServices: osServices);
        return (cpu, memory, router, machine);
    }

    private static void LoadCode(RealModeMemory memory, ushort cs, ushort ip, ReadOnlySpan<byte> bytes)
    {
        int physical = (cs << 4) + ip;
        memory.LoadBytes(physical, bytes);
    }

    // ---- IVT Stubs and Reserved Area ----------------------------------------------------

    [Fact]
    public void IvtStubs_Initialize_populates_all_256_vectors_pointing_to_reserved_stubs()
    {
        var memory = new RealModeMemory();
        IvtStubs.Initialize(memory);

        for (int v = 0; v < 256; v++)
        {
            byte vector = (byte)v;
            ushort expectedOffset = IvtStubs.GetDefaultStubOffset(vector);
            ushort actualOffset = memory.Read16(0x0000, (ushort)(vector * 4));
            ushort actualSegment = memory.Read16(0x0000, (ushort)(vector * 4 + 2));

            Assert.Equal(expectedOffset, actualOffset);
            Assert.Equal(IvtStubs.DefaultStubSegment, actualSegment);

            // Each stub in segment 0xF000 must contain an IRET opcode (0xCF)
            byte stubInstruction = memory.Read8(IvtStubs.DefaultStubSegment, expectedOffset);
            Assert.Equal(IvtStubs.IretOpcode, stubInstruction);

            // IsDefaultVector must report true
            Assert.True(IvtStubs.IsDefaultVector(memory, vector));
        }
    }

    [Fact]
    public void IvtStubs_detects_program_installed_vectors()
    {
        var memory = new RealModeMemory();
        IvtStubs.Initialize(memory);

        // Vector 0x21 modified offset
        memory.Write16(0x0000, 0x21 * 4, 0x1234);
        Assert.False(IvtStubs.IsDefaultVector(memory, 0x21));

        // Restore offset, modify segment
        memory.Write16(0x0000, 0x21 * 4, IvtStubs.GetDefaultStubOffset(0x21));
        memory.Write16(0x0000, (0x21 * 4) + 2, 0x2000);
        Assert.False(IvtStubs.IsDefaultVector(memory, 0x21));

        // Restore segment -> default again
        memory.Write16(0x0000, (0x21 * 4) + 2, IvtStubs.DefaultStubSegment);
        Assert.True(IvtStubs.IsDefaultVector(memory, 0x21));
    }

    // ---- AC-031: Divide Error -----------------------------------------------------------

    [Fact]
    public void AC_031_Divide_error_halts_with_runtime_error_at_CS_IP()
    {
        // AC-031: Given execution paused before "div bl" with BL=0,
        // When the user steps,
        // Then execution halts with a 'divide error' runtime message at that CS:IP (FR-038).
        var (cpu, memory, router, machine) = CreateTestMachine();

        // 0800:0100: F6 F3       DIV BL  (AX / BL with BL=0 -> divide error)
        LoadCode(memory, 0x0800, 0x0100, [0xF6, 0xF3]);
        cpu.SetState(new CpuState(
            AX: 0x0010, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002));

        var step = machine.Step();

        // Verifies AC-031 verbatim: halts with RuntimeError at CS:IP of the DIV instruction
        Assert.Equal(StepOutcome.Halted, step.Outcome);
        Assert.Equal(HaltReason.RuntimeError, step.HaltReason);
        Assert.Equal(0x0800, step.InstructionCs);
        Assert.Equal(0x0100, step.InstructionIp);

        Assert.NotNull(router.LastHalt);
        Assert.Equal("RTE001", router.LastHalt.Code);
        Assert.Contains("divide error", router.LastHalt.What, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0800:0100", router.LastHalt.Where);
        Assert.Contains("divide error", router.LastHalt.FormattedMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0800:0100", router.LastHalt.FormattedMessage);
        Assert.False(string.IsNullOrWhiteSpace(router.LastHalt.NextStep));
    }

    // ---- AC-029: Unsupported Interrupt Service -----------------------------------------

    [Fact]
    public void AC_029_Unsupported_INT_21h_function_3Dh_halts_naming_function_with_no_host_access()
    {
        // AC-029: Given a program calling INT 21h with AH=3Dh (open file),
        // When it executes the call,
        // Then execution halts with a message naming INT 21h function 3Dh as unsupported,
        // and no host file is accessed (FR-040, NFR-013).
        var (cpu, memory, router, machine) = CreateTestMachine();

        // 0800:0100: CD 21       INT 21h
        LoadCode(memory, 0x0800, 0x0100, [0xCD, 0x21]);
        cpu.SetState(new CpuState(
            AX: 0x3D00, BX: 0, CX: 0, DX: 0, // AH=3Dh
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002));

        var step = machine.Step();

        // Verifies AC-029 verbatim: halts with UnsupportedService at CS:IP
        Assert.Equal(StepOutcome.Halted, step.Outcome);
        Assert.Equal(HaltReason.UnsupportedService, step.HaltReason);
        Assert.Equal(0x0800, step.InstructionCs);
        Assert.Equal(0x0100, step.InstructionIp);

        Assert.NotNull(router.LastHalt);
        Assert.Equal("RTE021", router.LastHalt.Code);
        Assert.Equal((byte)0x21, router.LastHalt.Vector);
        Assert.Equal((byte)0x3D, router.LastHalt.Function);
        Assert.Contains("INT 21h function 3Dh", router.LastHalt.What);
        Assert.Contains("unsupported", router.LastHalt.What, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0800:0100", router.LastHalt.Where);
        Assert.False(string.IsNullOrWhiteSpace(router.LastHalt.NextStep));
    }

    // ---- Program-Installed Vector Tests ------------------------------------------------

    [Fact]
    public void Program_installed_vector_for_INT_21h_vectors_through_IVT_to_guest_handler()
    {
        var (cpu, memory, _, machine) = CreateTestMachine();

        // Hook INT 21h to point to custom handler at 1000:0200h
        const ushort handlerCs = 0x1000;
        const ushort handlerIp = 0x0200;
        memory.Write16(0x0000, 0x21 * 4, handlerIp);
        memory.Write16(0x0000, (0x21 * 4) + 2, handlerCs);

        // Main code at 0800:0100:
        // 0800:0100: CD 21       INT 21h
        // 0800:0102: 90          NOP (return target)
        LoadCode(memory, 0x0800, 0x0100, [0xCD, 0x21, 0x90]);

        // Handler code at 1000:0200:
        // 1000:0200: B8 34 12    MOV AX, 1234h
        // 1000:0203: CF          IRET
        LoadCode(memory, handlerCs, handlerIp, [0xB8, 0x34, 0x12, 0xCF]);

        cpu.SetState(new CpuState(
            AX: 0, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: (ushort)((ushort)FlagBit.IF | 0x0002)));

        // Step 1: INT 21h vectors through IVT
        var step1 = machine.Step();
        Assert.Equal(StepOutcome.Executed, step1.Outcome);
        Assert.Equal(HaltReason.None, step1.HaltReason);

        // Check CPU is now in custom handler with IF/TF cleared
        Assert.Equal(handlerCs, cpu.State.CS);
        Assert.Equal(handlerIp, cpu.State.IP);
        Assert.Equal(0, (ushort)(cpu.State.Flags & (ushort)(FlagBit.IF | FlagBit.TF)));

        // Check stack contents pushed by vectoring: return IP (0x0102), return CS (0x0800), flags
        ushort sp = cpu.State.SP;
        Assert.Equal((ushort)(0xFFFE - 6), sp);
        Assert.Equal(0x0102, memory.Read16(cpu.State.SS, sp));
        Assert.Equal(0x0800, memory.Read16(cpu.State.SS, (ushort)(sp + 2)));

        // Step 2: MOV AX, 1234h inside custom handler
        var step2 = machine.Step();
        Assert.Equal(StepOutcome.Executed, step2.Outcome);
        Assert.Equal(0x1234, cpu.State.AX);

        // Step 3: IRET inside custom handler returns to 0800:0102
        var step3 = machine.Step();
        Assert.Equal(StepOutcome.Executed, step3.Outcome);
        Assert.Equal(0x0800, cpu.State.CS);
        Assert.Equal(0x0102, cpu.State.IP);
        Assert.Equal(0xFFFE, cpu.State.SP);
        Assert.NotEqual(0, (ushort)(cpu.State.Flags & (ushort)FlagBit.IF)); // IF restored by IRET
    }

    [Fact]
    public void Program_installed_vector_for_divide_error_vectors_to_guest_handler()
    {
        var (cpu, memory, _, machine) = CreateTestMachine();

        // Hook INT 0 to point to custom divide error handler at 2000:0050h
        const ushort handlerCs = 0x2000;
        const ushort handlerIp = 0x0050;
        memory.Write16(0x0000, 0x00 * 4, handlerIp);
        memory.Write16(0x0000, (0x00 * 4) + 2, handlerCs);

        // Code at 0800:0100:
        // 0800:0100: F6 F3       DIV BL (with BL=0)
        // 0800:0102: 90          NOP
        LoadCode(memory, 0x0800, 0x0100, [0xF6, 0xF3, 0x90]);

        // Handler code at 2000:0050:
        // 2000:0050: B8 88 88    MOV AX, 8888h
        // 2000:0053: CF          IRET
        LoadCode(memory, handlerCs, handlerIp, [0xB8, 0x88, 0x88, 0xCF]);

        cpu.SetState(new CpuState(
            AX: 0x0050, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002));

        var divStep = machine.Step(); // DIV BL -> vectors to custom handler instead of halting

        Assert.Equal(StepOutcome.Executed, divStep.Outcome);
        Assert.Equal(HaltReason.None, divStep.HaltReason);
        Assert.Equal(handlerCs, cpu.State.CS);
        Assert.Equal(handlerIp, cpu.State.IP);

        machine.Step(); // MOV AX, 8888h
        Assert.Equal(0x8888, cpu.State.AX);

        machine.Step(); // IRET -> returns to 0800:0102
        Assert.Equal(0x0800, cpu.State.CS);
        Assert.Equal(0x0102, cpu.State.IP);
    }

    // ---- Other CPU Exceptions and Software Interrupts -----------------------------------

    [Theory]
    [InlineData(1, "RTE002", "Single-step")]
    [InlineData(2, "RTE003", "Non-maskable")]
    [InlineData(3, "RTE004", "Breakpoint")]
    [InlineData(4, "RTE005", "Overflow")]
    public void Default_CPU_exceptions_halt_with_RuntimeError_and_stable_code(
        byte vector, string expectedCode, string expectedKeyword)
    {
        var memory = new RealModeMemory();
        var router = new InterruptRouter(memory);
        var state = new CpuState(
            AX: 0, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0150, Flags: 0x0002);

        var result = router.Intercept(vector, state);

        Assert.NotNull(result);
        Assert.Equal(StepOutcome.Halted, result.Value.Outcome);
        Assert.Equal(HaltReason.RuntimeError, result.Value.HaltReason);
        Assert.Equal(0x0800, result.Value.InstructionCs);
        Assert.Equal(0x0150, result.Value.InstructionIp);

        Assert.NotNull(router.LastHalt);
        Assert.Equal(expectedCode, router.LastHalt.Code);
        Assert.Contains(expectedKeyword, router.LastHalt.What, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0800:0150", router.LastHalt.Where);
        Assert.False(string.IsNullOrWhiteSpace(router.LastHalt.NextStep));
    }

    [Theory]
    [InlineData(0x10, 0x0E, "INT 10h function 0Eh")]
    [InlineData(0x13, 0x02, "INT 13h function 02h")]
    [InlineData(0x16, 0x00, "INT 16h function 00h")]
    [InlineData(0x2F, 0x12, "INT 2Fh function 12h")]
    public void Unsupported_interrupts_halt_with_UnsupportedService(
        byte vector, byte ah, string expectedName)
    {
        var memory = new RealModeMemory();
        var router = new InterruptRouter(memory);
        var state = new CpuState(
            AX: (ushort)(ah << 8), BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0200, Flags: 0x0002);

        var result = router.Intercept(vector, state);

        Assert.NotNull(result);
        Assert.Equal(StepOutcome.Halted, result.Value.Outcome);
        Assert.Equal(HaltReason.UnsupportedService, result.Value.HaltReason);

        Assert.NotNull(router.LastHalt);
        Assert.Equal("RTE020", router.LastHalt.Code);
        Assert.Equal(vector, router.LastHalt.Vector);
        Assert.Equal(ah, router.LastHalt.Function);
        Assert.Contains(expectedName, router.LastHalt.What);
        Assert.Contains("0800:0200", router.LastHalt.Where);
    }

    // ---- OS Services Delegation --------------------------------------------------------

    private sealed class MockOsServices : IOsServices
    {
        public StepResult? NextResult { get; set; }
        public byte? SeenVector { get; private set; }
        public CpuState? SeenState { get; private set; }
        public IMachine? SeenMachine { get; private set; }

        public StepResult? HandleService(byte vector, CpuState state, IMachine machine)
        {
            SeenVector = vector;
            SeenState = state;
            SeenMachine = machine;
            return NextResult;
        }
    }

    [Fact]
    public void OS_services_delegation_returns_executed_when_handled()
    {
        var mockOs = new MockOsServices
        {
            NextResult = new StepResult(StepOutcome.Executed, 0x0800, 0x0100, HaltReason.None, FlagBit.None)
        };
        var (cpu, memory, router, machine) = CreateTestMachine(mockOs);

        LoadCode(memory, 0x0800, 0x0100, [0xCD, 0x21]);
        cpu.SetState(new CpuState(
            AX: 0x0200, BX: 0, CX: 0, DX: 0x0041,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002));

        var step = machine.Step();

        Assert.Equal(StepOutcome.Executed, step.Outcome);
        Assert.Equal((byte)0x21, mockOs.SeenVector);
        Assert.Same(machine, mockOs.SeenMachine);
        Assert.Null(router.LastHalt);
    }

    [Fact]
    public void OS_services_delegation_returns_WaitingForInput()
    {
        var mockOs = new MockOsServices
        {
            NextResult = new StepResult(StepOutcome.WaitingForInput, 0x0800, 0x0100, HaltReason.None, FlagBit.None)
        };
        var (cpu, memory, router, machine) = CreateTestMachine(mockOs);

        LoadCode(memory, 0x0800, 0x0100, [0xCD, 0x21]);
        cpu.SetState(new CpuState(
            AX: 0x0100, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002));

        var step = machine.Step();

        Assert.Equal(StepOutcome.WaitingForInput, step.Outcome);
        Assert.Equal(0x0100, cpu.State.IP); // CS:IP stays on the INT instruction
        Assert.Null(router.LastHalt);
    }

    [Fact]
    public void OS_services_returning_UnsupportedService_populates_LastHalt()
    {
        var mockOs = new MockOsServices
        {
            NextResult = new StepResult(StepOutcome.Halted, 0x0800, 0x0100, HaltReason.UnsupportedService, FlagBit.None)
        };
        var (cpu, memory, router, machine) = CreateTestMachine(mockOs);

        LoadCode(memory, 0x0800, 0x0100, [0xCD, 0x21]);
        cpu.SetState(new CpuState(
            AX: 0x4800, BX: 0x1000, CX: 0, DX: 0, // AH=48h (allocate memory)
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002));

        var step = machine.Step();

        Assert.Equal(StepOutcome.Halted, step.Outcome);
        Assert.Equal(HaltReason.UnsupportedService, step.HaltReason);

        Assert.NotNull(router.LastHalt);
        Assert.Equal("RTE021", router.LastHalt.Code);
        Assert.Equal((byte)0x48, router.LastHalt.Function);
        Assert.Contains("INT 21h function 48h", router.LastHalt.What);
    }

    [Fact]
    public void Router_ClearLastHalt_resets_diagnostic()
    {
        var memory = new RealModeMemory();
        var router = new InterruptRouter(memory);
        var state = new CpuState(
            AX: 0, BX: 0, CX: 0, DX: 0,
            SI: 0, DI: 0, BP: 0, SP: 0xFFFE,
            CS: 0x0800, DS: 0x0800, ES: 0x0800, SS: 0x0800,
            IP: 0x0100, Flags: 0x0002);

        router.Intercept(0, state);
        Assert.NotNull(router.LastHalt);

        router.ClearLastHalt();
        Assert.Null(router.LastHalt);
    }
}
