namespace AssemblyStudio.Core;

/// <summary>
/// The 8086 FLAGS register bits (Intel 8086 reference manual). Used for flag values and for the
/// mask of flags left <c>undefined</c> by the last instruction (ADR-0003 §3: the engine computes a
/// deterministic value and reports the mask; the UI shows masked flags as "undefined", FR-048).
/// </summary>
[Flags]
public enum FlagBit : ushort
{
    None = 0x0000,
    CF = 0x0001,
    PF = 0x0004,
    AF = 0x0010,
    ZF = 0x0040,
    SF = 0x0080,
    TF = 0x0100,
    IF = 0x0200,
    DF = 0x0400,
    OF = 0x0800,
}
