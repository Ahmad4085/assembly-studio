using AssemblyStudio.Core;

namespace AssemblyStudio.Os.Dos;

/// <summary>
/// The MVP DOS service subset (C06, ADR-0004 §4, §7) with DOS 3.3/4.0 semantics from the MS-DOS v4.0
/// source (`docs/02-research/dos-services.md` §2): INT 21h AH=00h and 4Ch (terminate), 01h (read
/// character with echo), 02h (write character), 07h and 08h (read character without echo), 09h (write
/// '$'-terminated string), 0Ah (buffered line input), INT 20h (terminate) and the default INT 23h
/// (Ctrl-C: terminate).
/// <para>
/// Like the DOS kernel, the service keeps its own output column: TAB is expanded to spaces up to the
/// next multiple of 8, CR resets the column, BS moves it back one (not destructive), and DEL and the
/// other bytes below 20h are output raw without advancing it.
/// </para>
/// <para>
/// Input comes from the console queue one byte per call. An extended key is queued as 00h followed
/// by its scan code, so 01h/07h/08h return 00h and the next call returns the scan code (01h echoes
/// both bytes, as DOS does). When the queue is empty the call returns
/// <see cref="StepOutcome.WaitingForInput"/> with the state untouched: CS:IP stays on the INT, so the
/// call re-executes when input arrives (ADR-0004 §5).
/// </para>
/// <para>
/// Break checks (01h, 02h, 08h, 09h, 0Ah; never 07h): a queued ^C starts the Ctrl-C protocol, ^S
/// pauses until the next key (^C after it still breaks), and ^P is consumed and ignored (printer echo
/// is unsupported). Other keys stay queued. Input only arrives between instructions, so checking once
/// before each character read and once before a 02h/09h output is the same as DOS's per-character
/// check (a documented deviation from v4's every-64th-character timing, R-DOS §4).
/// </para>
/// <para>
/// Ctrl-C prints <c>^C</c> CR LF and issues INT 23h. With the default vector the program terminates
/// (<see cref="TerminationPath.CtrlC"/>). A program-installed handler runs with the INT 21h instruction
/// as its return address: IRET, or RETF with CF clear, retries the function; RETF with CF set
/// terminates (the DOS 2.0+ protocol, CTRLC.ASM).
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
    private const byte CtrlC = 0x03;
    private const byte CtrlP = 0x10;
    private const byte CtrlS = 0x13;
    private const byte Bel = 0x07;
    private const byte Backspace = 0x08;
    private const byte Tab = 0x09;
    private const byte LineFeed = 0x0A;
    private const byte CarriageReturn = 0x0D;
    private const byte Escape = 0x1B;
    private const byte Delete = 0x7F;

    /// <summary>The line being edited by a 0Ah call that is waiting for input, or <see langword="null"/>.</summary>
    private LineEdit? _line;

    /// <summary>Set after ^S: the next key resumes output and is discarded (^C still breaks).</summary>
    private bool _paused;

    /// <summary>The INT 21h a program Ctrl-C handler returns to, with SP before the INT 23h frame.</summary>
    private (ushort Cs, ushort Ip, ushort Sp)? _ctrlCReturn;

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

    /// <summary>Resets the per-program state (output column, pending input and Ctrl-C state, last
    /// error) for a new load or Reset.</summary>
    public void Reset()
    {
        Column = 0;
        LastError = null;
        _line = null;
        _paused = false;
        _ctrlCReturn = null;
    }

    /// <inheritdoc />
    public StepResult? HandleService(byte vector, CpuState state, IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        switch (vector)
        {
            case 0x20:
                return TerminateRequiringPsp(state, state.IP == 0x0000 ? TerminationPath.ReturnToPsp : TerminationPath.Int20h, "INT 20h");

            case 0x23:
                // The default Ctrl-C handler (the vector is still the default one, or the router
                // would have vectored to the program's handler).
                return Terminated(state, TerminationPath.CtrlC, 0x00);

            case 0x21:
                break;

            default:
                return null;
        }

        if (ReturnFromCtrlCHandler(ref state, machine) is { } ended)
        {
            return ended;
        }

        byte function = (byte)(state.AX >> 8);
        return function switch
        {
            0x00 => TerminateRequiringPsp(state, TerminationPath.Int21hFunction00h, "INT 21h AH=00h"),
            0x01 => ReadCharacter(state, machine, echo: true, checkBreak: true),
            0x02 => WriteCharacter(state, machine),
            0x07 => ReadCharacter(state, machine, echo: false, checkBreak: false),
            0x08 => ReadCharacter(state, machine, echo: false, checkBreak: true),
            0x09 => WriteString(state, machine),
            0x0A => ReadLine(state, machine),
            0x4C => Terminated(state, TerminationPath.Int21hFunction4Ch, (byte)state.AX),
            _ => Halted(state, HaltReason.UnsupportedService),
        };
    }

    /// <summary>AH=01h/07h/08h: AL returns the next input byte; 01h echoes it through the output path
    /// (a TAB echoes as spaces but AL keeps 09h).</summary>
    private StepResult ReadCharacter(CpuState state, IMachine machine, bool echo, bool checkBreak)
    {
        if (checkBreak && CheckBreak(state, machine) is { } stop)
        {
            return stop;
        }

        if (!machine.Console.TryReadInput(out byte value))
        {
            return Waiting(state);
        }

        if (echo)
        {
            Output(machine.Console, value);
        }

        return Completed(state, machine, value);
    }

    /// <summary>AH=02h: writes DL; AL returns the last character output (20h for a TAB).</summary>
    private StepResult WriteCharacter(CpuState state, IMachine machine)
    {
        if (CheckBreak(state, machine) is { } stop)
        {
            return stop;
        }

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

        if (CheckBreak(state, machine) is { } stop)
        {
            return stop;
        }

        for (int i = 0; i < length; i++)
        {
            Output(machine.Console, machine.Memory.Read8(state.DS, (ushort)(state.DX + i)));
        }

        return Completed(state, machine, StringTerminator);
    }

    /// <summary>
    /// AH=0Ah: reads a line into the buffer at DS:DX (STRIN.ASM, MVP editing keys). Byte 0 is the
    /// maximum including the CR (0 returns at once); on Enter, byte 1 receives the count without the
    /// CR and the characters follow, ending with CR. Enter echoes CR only. Control characters echo as
    /// <c>^X</c> and TAB as spaces; BS and DEL erase the last character's cells; LF echoes CR LF and is
    /// not stored; Esc prints <c>\</c> CR LF and restarts the line; a character that doesn't fit
    /// sounds BEL. Template keys (extended keys) are consumed and ignored in the MVP. The line is
    /// written to memory on Enter; while waiting for input it is kept here.
    /// </summary>
    private StepResult ReadLine(CpuState state, IMachine machine)
    {
        IMemory memory = machine.Memory;
        byte max = memory.Read8(state.DS, state.DX);
        if (max == 0)
        {
            return Completed(state, machine, (byte)state.AX);
        }

        if (_line is not { } line || line.Cs != state.CS || line.Ip != state.IP)
        {
            line = new LineEdit(state.CS, state.IP);
            _line = line;
        }

        IConsoleDevice console = machine.Console;
        while (true)
        {
            if (line.SkipScanCode)
            {
                if (!console.TryReadInput(out _))
                {
                    return Waiting(state);
                }

                line.SkipScanCode = false;
                continue;
            }

            if (CheckBreak(state, machine) is { } stop)
            {
                if (stop.Outcome != StepOutcome.WaitingForInput)
                {
                    _line = null;
                }

                return stop;
            }

            if (!console.TryReadInput(out byte key))
            {
                return Waiting(state);
            }

            switch (key)
            {
                case CarriageReturn:
                    Output(console, CarriageReturn);
                    memory.Write8(state.DS, (ushort)(state.DX + 1), (byte)line.Chars.Count);
                    for (int i = 0; i < line.Chars.Count; i++)
                    {
                        memory.Write8(state.DS, (ushort)(state.DX + 2 + i), line.Chars[i]);
                    }

                    memory.Write8(state.DS, (ushort)(state.DX + 2 + line.Chars.Count), CarriageReturn);
                    _line = null;
                    return Completed(state, machine, (byte)state.AX);

                case LineFeed:
                    Output(console, CarriageReturn);
                    Output(console, LineFeed);
                    break;

                case Backspace or Delete:
                    if (line.Chars.Count > 0)
                    {
                        int cells = line.Cells[^1];
                        line.Chars.RemoveAt(line.Chars.Count - 1);
                        line.Cells.RemoveAt(line.Cells.Count - 1);
                        for (int i = 0; i < cells; i++)
                        {
                            Output(console, Backspace);
                            Output(console, (byte)' ');
                            Output(console, Backspace);
                        }
                    }

                    break;

                case Escape:
                    Output(console, (byte)'\\');
                    Output(console, CarriageReturn);
                    Output(console, LineFeed);
                    line.Chars.Clear();
                    line.Cells.Clear();
                    break;

                case 0x00:
                    line.SkipScanCode = true;
                    break;

                default:
                    if (line.Chars.Count >= max - 1)
                    {
                        Output(console, Bel);
                        break;
                    }

                    line.Chars.Add(key);
                    line.Cells.Add(EchoInLine(console, key));
                    break;
            }
        }
    }

    /// <summary>Echoes a stored 0Ah character and returns the number of cells it took.</summary>
    private int EchoInLine(IConsoleDevice console, byte key)
    {
        if (key == Tab)
        {
            byte before = Column;
            Output(console, Tab);
            return (byte)(Column - before);
        }

        if (key < 0x20)
        {
            Output(console, (byte)'^');
            Output(console, (byte)(key + 0x40));
            return 2;
        }

        Output(console, key);
        return 1;
    }

    /// <summary>
    /// The DOS break check (STATCHK): handles a queued ^C, ^S or ^P and leaves any other key queued.
    /// Returns <see langword="null"/> to continue the function, <see cref="StepOutcome.WaitingForInput"/>
    /// while paused by ^S, or the result of the Ctrl-C protocol.
    /// </summary>
    private StepResult? CheckBreak(CpuState state, IMachine machine)
    {
        IConsoleDevice console = machine.Console;
        while (true)
        {
            if (_paused)
            {
                if (!console.TryReadInput(out byte resume))
                {
                    return Waiting(state);
                }

                _paused = false;
                if (resume == CtrlC)
                {
                    return BreakIn(state, machine);
                }

                continue;
            }

            if (!console.TryPeekInput(out byte next))
            {
                return null;
            }

            switch (next)
            {
                case CtrlC:
                    console.TryReadInput(out _);
                    return BreakIn(state, machine);

                case CtrlS:
                    console.TryReadInput(out _);
                    _paused = true;
                    break;

                case CtrlP:
                    console.TryReadInput(out _); // printer echo is unsupported (R-DOS §4)
                    break;

                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// The Ctrl-C protocol: prints <c>^C</c> CR LF and issues INT 23h through the router. The default
    /// vector comes back here as vector 23h and terminates; a program-installed vector is entered with
    /// a real interrupt frame whose return address is this INT 21h, so the function is retried when the
    /// handler returns (see <see cref="ReturnFromCtrlCHandler"/>).
    /// </summary>
    private StepResult BreakIn(CpuState state, IMachine machine)
    {
        Output(machine.Console, (byte)'^');
        Output(machine.Console, (byte)'C');
        Output(machine.Console, CarriageReturn);
        Output(machine.Console, LineFeed);
        _line = null;

        if (machine.InterruptRouter?.Intercept(0x23, state) is { } handled)
        {
            return handled;
        }

        if (machine.InterruptRouter is null)
        {
            return Terminated(state, TerminationPath.CtrlC, 0x00);
        }

        IMemory memory = machine.Memory;
        ushort sp = state.SP;
        sp -= 2;
        memory.Write16(state.SS, sp, state.Flags);
        sp -= 2;
        memory.Write16(state.SS, sp, state.CS);
        sp -= 2;
        memory.Write16(state.SS, sp, state.IP);
        _ctrlCReturn = (state.CS, state.IP, state.SP);

        machine.Cpu.SetState(state with
        {
            SP = sp,
            IP = memory.Read16(0, 0x23 * 4),
            CS = memory.Read16(0, (0x23 * 4) + 2),
            Flags = (ushort)(state.Flags & ~(ushort)(FlagBit.IF | FlagBit.TF)),
        });
        return new StepResult(StepOutcome.Executed, state.CS, state.IP, HaltReason.None, FlagBit.None);
    }

    /// <summary>
    /// The INT 21h after a program Ctrl-C handler returned to it. IRET leaves SP as it was before the
    /// INT 23h frame: the function is retried. RETF leaves the FLAGS word on the stack: it is removed,
    /// and the carry flag the handler returned with (the live CF, not the stacked word) decides: set
    /// terminates the program, clear retries.
    /// </summary>
    private StepResult? ReturnFromCtrlCHandler(ref CpuState state, IMachine machine)
    {
        if (_ctrlCReturn is not { } frame || frame.Cs != state.CS || frame.Ip != state.IP)
        {
            return null;
        }

        _ctrlCReturn = null;
        if (state.SP != (ushort)(frame.Sp - 2))
        {
            return null;
        }

        state = state with { SP = frame.Sp };
        machine.Cpu.SetState(state);
        return (state.Flags & (ushort)FlagBit.CF) != 0 ? Terminated(state, TerminationPath.CtrlC, 0x00) : null;
    }

    /// <summary>The DOS console output path (CPMIO2.ASM): returns the last byte actually output.</summary>
    private byte Output(IConsoleDevice console, byte value)
    {
        switch (value)
        {
            case Tab:
                do
                {
                    console.WriteOutput((byte)' ');
                    Column++;
                }
                while (Column % 8 != 0);
                return (byte)' ';

            case CarriageReturn:
                Column = 0;
                break;

            case Backspace:
                if (Column > 0)
                {
                    Column--;
                }

                break;

            default:
                if (value >= 0x20 && value != Delete)
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

    private StepResult Terminated(CpuState state, TerminationPath path, byte exitCode)
    {
        _line = null;
        _paused = false;
        _ctrlCReturn = null;
        return new StepResult(StepOutcome.Terminated, state.CS, state.IP, HaltReason.None, FlagBit.None)
        {
            Termination = new ProgramTermination(path, exitCode),
        };
    }

    private StepResult Refused(CpuState state, DosServiceError error)
    {
        LastError = error;
        return Halted(state, HaltReason.RuntimeError);
    }

    private static StepResult Halted(CpuState state, HaltReason reason) =>
        new(StepOutcome.Halted, state.CS, state.IP, reason, FlagBit.None);

    private static StepResult Waiting(CpuState state) =>
        new(StepOutcome.WaitingForInput, state.CS, state.IP, HaltReason.None, FlagBit.None);

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

    /// <summary>A 0Ah line in progress: the characters typed so far and the cells each one's echo took.</summary>
    private sealed class LineEdit(ushort cs, ushort ip)
    {
        public ushort Cs { get; } = cs;

        public ushort Ip { get; } = ip;

        public List<byte> Chars { get; } = [];

        public List<int> Cells { get; } = [];

        public bool SkipScanCode { get; set; }
    }
}
