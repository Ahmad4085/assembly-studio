namespace AssemblyStudio.Core;

/// <summary>
/// A line breakpoint set in source code (FR-052, ADR-0007 §2).
/// Bound when the source file and line map to a code offset in the program segment.
/// </summary>
/// <param name="Location">The source file and line.</param>
/// <param name="IsBound">Whether the breakpoint maps to an offset in the program segment.</param>
/// <param name="Offset">The offset in the program segment when bound, or <see langword="null"/>.</param>
/// <param name="IsEnabled">Whether the breakpoint is enabled.</param>
public readonly record struct Breakpoint(
    SourceLocation Location,
    bool IsBound,
    ushort? Offset = null,
    bool IsEnabled = true);
