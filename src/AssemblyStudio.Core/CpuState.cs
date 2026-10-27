namespace AssemblyStudio.Core;

/// <summary>
/// Immutable 8086 CPU state: the general registers, segment registers, IP and FLAGS.
/// Published inside snapshots (ADR-0007 §2); the live register file stays inside the engine
/// (ADR-0003 §5: "the product API exposes snapshots, not live arrays").
/// A 16-bit model, 8086-shaped (ADR-0002; generalized before any 32/64-bit CPU work).
/// </summary>
/// <param name="AX">Accumulator AX (AH:AL).</param>
/// <param name="BX">Base BX (BH:BL).</param>
/// <param name="CX">Count CX (CH:CL).</param>
/// <param name="DX">Data DX (DH:DL).</param>
/// <param name="SP">Stack pointer.</param>
/// <param name="BP">Base pointer.</param>
/// <param name="SI">Source index.</param>
/// <param name="DI">Destination index.</param>
/// <param name="CS">Code segment.</param>
/// <param name="DS">Data segment.</param>
/// <param name="ES">Extra segment.</param>
/// <param name="SS">Stack segment.</param>
/// <param name="IP">Instruction pointer (16-bit, wraps).</param>
/// <param name="Flags">FLAGS register; see <see cref="FlagBit"/> for the defined bits.</param>
public readonly record struct CpuState(
    ushort AX,
    ushort BX,
    ushort CX,
    ushort DX,
    ushort SP,
    ushort BP,
    ushort SI,
    ushort DI,
    ushort CS,
    ushort DS,
    ushort ES,
    ushort SS,
    ushort IP,
    ushort Flags);
