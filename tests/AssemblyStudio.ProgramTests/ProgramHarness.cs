using System.Text;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using AssemblyStudio.Os.Dos;
using AssemblyStudio.Runtime;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Loading;
using AssemblyStudio.Runtime.Memory;
using AssemblyStudio.Toolchain.Nasm;
using Xunit;

namespace AssemblyStudio.ProgramTests;

/// <summary>How a harness run stopped.</summary>
public enum RunEnd
{
    /// <summary>The program terminated normally (see <see cref="ProgramRun.Termination"/>).</summary>
    Terminated,

    /// <summary>Execution halted (see <see cref="ProgramRun.Last"/>, <see cref="ProgramRun.Halt"/> and <see cref="ProgramRun.DosError"/>).</summary>
    Halted,

    /// <summary>The program waited for input and the script had none left.</summary>
    WaitingForInput,

    /// <summary>The instruction budget ran out.</summary>
    BudgetExhausted,
}

/// <summary>The input a run supplies: one chunk each time the program waits for input, or a recording to replay.</summary>
public sealed record InputScript
{
    /// <summary>No input.</summary>
    public static readonly InputScript None = new();

    /// <summary>Chunks typed one at a time: the next chunk is queued each time the program waits for input.</summary>
    public IReadOnlyList<string> Chunks { get; init; } = [];

    /// <summary>A recorded input sequence to replay instead (NFR-016, AC-047).</summary>
    public IReadOnlyList<InputEvent>? Replay { get; init; }

    /// <summary>Types <paramref name="chunks"/>, one per wait.</summary>
    public static InputScript Typed(params string[] chunks) => new() { Chunks = chunks };

    /// <summary>Replays <paramref name="recording"/>, each byte at its recorded instruction count.</summary>
    public static InputScript Replaying(IReadOnlyList<InputEvent> recording) => new() { Replay = recording };
}

/// <summary>The result of one harness run: the build, how the run ended, the console output and the CPU trace.</summary>
/// <param name="Build">The build result (its image was loaded).</param>
/// <param name="Load">The loader's result.</param>
/// <param name="InitialState">The CPU state right after loading.</param>
/// <param name="End">How the run stopped.</param>
/// <param name="Last">The last step result.</param>
/// <param name="Output">Everything the program wrote to the console (code page 437 bytes as Latin-1 text).</param>
/// <param name="Instructions">Instructions executed (waiting steps not counted).</param>
/// <param name="Trace">The CPU state (registers, IP, FLAGS) after each executed instruction, when tracing was requested.
/// Memory isn't part of it: input that changes only memory shows up in <paramref name="Output"/>, not in the trace.</param>
/// <param name="RecordedInput">The input bytes the program read, stamped with their instruction counts.</param>
/// <param name="Halt">The router's halt diagnostic (CPU exception or unsupported service), if any.</param>
/// <param name="DosError">The DOS service's refusal (DOS001/DOS002), if any.</param>
/// <param name="Machine">The machine after the run, for further inspection.</param>
public sealed record ProgramRun(
    BuildResult Build,
    ComLoadResult Load,
    CpuState InitialState,
    RunEnd End,
    StepResult Last,
    string Output,
    ulong Instructions,
    IReadOnlyList<CpuState> Trace,
    IReadOnlyList<InputEvent> RecordedInput,
    RuntimeHaltInfo? Halt,
    DosServiceError? DosError,
    Machine Machine)
{
    /// <summary>The termination path and exit code when <see cref="End"/> is <see cref="RunEnd.Terminated"/>.</summary>
    public ProgramTermination? Termination => Last.Termination;
}

/// <summary>
/// Program-level test harness (ADR-0008 §4, TASK-044): builds a NASM source with the bundled NASM 3.02, loads the
/// .COM image into a fresh machine (real engine, runtime, interrupt router, DOS services and console), runs it
/// headless with scripted input and reports output, exit path and an optional CPU trace. Samples (TASK-091) use
/// the same API.
/// </summary>
public static class ProgramHarness
{
    /// <summary>The default instruction budget (FR-042's default is 10,000,000; tests need far fewer).</summary>
    public const ulong DefaultBudget = 1_000_000;

    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

    /// <summary>Full path of a program fixture in <c>tests/AssemblyStudio.ProgramTests/Programs</c>.</summary>
    public static string Program(string fileName) =>
        Path.Combine(RepositoryRoot.Value, "tests", "AssemblyStudio.ProgramTests", "Programs", fileName);

    /// <summary>Builds <paramref name="sourcePath"/>; fails the test with the build output when the build fails.</summary>
    public static async Task<BuildResult> BuildAsync(string sourcePath)
    {
        var assembler = new NasmAssembler(new NasmOptions
        {
            BundledPath = Path.Combine(RepositoryRoot.Value, "tools", "nasm", "nasm.exe"),
            TempRoot = Path.Combine(Path.GetTempPath(), "asmstudio-programtests"),
        });
        BuildResult build = await assembler.BuildAsync(new BuildRequest(sourcePath, null), TestContext.Current.CancellationToken);
        Assert.True(build.Outcome == BuildOutcome.Succeeded,
            $"{Path.GetFileName(sourcePath)} did not build: {build.Outcome}\n{string.Join("\n", build.Diagnostics.Select(d => $"{d.Code} line {d.Line}: {d.What}"))}\n{build.Output}");
        return build;
    }

    /// <summary>Builds and runs <paramref name="sourcePath"/>.</summary>
    public static async Task<ProgramRun> RunAsync(string sourcePath, InputScript? input = null, ulong budget = DefaultBudget, bool trace = false) =>
        Run(await BuildAsync(sourcePath), input, budget, trace);

    /// <summary>Loads a built image into a fresh machine and runs it.</summary>
    public static ProgramRun Run(BuildResult build, InputScript? input = null, ulong budget = DefaultBudget, bool trace = false)
    {
        ArgumentNullException.ThrowIfNull(build);
        input ??= InputScript.None;

        var memory = new RealModeMemory();
        var dos = new DosServices(ComLoader.DefaultProgramSegment);
        var router = new InterruptRouter(memory, dos);
        var cpu = new Cpu8086(memory, router);
        var console = new ConsoleDevice();
        var machine = new Machine(cpu, memory, console, router, dos);
        ComLoadResult load = ComLoader.Load(machine, build.Image.Span);
        CpuState initial = cpu.State;

        ulong instructions = 0;
        console.InstructionCountProvider = () => instructions;
        if (input.Replay is { } recording)
        {
            console.SetReplayInputs(recording);
        }

        var states = new List<CpuState>();
        int nextChunk = 0;
        StepResult last = default;
        RunEnd end = RunEnd.BudgetExhausted;
        while (instructions < budget)
        {
            last = cpu.StepInstruction();
            if (last.Outcome == StepOutcome.Executed)
            {
                instructions++;
                if (trace)
                {
                    states.Add(cpu.State);
                }

                continue;
            }

            if (last.Outcome == StepOutcome.WaitingForInput && input.Replay is null && nextChunk < input.Chunks.Count)
            {
                console.EnqueueInput(Encoding.Latin1.GetBytes(input.Chunks[nextChunk++]));
                continue;
            }

            end = last.Outcome switch
            {
                StepOutcome.Terminated => RunEnd.Terminated,
                StepOutcome.WaitingForInput => RunEnd.WaitingForInput,
                _ => RunEnd.Halted,
            };
            break;
        }

        return new ProgramRun(build, load, initial, end, last, Encoding.Latin1.GetString(console.ToByteArray()), instructions,
            states, [.. console.RecordedInputs], router.LastHalt, dos.LastError, machine);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (AssemblyStudio.slnx) not found.");
    }
}
