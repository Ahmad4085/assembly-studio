using System.Text;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using AssemblyStudio.Runtime;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Loading;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Os.Dos.Tests;

/// <summary>
/// TASK-034: INT 21h AH=02h/09h/00h/4Ch and INT 20h against the research table
/// (`docs/02-research/dos-services.md` §2), run through the real engine, interrupt router and
/// console device. AC-064 (all four termination paths) and the runtime part of AC-022 load
/// hand-assembled .COM images with <see cref="ComLoader"/>.
/// </summary>
public class DosServicesTests
{
    private const ushort Psp = DosServices.DefaultPspSegment;

    private sealed class Rig
    {
        public Rig()
        {
            Memory = new RealModeMemory();
            Dos = new DosServices();
            Router = new InterruptRouter(Memory, Dos);
            Console = new ConsoleDevice();
            Cpu = new Cpu8086(Memory, Router);
            Machine = new Machine(Cpu, Memory, Console, Router, Dos);
        }

        public RealModeMemory Memory { get; }

        public DosServices Dos { get; }

        public InterruptRouter Router { get; }

        public ConsoleDevice Console { get; }

        public Cpu8086 Cpu { get; }

        public Machine Machine { get; }

        public string Output => Encoding.Latin1.GetString(Console.ToByteArray());

        /// <summary>Places <paramref name="code"/> at <paramref name="cs"/>:0100 and sets the registers.</summary>
        public void Place(byte[] code, ushort ax = 0, ushort dx = 0, ushort cs = Psp, ushort ds = Psp)
        {
            for (int i = 0; i < code.Length; i++)
            {
                Memory.Write8(cs, (ushort)(0x0100 + i), code[i]);
            }

            Cpu.SetState(new CpuState(
                AX: ax, BX: 0x1111, CX: 0x2222, DX: dx, SP: 0xFFFE, BP: 0x3333, SI: 0x4444, DI: 0x5555,
                CS: cs, DS: ds, ES: Psp, SS: Psp, IP: 0x0100, Flags: 0x0002));
        }

        public void PlaceString(ushort offset, string text)
        {
            byte[] bytes = Encoding.Latin1.GetBytes(text);
            for (int i = 0; i < bytes.Length; i++)
            {
                Memory.Write8(Psp, (ushort)(offset + i), bytes[i]);
            }
        }

        /// <summary>Steps until the program stops executing (terminated, halted or waiting).</summary>
        public StepResult RunToEnd(int maxSteps = 1000)
        {
            for (int i = 0; i < maxSteps; i++)
            {
                StepResult result = Cpu.StepInstruction();
                if (result.Outcome != StepOutcome.Executed)
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"The program did not stop within {maxSteps} steps.");
        }
    }

    private static readonly byte[] Int21 = [0xCD, 0x21];

    // ---- AH=02h write character ----

    [Fact]
    public void Function_02h_writes_dl_returns_it_in_al_and_moves_past_the_int()
    {
        var rig = new Rig();
        rig.Place(Int21, ax: 0x0200, dx: 'A');
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal("A", rig.Output);
        Assert.Equal(before with { AX = 0x0241, IP = 0x0102 }, rig.Cpu.State);
        Assert.Equal(1, rig.Dos.Column);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(3, 5)]
    [InlineData(7, 1)]
    [InlineData(8, 8)]
    public void Function_02h_tab_pads_spaces_to_the_next_multiple_of_8_and_returns_20h(int column, int spaces)
    {
        var rig = new Rig();
        for (int i = 0; i < column; i++)
        {
            rig.Place(Int21, ax: 0x0200, dx: 'x');
            rig.Cpu.StepInstruction();
        }

        rig.Place(Int21, ax: 0x0200, dx: 0x09);
        rig.Cpu.StepInstruction();

        Assert.Equal(new string('x', column) + new string(' ', spaces), rig.Output);
        Assert.Equal(0x20, rig.Cpu.State.AX & 0xFF);
        Assert.Equal(column + spaces, rig.Dos.Column);
    }

    [Fact]
    public void Function_02h_cr_resets_the_column_and_bs_moves_it_back_without_going_below_zero()
    {
        var rig = new Rig();
        foreach (byte c in "ab"u8.ToArray().Append((byte)0x08).Append((byte)0x0D).Append((byte)0x08))
        {
            rig.Place(Int21, ax: 0x0200, dx: c);
            rig.Cpu.StepInstruction();
            Assert.Equal(c, (byte)rig.Cpu.State.AX);
        }

        Assert.Equal("ab\b\r\b", rig.Output);
        Assert.Equal(0, rig.Dos.Column);
    }

    [Theory]
    [InlineData(0x0A)] // LF
    [InlineData(0x07)] // BEL
    [InlineData(0x1B)] // ESC
    [InlineData(0x7F)] // DEL
    public void Function_02h_outputs_control_bytes_and_del_raw_without_advancing_the_column(byte value)
    {
        var rig = new Rig();
        rig.Place(Int21, ax: 0x0200, dx: value);

        rig.Cpu.StepInstruction();

        Assert.Equal([value], rig.Console.ToByteArray());
        Assert.Equal(value, (byte)rig.Cpu.State.AX);
        Assert.Equal(0, rig.Dos.Column);
    }

    [Fact]
    public void Function_02h_after_a_segment_override_prefix_returns_after_the_whole_instruction()
    {
        var rig = new Rig();
        rig.Place([0x2E, 0xCD, 0x21], ax: 0x0200, dx: 'Z');

        rig.Cpu.StepInstruction();

        Assert.Equal("Z", rig.Output);
        Assert.Equal(0x0103, rig.Cpu.State.IP);
    }

    // ---- AH=09h write string ----

    [Fact]
    public void Function_09h_writes_up_to_the_dollar_through_the_02h_path_and_returns_24h()
    {
        var rig = new Rig();
        rig.PlaceString(0x0200, "Hi\t!\r\n$not printed");
        rig.Place(Int21, ax: 0x0900, dx: 0x0200);
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal("Hi      !\r\n", rig.Output);
        Assert.Equal(before with { AX = 0x0924, IP = 0x0102 }, rig.Cpu.State);
        Assert.Equal(0, rig.Dos.Column);
    }

    [Fact]
    public void Function_09h_with_an_empty_string_writes_nothing()
    {
        var rig = new Rig();
        rig.PlaceString(0x0200, "$");
        rig.Place(Int21, ax: 0x0900, dx: 0x0200);

        rig.Cpu.StepInstruction();

        Assert.Equal(string.Empty, rig.Output);
        Assert.Equal(0x0924, rig.Cpu.State.AX);
    }

    [Fact]
    public void Function_09h_wraps_the_offset_within_the_segment()
    {
        var rig = new Rig();
        rig.PlaceString(0xFFFE, "AB");
        rig.PlaceString(0x0000, "$");
        rig.Place(Int21, ax: 0x0900, dx: 0xFFFE);

        rig.Cpu.StepInstruction();

        Assert.Equal("AB", rig.Output);
    }

    [Fact]
    public void Function_09h_without_a_dollar_in_the_segment_halts_without_output()
    {
        var rig = new Rig();
        rig.Place(Int21, ax: 0x0900, dx: 0x0200, ds: 0x2000); // segment 2000h is all zeros
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.RuntimeError, result.HaltReason);
        Assert.Equal("DOS002", rig.Dos.LastError?.Code);
        Assert.Equal(string.Empty, rig.Output);
        Assert.Equal(before, rig.Cpu.State);
    }

    // ---- Termination ----

    [Fact]
    public void Function_4Ch_terminates_with_the_exit_code_in_al_from_any_code_segment()
    {
        var rig = new Rig();
        rig.Place(Int21, ax: 0x4C05, cs: 0x3000);
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Terminated, result.Outcome);
        Assert.Equal(HaltReason.None, result.HaltReason);
        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction4Ch, 0x05), result.Termination);
        Assert.Equal(before, rig.Cpu.State); // CS:IP stays on the INT
    }

    [Fact]
    public void Function_00h_terminates_with_exit_code_0_when_cs_is_the_psp()
    {
        var rig = new Rig();
        rig.Place(Int21, ax: 0x00FF);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction00h, 0x00), result.Termination);
    }

    [Theory]
    [InlineData(new byte[] { 0xCD, 0x21 }, 0x0000)] // INT 21h AH=00h
    [InlineData(new byte[] { 0xCD, 0x20 }, 0x0000)] // INT 20h
    public void Psp_terminations_halt_with_DOS001_when_cs_is_not_the_psp(byte[] code, ushort ax)
    {
        var rig = new Rig();
        rig.Place(code, ax: ax, cs: 0x3000);
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.RuntimeError, result.HaltReason);
        Assert.Null(result.Termination);
        Assert.Equal("DOS001", rig.Dos.LastError?.Code);
        Assert.Contains("3000", rig.Dos.LastError!.What, StringComparison.Ordinal);
        Assert.Equal(before, rig.Cpu.State);
    }

    [Fact]
    public void Int_20h_terminates_with_exit_code_0_when_cs_is_the_psp()
    {
        var rig = new Rig();
        rig.Place([0xCD, 0x20]);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(new ProgramTermination(TerminationPath.Int20h, 0x00), result.Termination);
    }

    // ---- Routing ----

    [Fact]
    public void Unsupported_int_21h_function_halts_through_the_router_naming_the_function()
    {
        var rig = new Rig();
        rig.Place(Int21, ax: 0x3D00);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Halted, result.Outcome);
        Assert.Equal(HaltReason.UnsupportedService, result.HaltReason);
        Assert.Equal("RTE021", rig.Router.LastHalt?.Code);
        Assert.Contains("3Dh", rig.Router.LastHalt!.What, StringComparison.Ordinal);
    }

    [Fact]
    public void Other_vectors_are_left_to_the_router()
    {
        var rig = new Rig();

        Assert.Null(rig.Dos.HandleService(0x10, rig.Cpu.State, rig.Machine));
    }

    [Fact]
    public void Reset_clears_the_column_and_the_last_error()
    {
        var rig = new Rig();
        rig.Place([0xCD, 0x20], cs: 0x3000);
        rig.Cpu.StepInstruction();
        rig.Place(Int21, ax: 0x0200, dx: 'A');
        rig.Cpu.StepInstruction();

        rig.Dos.Reset();

        Assert.Equal(0, rig.Dos.Column);
        Assert.Null(rig.Dos.LastError);
    }

    // ---- AC-064: four built programs, one per termination path ----

    public static TheoryData<string, byte[], TerminationPath, byte> TerminationPrograms => new()
    {
        { "(a) int 20h", [0xCD, 0x20], TerminationPath.Int20h, 0x00 },
        { "(b) mov ah,0 / int 21h", [0xB4, 0x00, 0xCD, 0x21], TerminationPath.Int21hFunction00h, 0x00 },
        { "(c) mov ax,4C05h / int 21h", [0xB8, 0x05, 0x4C, 0xCD, 0x21], TerminationPath.Int21hFunction4Ch, 0x05 },
        { "(d) ret", [0xC3], TerminationPath.ReturnToPsp, 0x00 },
    };

    [Theory]
    [MemberData(nameof(TerminationPrograms))]
    public void AC_064_each_termination_path_ends_the_session_and_reports_path_and_exit_code(
        string program, byte[] image, TerminationPath path, byte exitCode)
    {
        _ = program;
        var rig = new Rig();
        ComLoader.Load(rig.Machine, image);

        StepResult result = rig.RunToEnd();

        Assert.Equal(StepOutcome.Terminated, result.Outcome);
        Assert.Equal(new ProgramTermination(path, exitCode), result.Termination);
    }

    [Fact]
    public void AC_064_d_ret_reaches_the_int_20h_at_psp_0000()
    {
        var rig = new Rig();
        ComLoader.Load(rig.Machine, [0xC3]);

        StepResult result = rig.RunToEnd();

        Assert.Equal((ushort)Psp, result.InstructionCs);
        Assert.Equal(0x0000, result.InstructionIp);
        Assert.Equal(0x0000, rig.Cpu.State.SP);
    }

    // ---- AC-022 (runtime part): hello world prints and terminates via AH=4Ch with exit code 0 ----

    [Fact]
    public void AC_022_hello_world_prints_its_message_and_terminates_with_4Ch_exit_code_0()
    {
        byte[] code =
        [
            0xBA, 0x0C, 0x01, // mov dx, msg (010Ch)
            0xB4, 0x09,       // mov ah, 09h
            0xCD, 0x21,       // int 21h
            0xB8, 0x00, 0x4C, // mov ax, 4C00h
            0xCD, 0x21,       // int 21h
        ];
        byte[] image = [.. code, .. "Hello, World!\r\n$"u8.ToArray()];
        var rig = new Rig();
        ComLoader.Load(rig.Machine, image);

        StepResult result = rig.RunToEnd();

        Assert.Equal("Hello, World!\r\n", rig.Output);
        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction4Ch, 0x00), result.Termination);
        Assert.Equal(0x010A, result.InstructionIp);
    }
}
