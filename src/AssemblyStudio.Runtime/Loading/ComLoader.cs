using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Memory;

namespace AssemblyStudio.Runtime.Loading;

/// <summary>
/// Loads DOS .COM executable binary images into real-mode memory with the documented initial state
/// (FR-034, AC-021; ADR-0004 §2).
/// Places the PSP at 0800h, the executable image at PSP:0100h, return word 0000h at SS:SP (FFFEh),
/// and initializes all CPU registers to their documented baseline.
/// </summary>
public static class ComLoader
{
    /// <summary>
    /// Default fixed program segment for .COM loading (0x0800, ADR-0004 §2).
    /// </summary>
    public const ushort DefaultProgramSegment = 0x0800;

    /// <summary>
    /// Entry point offset within the program segment for .COM programs (0x0100).
    /// </summary>
    public const ushort DefaultEntryPoint = 0x0100;

    /// <summary>
    /// Initial stack pointer for a full 64 KiB segment (0xFFFE).
    /// </summary>
    public const ushort DefaultInitialSp = 0xFFFE;

    /// <summary>
    /// Maximum allowable .COM image size in bytes (65,278 bytes = 0xFEFE = 64 KiB - 256-byte PSP - 2-byte stack return word).
    /// </summary>
    public const int MaxComImageSize = DefaultInitialSp - DefaultEntryPoint; // 65,278 bytes

    /// <summary>
    /// Initial FLAGS register value: bit 1 is always 1 in 8086 architecture.
    /// </summary>
    public const ushort InitialFlags = 0x0002;

    /// <summary>
    /// Loads a .COM image into the given machine, setting up memory (PSP, image, stack word) and configuring initial CPU state.
    /// Clears the memory journal so the loaded baseline starts with zero changes (FR-056).
    /// </summary>
    /// <param name="machine">The target emulated machine.</param>
    /// <param name="image">Raw .COM executable bytes.</param>
    /// <param name="commandTail">Optional command line arguments (defaults to empty).</param>
    /// <param name="programSegment">Target program segment (defaults to 0x0800).</param>
    /// <returns>Load details including entry CS:IP, image size, and initial CPU state.</returns>
    public static ComLoadResult Load(
        IMachine machine,
        ReadOnlySpan<byte> image,
        string? commandTail = null,
        ushort programSegment = DefaultProgramSegment)
    {
        ArgumentNullException.ThrowIfNull(machine);
        return Load(machine.Memory, machine.Cpu, image, commandTail, programSegment);
    }

    /// <summary>
    /// Loads a .COM image into the specified memory and optionally applies initial register state to the CPU.
    /// </summary>
    /// <param name="memory">Target memory to populate.</param>
    /// <param name="cpu">Target CPU to initialize, or <see langword="null"/> if only memory is being configured.</param>
    /// <param name="image">Raw .COM executable bytes.</param>
    /// <param name="commandTail">Optional command line arguments.</param>
    /// <param name="programSegment">Target program segment (defaults to 0x0800).</param>
    /// <returns>Load details including entry CS:IP, image size, and initial CPU state.</returns>
    public static ComLoadResult Load(
        IMemory memory,
        ICpu? cpu,
        ReadOnlySpan<byte> image,
        string? commandTail = null,
        ushort programSegment = DefaultProgramSegment)
    {
        ArgumentNullException.ThrowIfNull(memory);

        if (image.Length > MaxComImageSize)
        {
            throw new ArgumentException(
                $"COM image size ({image.Length} bytes) exceeds maximum allowable size of {MaxComImageSize} bytes.",
                nameof(image));
        }

        // 1. Initialize the 256-byte PSP at programSegment:0000h
        ProgramSegmentPrefix.Initialize(memory, programSegment, commandTail);

        // 2. Load the binary image at programSegment:0100h
        if (memory is RealModeMemory rmm)
        {
            int imagePhysicalAddress = RealModeMemory.ToPhysicalAddress(programSegment, DefaultEntryPoint);
            rmm.LoadBytes(imagePhysicalAddress, image);
        }
        else
        {
            for (int i = 0; i < image.Length; i++)
            {
                memory.Write8(programSegment, (ushort)(DefaultEntryPoint + i), image[i]);
            }
        }

        // 3. Store word 0000h at SS:SP (programSegment:FFFEh)
        // This is the return address for near RET to PSP:0000h (INT 20h termination).
        memory.Write16(programSegment, DefaultInitialSp, 0x0000);

        // 4. Clear the memory journal so that initial loading is not counted as execution changes (FR-056)
        memory.TakeJournal();

        // 5. Construct initial CPU register state
        // CS=DS=ES=SS=programSegment, IP=0100h, SP=FFFEh, AX=BX=CX=DX=SI=DI=BP=0000h (ADR-0004 §2)
        var initialState = new CpuState(
            AX: 0x0000,
            BX: 0x0000,
            CX: 0x0000,
            DX: 0x0000,
            SP: DefaultInitialSp,
            BP: 0x0000,
            SI: 0x0000,
            DI: 0x0000,
            CS: programSegment,
            DS: programSegment,
            ES: programSegment,
            SS: programSegment,
            IP: DefaultEntryPoint,
            Flags: InitialFlags);

        cpu?.SetState(initialState);

        return new ComLoadResult(
            ProgramSegment: programSegment,
            EntryCs: programSegment,
            EntryIp: DefaultEntryPoint,
            ImageSize: image.Length,
            InitialCpuState: initialState);
    }
}
