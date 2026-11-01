using AssemblyStudio.Core;
using AssemblyStudio.Debugging;
using AssemblyStudio.Engine.I8086;
using AssemblyStudio.Engine.I8086.Disassembly;
using AssemblyStudio.Os.Dos;
using AssemblyStudio.Runtime;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Loading;
using AssemblyStudio.Runtime.Memory;

namespace AssemblyStudio.App.Commands;

/// <summary>
/// Creates the debug session for a successful build (ADR-0007): a fresh 8086 machine with real-mode memory, the DOS
/// services and a console, with the .COM image loaded at <see cref="ComLoader.DefaultProgramSegment"/>. Reset reloads the
/// same image into a new machine.
/// </summary>
public static class SessionFactory
{
    private static readonly IcedDisassembler Disassembler = new();

    /// <summary>The bundled NASM next to the application (FR-079); Settings may name another one.</summary>
    public static string BundledAssemblerPath => System.IO.Path.Combine(AppContext.BaseDirectory, "tools", "nasm", "nasm.exe");

    /// <summary>The segment the program is loaded at (for breakpoint addresses).</summary>
    public const ushort ProgramSegment = ComLoader.DefaultProgramSegment;

    /// <summary>A session over <paramref name="build"/>'s image and source map.</summary>
    /// <param name="build">A successful build.</param>
    /// <param name="instructionBudget">Instructions one Run may execute before it pauses (FR-042, Settings).</param>
    /// <param name="faultLog">Receives engine faults (NFR-012), or <see langword="null"/>.</param>
    public static IDebugSession Create(BuildResult build, ulong instructionBudget, Action<Exception>? faultLog = null)
    {
        ArgumentNullException.ThrowIfNull(build);
        byte[] image = build.Image.ToArray();
        var session = new DebugSession(
            () =>
            {
                var memory = new RealModeMemory();
                var dos = new DosServices(ProgramSegment);
                var router = new InterruptRouter(memory, dos);
                var machine = new Machine(new Cpu8086(memory, router), memory, new ConsoleDevice(), router, dos);
                ComLoader.Load(machine, image);
                return machine;
            },
            Disassembler,
            build.SourceMap,
            faultLog)
        {
            InstructionBudget = instructionBudget,
        };
        return session;
    }
}
