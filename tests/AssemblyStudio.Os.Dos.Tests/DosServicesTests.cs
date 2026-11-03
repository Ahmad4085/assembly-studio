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
        // TASK-104 (NFR-032): the texts moved to DosMessages.resx read exactly as they did in code.
        Assert.Equal("INT 21h AH=09h found no '$' terminator in the 64 KiB segment starting at 2000:0200", rig.Dos.LastError!.What);
        Assert.Matches("^at [0-9A-F]{4}:[0-9A-F]{4}$", rig.Dos.LastError.Where);
        Assert.Equal("End the string with '$' (for example: msg db 'Hello', 13, 10, '$') and check that DS:DX points to it.", rig.Dos.LastError.NextStep);
        Assert.Equal(string.Empty, rig.Output);
        Assert.Equal(before, rig.Cpu.State);
    }

    // ---- AH=01h/07h/08h read character (TASK-035) ----

    [Fact]
    public void Function_01h_returns_the_input_byte_in_al_and_echoes_it()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput((byte)'a');
        rig.Place(Int21, ax: 0x0100);
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal("a", rig.Output);
        Assert.Equal(before with { AX = 0x0161, IP = 0x0102 }, rig.Cpu.State);
        Assert.Equal(1, rig.Dos.Column);
    }

    [Fact]
    public void Function_01h_echoes_a_tab_as_spaces_but_returns_09h()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput(0x09);
        rig.Place(Int21, ax: 0x0100);

        rig.Cpu.StepInstruction();

        Assert.Equal(new string(' ', 8), rig.Output);
        Assert.Equal(0x09, rig.Cpu.State.AX & 0xFF);
    }

    [Fact]
    public void Function_01h_echoes_enter_as_cr_only()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput(0x0D);
        rig.Place(Int21, ax: 0x0100);

        rig.Cpu.StepInstruction();

        Assert.Equal("\r", rig.Output);
        Assert.Equal(0x0D, rig.Cpu.State.AX & 0xFF);
    }

    [Theory]
    [InlineData(0x07)]
    [InlineData(0x08)]
    public void Functions_07h_and_08h_return_the_input_byte_without_echo(byte function)
    {
        var rig = new Rig();
        rig.Console.EnqueueInput((byte)'q');
        rig.Place(Int21, ax: (ushort)(function << 8));
        CpuState before = rig.Cpu.State;

        rig.Cpu.StepInstruction();

        Assert.Equal(string.Empty, rig.Output);
        Assert.Equal(before with { AX = (ushort)((function << 8) | 'q'), IP = 0x0102 }, rig.Cpu.State);
    }

    [Theory]
    [InlineData(0x01, "\0;")]
    [InlineData(0x07, "")]
    [InlineData(0x08, "")]
    public void An_extended_key_returns_00h_then_its_scan_code_on_the_next_call(byte function, string echo)
    {
        var rig = new Rig();
        rig.Console.EnqueueInput([0x00, 0x3B]); // F1
        rig.Place(Int21, ax: (ushort)(function << 8));

        rig.Cpu.StepInstruction();
        Assert.Equal(0x00, rig.Cpu.State.AX & 0xFF);

        rig.Place(Int21, ax: (ushort)(function << 8));
        rig.Cpu.StepInstruction();
        Assert.Equal(0x3B, rig.Cpu.State.AX & 0xFF);

        Assert.Equal(echo, rig.Output);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x07)]
    [InlineData(0x08)]
    public void An_empty_queue_waits_on_the_int_and_the_call_completes_when_input_arrives(byte function)
    {
        var rig = new Rig();
        rig.Place(Int21, ax: (ushort)(function << 8));
        CpuState before = rig.Cpu.State;

        StepResult first = rig.Cpu.StepInstruction();
        StepResult second = rig.Cpu.StepInstruction();

        foreach (StepResult waiting in new[] { first, second })
        {
            Assert.Equal(StepOutcome.WaitingForInput, waiting.Outcome);
            Assert.Equal(HaltReason.None, waiting.HaltReason);
            Assert.Equal((ushort)0x0100, waiting.InstructionIp);
        }

        Assert.Equal(before, rig.Cpu.State); // CS:IP still on the INT, nothing changed
        Assert.Equal(string.Empty, rig.Output);

        rig.Console.EnqueueInput((byte)'z');
        StepResult resumed = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, resumed.Outcome);
        Assert.Equal((byte)'z', (byte)rig.Cpu.State.AX);
        Assert.Equal(0x0102, rig.Cpu.State.IP);
    }

    [Fact]
    public void A_program_waits_for_a_key_echoes_it_prints_it_again_and_exits()
    {
        byte[] image =
        [
            0xB4, 0x01,       // mov ah, 01h
            0xCD, 0x21,       // int 21h        ; read with echo
            0x88, 0xC2,       // mov dl, al
            0xB4, 0x02,       // mov ah, 02h
            0xCD, 0x21,       // int 21h        ; print it
            0xB8, 0x00, 0x4C, // mov ax, 4C00h
            0xCD, 0x21,       // int 21h
        ];
        var rig = new Rig();
        ComLoader.Load(rig.Machine, image);

        StepResult waiting = rig.RunToEnd();
        Assert.Equal(StepOutcome.WaitingForInput, waiting.Outcome);
        Assert.Equal(0x0102, waiting.InstructionIp);

        rig.Console.EnqueueInput((byte)'k');
        StepResult end = rig.RunToEnd();

        Assert.Equal("kk", rig.Output);
        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction4Ch, 0x00), end.Termination);
    }

    // ---- AH=0Ah buffered input (TASK-036) ----

    private const ushort Buffer = 0x0200;

    private static Rig LineInput(byte max, string typed, byte initialCount = 0)
    {
        var rig = new Rig();
        rig.Memory.Write8(Psp, Buffer, max);
        rig.Memory.Write8(Psp, Buffer + 1, initialCount);
        rig.Console.EnqueueInput(Encoding.Latin1.GetBytes(typed));
        rig.Place(Int21, ax: 0x0A55, dx: Buffer);
        return rig;
    }

    private static string Stored(Rig rig)
    {
        int count = rig.Memory.Read8(Psp, Buffer + 1);
        var bytes = new byte[count + 1];
        for (int i = 0; i <= count; i++)
        {
            bytes[i] = rig.Memory.Read8(Psp, (ushort)(Buffer + 2 + i));
        }

        return Encoding.Latin1.GetString(bytes);
    }

    [Fact]
    public void Function_0Ah_stores_count_chars_and_cr_echoes_cr_only_and_keeps_al()
    {
        Rig rig = LineInput(10, "abc\r");
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal("abc\r", Stored(rig));
        Assert.Equal(10, rig.Memory.Read8(Psp, Buffer)); // max untouched
        Assert.Equal("abc\r", rig.Output);
        Assert.Equal(before with { IP = 0x0102 }, rig.Cpu.State); // AL (55h) unchanged
    }

    [Fact]
    public void Function_0Ah_waits_between_keys_and_writes_the_buffer_only_on_enter()
    {
        Rig rig = LineInput(10, "ab", initialCount: 7);

        StepResult waiting = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.WaitingForInput, waiting.Outcome);
        Assert.Equal("ab", rig.Output); // echoed as typed
        Assert.Equal(7, rig.Memory.Read8(Psp, Buffer + 1)); // not written yet
        Assert.Equal(0x0100, rig.Cpu.State.IP);

        rig.Console.EnqueueInput("c\r"u8);
        StepResult done = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, done.Outcome);
        Assert.Equal("abc\r", Stored(rig));
        Assert.Equal("abc\r", rig.Output);
    }

    [Fact]
    public void Function_0Ah_with_max_0_returns_at_once_without_reading_input()
    {
        Rig rig = LineInput(0, "x");

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal(0x0102, rig.Cpu.State.IP);
        Assert.Equal(1, rig.Console.PendingInputCount);
        Assert.Equal(string.Empty, rig.Output);
    }

    [Theory]
    [InlineData(3, "abc\r", "ab\r", "ab\a\r")]
    [InlineData(1, "a\r", "\r", "\a\r")]
    public void Function_0Ah_sounds_bel_for_a_character_that_does_not_fit(byte max, string typed, string stored, string output)
    {
        Rig rig = LineInput(max, typed);

        rig.Cpu.StepInstruction();

        Assert.Equal(stored, Stored(rig));
        Assert.Equal(output, rig.Output);
    }

    [Theory]
    [InlineData("ab\bc\r", "ac\r", "ab\b \bc\r")]
    [InlineData("ab\u007Fc\r", "ac\r", "ab\b \bc\r")]
    [InlineData("\b\r", "\r", "\r")]
    [InlineData("\u0001\b\r", "\r", "^A\b \b\b \b\r")]
    public void Function_0Ah_bs_and_del_erase_the_last_character_cells(string typed, string stored, string output)
    {
        Rig rig = LineInput(10, typed);

        rig.Cpu.StepInstruction();

        Assert.Equal(stored, Stored(rig));
        Assert.Equal(output, rig.Output);
    }

    [Fact]
    public void Function_0Ah_stores_a_tab_echoes_spaces_and_erases_all_of_them()
    {
        Rig rig = LineInput(10, "x\t\b\r");

        rig.Cpu.StepInstruction();

        Assert.Equal("x\r", Stored(rig));
        Assert.Equal("x" + new string(' ', 7) + string.Concat(Enumerable.Repeat("\b \b", 7)) + "\r", rig.Output);
        Assert.Equal(0, rig.Dos.Column);
    }

    [Fact]
    public void Function_0Ah_echoes_control_characters_as_caret_letter_and_stores_them_raw()
    {
        Rig rig = LineInput(10, "\u0001\r");

        rig.Cpu.StepInstruction();

        Assert.Equal("\u0001\r", Stored(rig));
        Assert.Equal("^A\r", rig.Output);
    }

    [Fact]
    public void Function_0Ah_lf_echoes_cr_lf_and_is_not_stored()
    {
        Rig rig = LineInput(10, "a\nb\r");

        rig.Cpu.StepInstruction();

        Assert.Equal("ab\r", Stored(rig));
        Assert.Equal("a\r\nb\r", rig.Output);
    }

    [Fact]
    public void Function_0Ah_esc_prints_backslash_cr_lf_and_restarts_the_line()
    {
        Rig rig = LineInput(10, "ab\u001Bcd\r");

        rig.Cpu.StepInstruction();

        Assert.Equal("cd\r", Stored(rig));
        Assert.Equal("ab\\\r\ncd\r", rig.Output);
    }

    [Fact]
    public void Function_0Ah_ignores_extended_template_keys_in_the_mvp()
    {
        Rig rig = LineInput(10, "\0;x\r"); // F1, then x

        rig.Cpu.StepInstruction();

        Assert.Equal("x\r", Stored(rig));
        Assert.Equal("x\r", rig.Output);
    }

    // ---- Ctrl-C, ^S and ^P (TASK-036) ----

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x08)]
    [InlineData(0x02)]
    public void Ctrl_c_in_a_checked_function_prints_caret_c_and_terminates_with_the_default_handler(byte function)
    {
        var rig = new Rig();
        rig.Console.EnqueueInput([CtrlC, (byte)'x']);
        rig.Place(Int21, ax: (ushort)(function << 8), dx: 'A');
        CpuState before = rig.Cpu.State;

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Terminated, result.Outcome);
        Assert.Equal(new ProgramTermination(TerminationPath.CtrlC, 0x00), result.Termination);
        Assert.Equal("^C\r\n", rig.Output);
        Assert.Equal(before, rig.Cpu.State);
        Assert.Equal(1, rig.Console.PendingInputCount); // only the ^C was taken
    }

    [Fact]
    public void Ctrl_c_before_a_09h_string_terminates_without_printing_it()
    {
        var rig = new Rig();
        rig.PlaceString(0x0200, "Hello$");
        rig.Console.EnqueueInput(CtrlC);
        rig.Place(Int21, ax: 0x0900, dx: 0x0200);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(TerminationPath.CtrlC, result.Termination?.Path);
        Assert.Equal("^C\r\n", rig.Output);
    }

    [Fact]
    public void Ctrl_c_while_editing_a_0Ah_line_terminates_and_leaves_the_buffer_untouched()
    {
        Rig rig = LineInput(10, "ab\u0003", initialCount: 7);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(TerminationPath.CtrlC, result.Termination?.Path);
        Assert.Equal("ab^C\r\n", rig.Output);
        Assert.Equal(7, rig.Memory.Read8(Psp, Buffer + 1));
    }

    [Fact]
    public void Function_07h_returns_ctrl_c_s_and_p_as_ordinary_characters()
    {
        foreach (byte key in new[] { CtrlC, CtrlS, CtrlP })
        {
            var rig = new Rig();
            rig.Console.EnqueueInput(key);
            rig.Place(Int21, ax: 0x0700);

            StepResult result = rig.Cpu.StepInstruction();

            Assert.Equal(StepOutcome.Executed, result.Outcome);
            Assert.Equal(key, (byte)rig.Cpu.State.AX);
        }
    }

    [Fact]
    public void Output_leaves_ordinary_queued_keys_for_the_program()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput((byte)'x');
        rig.Place(Int21, ax: 0x0200, dx: 'A');

        rig.Cpu.StepInstruction();

        Assert.Equal("A", rig.Output);
        Assert.Equal(1, rig.Console.PendingInputCount);
    }

    [Fact]
    public void Ctrl_p_is_consumed_and_ignored()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput([CtrlP, (byte)'x']);
        rig.Place(Int21, ax: 0x0200, dx: 'A');

        rig.Cpu.StepInstruction();

        Assert.Equal("A", rig.Output);
        Assert.True(rig.Console.TryReadInput(out byte left));
        Assert.Equal((byte)'x', left);
    }

    [Fact]
    public void Ctrl_s_pauses_output_until_the_next_key_which_is_discarded()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput(CtrlS);
        rig.Place(Int21, ax: 0x0200, dx: 'A');

        StepResult paused = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.WaitingForInput, paused.Outcome);
        Assert.Equal(string.Empty, rig.Output);

        rig.Console.EnqueueInput((byte)'k');
        StepResult resumed = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, resumed.Outcome);
        Assert.Equal("A", rig.Output);
        Assert.Equal(0, rig.Console.PendingInputCount);
    }

    [Fact]
    public void Ctrl_c_after_ctrl_s_still_breaks()
    {
        var rig = new Rig();
        rig.Console.EnqueueInput([CtrlS, CtrlC]);
        rig.Place(Int21, ax: 0x0200, dx: 'A');

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(TerminationPath.CtrlC, result.Termination?.Path);
    }

    [Fact]
    public void A_program_int_23h_with_the_default_vector_terminates_with_ctrl_c()
    {
        var rig = new Rig();
        rig.Place([0xCD, 0x23]);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(new ProgramTermination(TerminationPath.CtrlC, 0x00), result.Termination);
        Assert.Equal(string.Empty, rig.Output);
    }

    private const ushort HandlerSegment = 0x3000;

    /// <summary>Installs a program INT 23h handler at 3000:0000 and runs a 01h call with ^C then 'y' queued.</summary>
    private static Rig WithCtrlCHandler(byte[] handler)
    {
        var rig = new Rig();
        for (int i = 0; i < handler.Length; i++)
        {
            rig.Memory.Write8(HandlerSegment, (ushort)i, handler[i]);
        }

        rig.Memory.Write16(0, 0x23 * 4, 0x0000);
        rig.Memory.Write16(0, (0x23 * 4) + 2, HandlerSegment);
        rig.Console.EnqueueInput([CtrlC, (byte)'y']);
        rig.Place(Int21, ax: 0x0100);
        return rig;
    }

    [Fact]
    public void A_program_ctrl_c_handler_is_entered_with_an_interrupt_frame_returning_to_the_int_21h()
    {
        Rig rig = WithCtrlCHandler([0xCF]); // IRET
        CpuState before = rig.Cpu.State with { Flags = 0x0202 };
        rig.Cpu.SetState(before);

        StepResult result = rig.Cpu.StepInstruction();

        Assert.Equal(StepOutcome.Executed, result.Outcome);
        Assert.Equal("^C\r\n", rig.Output);
        CpuState inHandler = rig.Cpu.State;
        Assert.Equal(HandlerSegment, inHandler.CS);
        Assert.Equal(0x0000, inHandler.IP);
        Assert.Equal(0xFFF8, inHandler.SP);
        Assert.Equal(0, inHandler.Flags & (ushort)(FlagBit.IF | FlagBit.TF));
        Assert.Equal(0x0100, rig.Memory.Read16(Psp, 0xFFF8)); // return IP = the INT 21h
        Assert.Equal(Psp, rig.Memory.Read16(Psp, 0xFFFA));
        Assert.Equal(0x0202, rig.Memory.Read16(Psp, 0xFFFC));
    }

    [Theory]
    [InlineData(new byte[] { 0xCF })]       // IRET
    [InlineData(new byte[] { 0xF8, 0xCB })] // CLC / RETF
    public void A_program_ctrl_c_handler_returning_with_iret_or_retf_cf_clear_retries_the_function(byte[] handler)
    {
        Rig rig = WithCtrlCHandler(handler);

        for (int i = 0; i < handler.Length; i++)
        {
            rig.Cpu.StepInstruction(); // INT 21h → ^C → handler, then the handler instructions
        }

        StepResult retried = rig.Cpu.StepInstruction(); // the handler's return (IRET/RETF)
        Assert.Equal((Psp, (ushort)0x0100), (rig.Cpu.State.CS, rig.Cpu.State.IP));
        retried = rig.Cpu.StepInstruction(); // INT 21h retried

        Assert.Equal(StepOutcome.Executed, retried.Outcome);
        CpuState after = rig.Cpu.State;
        Assert.Equal(0x0102, after.IP);
        Assert.Equal((byte)'y', (byte)after.AX);
        Assert.Equal(Psp, after.CS);
        Assert.Equal(0xFFFE, after.SP); // the FLAGS word a RETF leaves behind is removed
        Assert.Equal("^C\r\ny", rig.Output);
    }

    [Fact]
    public void After_retf_the_handlers_live_carry_decides_not_the_flags_word_left_on_the_stack()
    {
        Rig rig = WithCtrlCHandler([0xF8, 0xCB]); // CLC / RETF
        rig.Cpu.SetState(rig.Cpu.State with { Flags = 0x0003 }); // CF set in the stacked FLAGS

        for (int i = 0; i < 4; i++) // INT 21h → handler, CLC, RETF, INT 21h retried
        {
            rig.Cpu.StepInstruction();
        }

        Assert.Equal((byte)'y', (byte)rig.Cpu.State.AX);
        Assert.Equal(0x0102, rig.Cpu.State.IP);
    }

    [Fact]
    public void A_program_ctrl_c_handler_returning_with_retf_and_cf_set_terminates()
    {
        Rig rig = WithCtrlCHandler([0xF9, 0xCB]); // STC / RETF

        StepResult result = default;
        for (int i = 0; i < 4; i++) // INT 21h → handler, STC, RETF, INT 21h again
        {
            result = rig.Cpu.StepInstruction();
        }

        Assert.Equal(new ProgramTermination(TerminationPath.CtrlC, 0x00), result.Termination);
        Assert.Equal(0xFFFE, rig.Cpu.State.SP);
        Assert.Equal("^C\r\n", rig.Output);
    }

    // ---- AC-023 (runtime part): read a line with 0Ah and print it ----

    [Fact]
    public void AC_023_a_program_waits_for_a_line_echoes_it_and_prints_it()
    {
        byte[] code =
        [
            0xBA, 0x20, 0x01,                   // mov dx, buf (0120h)
            0xB4, 0x0A,                         // mov ah, 0Ah
            0xCD, 0x21,                         // int 21h
            0x8A, 0x1E, 0x21, 0x01,             // mov bl, [buf+1]
            0xB7, 0x00,                         // mov bh, 0
            0xC6, 0x87, 0x22, 0x01, 0x24,       // mov byte [bx+buf+2], '$'
            0xBA, 0x22, 0x01,                   // mov dx, buf+2
            0xB4, 0x09,                         // mov ah, 09h
            0xCD, 0x21,                         // int 21h
            0xB8, 0x00, 0x4C,                   // mov ax, 4C00h
            0xCD, 0x21,                         // int 21h
        ];
        byte[] image = [.. code, 0x90, 0x90, 20, 0, .. new byte[21]];
        Assert.Equal(0x20, code.Length + 2);
        var rig = new Rig();
        ComLoader.Load(rig.Machine, image);

        StepResult waiting = rig.RunToEnd();
        Assert.Equal(StepOutcome.WaitingForInput, waiting.Outcome);
        Assert.Equal(0x0105, waiting.InstructionIp);

        rig.Console.EnqueueInput("abc\r"u8);
        StepResult end = rig.RunToEnd();

        Assert.Equal("abc\rabc", rig.Output);
        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction4Ch, 0x00), end.Termination);
    }

    private const byte CtrlC = 0x03;
    private const byte CtrlP = 0x10;
    private const byte CtrlS = 0x13;

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
        // TASK-104 (NFR-032): the texts moved to DosMessages.resx read exactly as they did in code.
        Assert.Equal($"{(code[1] == 0x20 ? "INT 20h" : "INT 21h AH=00h")} requires CS to be the program segment (PSP 0800h), but CS is 3000h", rig.Dos.LastError.What);
        Assert.Equal("Use INT 21h AH=4Ch (exit code in AL), which works from any code segment, or call this service with CS = PSP.", rig.Dos.LastError.NextStep);
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
