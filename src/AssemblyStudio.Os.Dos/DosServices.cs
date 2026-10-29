using AssemblyStudio.Core;

namespace AssemblyStudio.Os.Dos;

/// <summary>
/// The MVP DOS service subset (C06, ADR-0004 §4, §7) with DOS 3.3/4.0 semantics from the MS-DOS v4.0
/// source (`docs/02-research/dos-services.md` §2): INT 21h AH=02h (write character), 09h (write
/// '$'-terminated string), 00h and 4Ch (terminate), and INT 20h (terminate). The input functions
/// 01h, 07h, 08h and 0Ah are TASK-035/036; until then they halt as unsupported (FR-040).
/// <para>
/// Like the DOS kernel, the service keeps its own output column: TAB is expanded to spaces up to the
/// next multiple of 8, CR resets the column, BS moves it back one (not destructive), and DEL and the
/// other bytes below 20h are output raw without advancing it.
/// </para>
/// <para>
/// A handled call moves IP past the INT instruction and writes register results through
/// <see cref="ICpu.SetState"/>. A termination leaves CS:IP on the INT instruction and returns
/// <see cref="StepOutcome.Terminated"/> with the path and exit code (FR-041).
/// </para>
/// </summary>
public sealed class DosServices : IOsServices
{
    /// <summary>The program segment the .COM loader uses (ADR-0004 §2).</summary>
    public const ushort DefaultPspSegment = 0x0800;

    private const byte IntOpcode = 0xCD;
    private const byte StringTerminator = (byte)'$';

    /// <summary>Creates the service layer for a program whose PSP is at <paramref name="pspSegment"/>.</summary>
    /// <param name="pspSegment">Segment of the running program's PSP (the "current PSP" of DOS).</param>
    public DosServices(ushort pspSegment = DefaultPspSegment)
    {
        PspSegment = pspSegment;
    }

    /// <summary>Segment of the running program's PSP. INT 20h and AH=00h require CS to equal it.</summary>
    public ushort PspSegment { get; set; }

    /// <summary>The DOS output column (0-based, a byte as in the DOS kernel), used for TAB expansion.</summary>
    public byte Column { get; private set; }

    /// <summary>The most recent refused service call, or <see langword="null"/>.</summary>
    public DosServiceError? LastError { get; private set; }

    /// <summary>Resets the per-program state (output column and last error) for a new load or Reset.</summary>
    public void Reset()
    {
        Column = 0;
        LastError = null;
    }

    /// <inheritdoc />
    public StepResult? HandleService(byte vector, CpuState state, IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        if (vector == 0x20)
        {
            return TerminateRequiringPsp(state, state.IP == 0x0000 ? TerminationPath.ReturnToPsp : TerminationPath.Int20h, "INT 20h");
        }

        if (vector != 0x21)
        {
            return null;
        }

        byte function = (byte)(state.AX >> 8);
        return function switch
        {
            0x00 => TerminateRequiringPsp(state, TerminationPath.Int21hFunction00h, "INT 21h AH=00h"),
            0x02 => WriteCharacter(state, machine),
            0x09 => WriteString(state, machine),
            0x4C => Terminated(state, TerminationPath.Int21hFunction4Ch, (byte)state.AX),
            _ => Halted(state, HaltReason.UnsupportedService),
        };
    }

    /// <summary>AH=02h: writes DL; AL returns the last character output (20h for a TAB).</summary>
    private StepResult WriteCharacter(CpuState state, IMachine machine)
    {
        byte last = Output(machine.Console, (byte)state.DX);
        return Completed(state, machine, last);
    }

    /// <summary>
    /// AH=09h: writes DS:DX up to '$' (not printed) through the AH=02h path; AL returns 24h. The offset
    /// wraps within the segment. Deviation: real DOS keeps printing forever when the segment holds no
    /// '$'; the emulator refuses the call instead (DOS002), before writing anything.
    /// </summary>
    private StepResult WriteString(CpuState state, IMachine machine)
    {
        int length = 0;
        while (length <= ushort.MaxValue && machine.Memory.Read8(state.DS, (ushort)(state.DX + length)) != StringTerminator)
        {
            length++;
        }

        if (length > ushort.MaxValue)
        {
            return Refused(state, new DosServiceError(
                "DOS002",
                $"INT 21h AH=09h found no '$' terminator in the 64 KiB segment starting at {state.DS:X4}:{state.DX:X4}",
                Where(state),
                "End the string with '$' (for example: msg db 'Hello', 13, 10, '$') and check that DS:DX points to it."));
        }

        for (int i = 0; i < length; i++)
        {
            Output(machine.Console, machine.Memory.Read8(state.DS, (ushort)(state.DX + i)));
        }

        return Completed(state, machine, StringTerminator);
    }

    /// <summary>The DOS console output path (CPMIO2.ASM): returns the last byte actually output.</summary>
    private byte Output(IConsoleDevice console, byte value)
    {
        switch (value)
        {
            case 0x09:
                do
                {
                    console.WriteOutput(0x20);
                    Column++;
                }
                while (Column % 8 != 0);
                return 0x20;

            case 0x0D:
                Column = 0;
                break;

            case 0x08:
                if (Column > 0)
                {
                    Column--;
                }

                break;

            default:
                if (value >= 0x20 && value != 0x7F)
                {
                    Column++;
                }

                break;
        }

        console.WriteOutput(value);
        return value;
    }

    /// <summary>INT 20h and AH=00h terminate the program whose PSP is CS, so CS must be the PSP.</summary>
    private StepResult TerminateRequiringPsp(CpuState state, TerminationPath path, string service)
    {
        if (state.CS != PspSegment)
        {
            return Refused(state, new DosServiceError(
                "DOS001",
                $"{service} requires CS to be the program segment (PSP {PspSegment:X4}h), but CS is {state.CS:X4}h",
                Where(state),
                "Use INT 21h AH=4Ch (exit code in AL), which works from any code segment, or call this service with CS = PSP."));
        }

        return Terminated(state, path, 0x00);
    }

    private static StepResult Terminated(CpuState state, TerminationPath path, byte exitCode) =>
        new(StepOutcome.Terminated, state.CS, state.IP, HaltReason.None, FlagBit.None)
        {
            Termination = new ProgramTermination(path, exitCode),
        };

    private StepResult Refused(CpuState state, DosServiceError error)
    {
        LastError = error;
        return Halted(state, HaltReason.RuntimeError);
    }

    private static StepResult Halted(CpuState state, HaltReason reason) =>
        new(StepOutcome.Halted, state.CS, state.IP, reason, FlagBit.None);

    /// <summary>Sets AL and moves IP past the INT instruction.</summary>
    private static StepResult Completed(CpuState state, IMachine machine, byte al)
    {
        machine.Cpu.SetState(state with
        {
            AX = (ushort)((state.AX & 0xFF00) | al),
            IP = NextIp(state, machine.Memory),
        });
        return new StepResult(StepOutcome.Executed, state.CS, state.IP, HaltReason.None, FlagBit.None);
    }

    /// <summary>The address after <c>INT imm8</c>, skipping any prefix bytes before the opcode.</summary>
    private static ushort NextIp(CpuState state, IMemory memory)
    {
        ushort ip = state.IP;
        for (int i = 0; i < 16 && memory.Read8(state.CS, ip) != IntOpcode; i++)
        {
            ip++;
        }

        return (ushort)(ip + 2);
    }

    private static string Where(CpuState state) => $"at {state.CS:X4}:{state.IP:X4}";
}
