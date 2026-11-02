using System.Windows.Media;
using AssemblyStudio.Core;

namespace AssemblyStudio.App.Editor.Adornments;

/// <summary>Whether a breakpoint's line has code (ui-spec §5: ● bound, ○ unbound).</summary>
public enum BreakpointBinding
{
    /// <summary>No build yet: the line is marked but not checked.</summary>
    NotBuilt,

    /// <summary>The line has code; <see cref="EditorBreakpoint.Address"/> says where.</summary>
    Bound,

    /// <summary>The line has no code (a comment, blank or data line); the breakpoint never triggers.</summary>
    Unbound,
}

/// <summary>What the execution marker shows (ui-spec §3 state table).</summary>
public enum ExecutionMarkerKind
{
    /// <summary>Paused on the line: the next instruction to execute (➜ and the execution-line background).</summary>
    Current,

    /// <summary>Stopped by a runtime error or an unsupported instruction or service on the line (➜ with ⨯).</summary>
    Fault,

    /// <summary>The program ended on the line ("ended here", ■).</summary>
    Ended,
}

/// <summary>A breakpoint as the editor shows it.</summary>
/// <param name="Line">The 1-based source line.</param>
/// <param name="Binding">Whether the line has code.</param>
/// <param name="Address">The bound address as text (for example <c>0800:0105</c>), when bound.</param>
/// <param name="IsEnabled">Whether the breakpoint stops the program (the breakpoints panel disables it, UI-014).</param>
public sealed record EditorBreakpoint(int Line, BreakpointBinding Binding = BreakpointBinding.NotBuilt, string? Address = null, bool IsEnabled = true);

/// <summary>
/// The gutter glyph shapes (design tokens §3: ● breakpoint, ○ unbound, ➜ execution, ⨯ error, ▲ warning, ⓘ info). Each kind
/// has its own shape, so the meaning doesn't depend on color (high contrast draws them all in one color). Geometries are
/// in a 1 × 1 box and scaled to the line height.
/// </summary>
public static class GlyphShapes
{
    /// <summary>● A set or bound breakpoint (filled).</summary>
    public static Geometry Breakpoint { get; } = Freeze(new EllipseGeometry(new System.Windows.Point(0.5, 0.5), 0.36, 0.36));

    /// <summary>○ An unbound breakpoint (drawn as an outline only).</summary>
    public static Geometry UnboundBreakpoint => Breakpoint;

    /// <summary>➜ The execution arrow.</summary>
    public static Geometry Arrow { get; } = Freeze(Geometry.Parse("M 0.1,0.35 L 0.5,0.35 L 0.5,0.15 L 0.9,0.5 L 0.5,0.85 L 0.5,0.65 L 0.1,0.65 Z"));

    /// <summary>⨯ An error (a cross).</summary>
    public static Geometry Error { get; } = Freeze(Geometry.Parse("M 0.2,0.32 L 0.32,0.2 L 0.5,0.38 L 0.68,0.2 L 0.8,0.32 L 0.62,0.5 L 0.8,0.68 L 0.68,0.8 L 0.5,0.62 L 0.32,0.8 L 0.2,0.68 L 0.38,0.5 Z"));

    /// <summary>▲ A warning (a triangle).</summary>
    public static Geometry Warning { get; } = Freeze(Geometry.Parse("M 0.5,0.12 L 0.9,0.86 L 0.1,0.86 Z"));

    /// <summary>ⓘ Information (a circle with a gap: the "i").</summary>
    public static Geometry Info { get; } = Freeze(Geometry.Parse("M 0.5,0.12 A 0.38,0.38 0 1 1 0.4999,0.12 Z M 0.44,0.42 L 0.56,0.42 L 0.56,0.78 L 0.44,0.78 Z M 0.44,0.24 L 0.56,0.24 L 0.56,0.34 L 0.44,0.34 Z"));

    /// <summary>■ The line where the program ended.</summary>
    public static Geometry Ended { get; } = Freeze(new RectangleGeometry(new System.Windows.Rect(0.25, 0.25, 0.5, 0.5)));

    /// <summary>The glyph of a diagnostic severity.</summary>
    public static Geometry For(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => Error,
        DiagnosticSeverity.Warning => Warning,
        _ => Info,
    };

    /// <summary>The theme token of a diagnostic severity.</summary>
    public static string Token(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "Status.Error",
        DiagnosticSeverity.Warning => "Status.Warning",
        _ => "Status.Info",
    };

    private static Geometry Freeze(Geometry geometry)
    {
        geometry.Freeze();
        return geometry;
    }
}
