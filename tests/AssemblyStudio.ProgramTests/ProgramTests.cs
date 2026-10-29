using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Loading;
using Xunit;
using static AssemblyStudio.ProgramTests.ProgramHarness;

namespace AssemblyStudio.ProgramTests;

/// <summary>
/// TASK-044: acceptance criteria at runtime level, with programs built from NASM source by the bundled NASM 3.02
/// and run headless on the real engine, runtime and DOS services.
/// </summary>
public class ProgramTests
{
    // ---- AC-021: loaded with the documented initial state ----

    [Fact]
    public async Task AC_021_the_image_psp_and_registers_have_the_documented_initial_state()
    {
        BuildResult build = await BuildAsync(Program("hello.asm"));
        ProgramRun run = Run(build, budget: 0); // load only
        var memory = run.Machine.Memory;
        const ushort psp = ComLoader.DefaultProgramSegment;

        Assert.Equal(new CpuState(
            AX: 0, BX: 0, CX: 0, DX: 0, SP: 0xFFFE, BP: 0, SI: 0, DI: 0,
            CS: psp, DS: psp, ES: psp, SS: psp, IP: 0x0100, Flags: 0x0002), run.InitialState);
        for (int i = 0; i < build.Image.Length; i++)
        {
            Assert.Equal(build.Image.Span[i], memory.Read8(psp, (ushort)(0x0100 + i))); // image at PSP:0100h
        }

        Assert.Equal(0x20CD, memory.Read16(psp, 0x0000));  // PSP:0000 INT 20h
        Assert.Equal([0xCD, 0x21, 0xCB], new[] { memory.Read8(psp, 0x50), memory.Read8(psp, 0x51), memory.Read8(psp, 0x52) }); // INT 21h; RETF
        Assert.Equal([0x00, 0x0D], new[] { memory.Read8(psp, 0x80), memory.Read8(psp, 0x81) }); // empty command tail
        Assert.Equal(0x0000, memory.Read16(psp, 0xFFFE)); // the return word for RET to PSP:0000
    }

    // ---- AC-022: hello world ----

    [Fact]
    public async Task AC_022_hello_world_prints_its_message_and_exits_through_4Ch_with_code_0()
    {
        ProgramRun run = await RunAsync(Program("hello.asm"));

        Assert.Equal(RunEnd.Terminated, run.End);
        Assert.Equal("Hello, World!\r\n", run.Output);
        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction4Ch, 0x00), run.Termination);
        Assert.Equal(0, run.Build.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning)); // AC-017: 0 warnings
        Assert.NotNull(run.Build.SourceMap);                                                          // AC-017: mapping produced
    }

    // ---- AC-023: keyboard input ----

    [Fact]
    public async Task AC_023_the_program_waits_echoes_the_typed_line_and_prints_it()
    {
        BuildResult build = await BuildAsync(Program("echo.asm"));

        ProgramRun waiting = Run(build);
        ProgramRun typed = Run(build, InputScript.Typed("abc\r"));

        Assert.Equal(RunEnd.WaitingForInput, waiting.End); // the console would show the waiting indicator
        Assert.Equal("Name: ", waiting.Output);
        Assert.Equal(RunEnd.Terminated, typed.End);
        Assert.Equal("Name: abc\r\r\nabc", typed.Output); // echo "abc" + CR, then the program's CR LF and the line
        Assert.Equal(TerminationPath.Int21hFunction4Ch, typed.Termination?.Path);
    }

    [Fact]
    public async Task Input_typed_in_several_chunks_is_edited_like_one_line()
    {
        ProgramRun run = await RunAsync(Program("echo.asm"), InputScript.Typed("ab", "x\b", "c\r"));

        Assert.Equal(RunEnd.Terminated, run.End);
        Assert.EndsWith("\r\nabc", run.Output, StringComparison.Ordinal);
    }

    // ---- AC-029: unsupported service ----

    [Fact]
    public async Task AC_029_an_unsupported_dos_function_halts_naming_it_and_no_host_file_is_created()
    {
        string sentinel = Path.Combine(Environment.CurrentDirectory, "HARNESS.TXT");
        File.Delete(sentinel);

        ProgramRun run = await RunAsync(Program("open-file.asm"));

        Assert.Equal(RunEnd.Halted, run.End);
        Assert.Equal(HaltReason.UnsupportedService, run.Last.HaltReason);
        Assert.Equal("RTE021", run.Halt?.Code);
        Assert.Contains("INT 21h function 3Ch", run.Halt!.What, StringComparison.Ordinal);
        Assert.Equal((ushort)run.Build.SourceMap!.FirstOffsetOf(Program("open-file.asm"), 6)!, run.Last.InstructionIp); // the INT 21h
        Assert.False(File.Exists(sentinel));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(Program("open-file.asm"))!, "HARNESS.TXT")));
    }

    // ---- AC-031: divide error ----

    [Fact]
    public async Task AC_031_div_by_zero_halts_with_a_divide_error_at_the_div_instruction()
    {
        ProgramRun run = await RunAsync(Program("divide-error.asm"));

        Assert.Equal(RunEnd.Halted, run.End);
        Assert.Equal(HaltReason.RuntimeError, run.Last.HaltReason);
        Assert.Equal("RTE001", run.Halt?.Code);
        Assert.Contains("Divide error", run.Halt!.What, StringComparison.Ordinal);
        ushort divAt = run.Build.SourceMap!.FirstOffsetOf(Program("divide-error.asm"), 5)!.Value;
        Assert.Equal((ComLoader.DefaultProgramSegment, divAt), (run.Last.InstructionCs, run.Last.InstructionIp));
        Assert.Equal($"at {ComLoader.DefaultProgramSegment:X4}:{divAt:X4}", run.Halt.Where);
        Assert.Equal(new SourceMapping(new SourceLocation(Program("divide-error.asm"), 5), false), run.Build.SourceMap.Find(divAt));
    }

    // ---- AC-064: the four termination paths ----

    [Theory]
    [InlineData("exit-int20.asm", TerminationPath.Int20h, 0x00)]
    [InlineData("exit-00h.asm", TerminationPath.Int21hFunction00h, 0x00)]
    [InlineData("exit-4ch.asm", TerminationPath.Int21hFunction4Ch, 0x05)]
    [InlineData("exit-ret.asm", TerminationPath.ReturnToPsp, 0x00)]
    public async Task AC_064_each_termination_path_ends_normally_with_its_exit_code(string program, TerminationPath path, byte exitCode)
    {
        ProgramRun run = await RunAsync(Program(program));

        Assert.Equal(RunEnd.Terminated, run.End);
        Assert.Equal(new ProgramTermination(path, exitCode), run.Termination);
    }

    // ---- AC-047 (runtime level): deterministic replay ----

    [Fact]
    public async Task AC_047_a_recorded_run_replays_with_an_identical_cpu_trace_and_output()
    {
        BuildResult build = await BuildAsync(Program("echo.asm"));

        ProgramRun live = Run(build, InputScript.Typed("Ada\r"), trace: true);
        ProgramRun replay = Run(build, InputScript.Replaying(live.RecordedInput), trace: true);

        Assert.Equal(RunEnd.Terminated, live.End);
        Assert.Equal(4, live.RecordedInput.Count);
        Assert.NotEmpty(live.Trace);
        Assert.Equal(live.Trace, replay.Trace);
        Assert.Equal(live.Output, replay.Output);
        Assert.Equal(live.Termination, replay.Termination);
    }

    [Fact]
    public async Task Two_runs_with_the_same_input_produce_identical_traces()
    {
        BuildResult build = await BuildAsync(Program("echo.asm"));

        ProgramRun first = Run(build, InputScript.Typed("xyz\r"), trace: true);
        ProgramRun second = Run(build, InputScript.Typed("xyz\r"), trace: true);

        Assert.Equal(first.Trace, second.Trace);
        Assert.Equal(first.RecordedInput, second.RecordedInput);
    }

    [Fact]
    public async Task A_replay_with_different_input_diverges()
    {
        BuildResult build = await BuildAsync(Program("echo.asm"));
        ProgramRun live = Run(build, InputScript.Typed("Ada\r"), trace: true);
        InputEvent[] changed = [.. live.RecordedInput];
        changed[1] = changed[1] with { Value = 0x0D }; // Enter after one letter: a shorter line, so the registers differ too

        ProgramRun replay = Run(build, InputScript.Replaying(changed), trace: true);

        Assert.NotEqual(live.Output, replay.Output);
        Assert.NotEqual(live.Trace, replay.Trace);
    }

    // ---- The harness itself ----

    [Fact]
    public async Task An_endless_program_stops_at_the_instruction_budget()
    {
        ProgramRun run = await RunAsync(Program("count.asm"), budget: 10_000);

        Assert.Equal(RunEnd.BudgetExhausted, run.End);
        Assert.Equal(10_000UL, run.Instructions);
    }
}
