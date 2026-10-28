using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Loading;

/// <summary>
/// Result metadata from loading a DOS .COM executable image.
/// </summary>
/// <param name="ProgramSegment">Segment address where the program PSP and code are loaded (0x0800).</param>
/// <param name="EntryCs">Initial Code Segment (equal to ProgramSegment).</param>
/// <param name="EntryIp">Initial Instruction Pointer (0x0100).</param>
/// <param name="ImageSize">Byte length of the loaded executable code/data payload.</param>
/// <param name="InitialCpuState">The complete initial 8086 CPU register state.</param>
public readonly record struct ComLoadResult(
    ushort ProgramSegment,
    ushort EntryCs,
    ushort EntryIp,
    int ImageSize,
    CpuState InitialCpuState);
