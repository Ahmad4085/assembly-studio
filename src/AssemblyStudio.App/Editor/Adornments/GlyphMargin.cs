using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace AssemblyStudio.App.Editor.Adornments;

/// <summary>
/// The gutter left of the line numbers (UI-003): a diagnostics column (⨯ ▲ ⓘ, FR-018) and a breakpoint column (● bound or
/// not yet built, ○ unbound; FR-052) with the execution arrow on top (➜, ⨯ for a fault, ■ where the program ended; FR-053).
/// A click in the margin toggles a breakpoint on that line; hovering shows the line's diagnostics or the breakpoint's state.
/// </summary>
public sealed class GlyphMargin : AbstractMargin
{
    private readonly CodeEditor _editor;
    private readonly ToolTip _toolTip = new();

    /// <summary>Creates the margin for <paramref name="editor"/>.</summary>
    public GlyphMargin(CodeEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        _editor = editor;
        Cursor = Cursors.Arrow;
        ToolTip = _toolTip;
        ToolTipService.SetInitialShowDelay(this, 300);
        AutomationProperties.SetName(this, Strings.EditorGutterName);
    }

    private double Cell => Math.Max(10, TextView?.DefaultLineHeight ?? 16);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) => new(Cell * 2 + 2, 0);

    /// <inheritdoc />
    protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
    {
        if (oldTextView is not null)
        {
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
        }

        base.OnTextViewChanged(oldTextView, newTextView);
        if (newTextView is not null)
        {
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize)); // the whole margin takes clicks
        if (TextView is not { VisualLinesValid: true } view)
        {
            return;
        }

        double cell = Cell;
        var breakpoints = _editor.Breakpoints?.ToDictionary(b => b.Line) ?? [];
        Brush breakpointBrush = Resource("Accent.Breakpoint");
        Brush arrowBrush = Resource("Accent.ExecutionArrow");
        foreach (VisualLine visual in view.VisualLines)
        {
            int line = visual.FirstDocumentLine.LineNumber;
            double top = visual.GetTextLineVisualYPosition(visual.TextLines[0], VisualYPosition.LineTop) - view.VerticalOffset;
            double height = visual.GetTextLineVisualYPosition(visual.TextLines[0], VisualYPosition.LineBottom) - view.VerticalOffset - top;
            double size = Math.Min(cell, height);

            if (_editor.LineDiagnostics.TryGetValue(line, out IReadOnlyList<Diagnostic>? diagnostics))
            {
                DiagnosticSeverity severity = diagnostics.Max(d => d.Severity);
                Draw(drawingContext, GlyphShapes.For(severity), Resource(GlyphShapes.Token(severity)), null, 0, top, size);
            }

            if (breakpoints.TryGetValue(line, out EditorBreakpoint? breakpoint))
            {
                bool unbound = breakpoint.Binding == BreakpointBinding.Unbound;
                Draw(drawingContext, GlyphShapes.Breakpoint, unbound ? null : breakpointBrush, unbound ? new Pen(breakpointBrush, 1.5) : null, cell, top, size);
            }

            if (_editor.ExecutionLine == line)
            {
                Geometry marker = _editor.ExecutionMarker == ExecutionMarkerKind.Ended ? GlyphShapes.Ended : GlyphShapes.Arrow;
                Draw(drawingContext, marker, arrowBrush, new Pen(Resource("Surface.Editor"), 0.5), cell, top, size);
                if (_editor.ExecutionMarker == ExecutionMarkerKind.Fault)
                {
                    Draw(drawingContext, GlyphShapes.Error, Resource("Status.Error"), null, 0, top, size);
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseLeftButtonDown(e);
        if (LineAt(e.GetPosition(this)) is int line)
        {
            _editor.RequestToggleBreakpoint(line);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseMove(e);
        string? text = LineAt(e.GetPosition(this)) is int line ? Describe(line) : null;
        _toolTip.Content = text;
        _toolTip.IsOpen = text is not null && IsMouseOver;
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _toolTip.IsOpen = false;
    }

    /// <summary>The margin's text for <paramref name="line"/>: its diagnostics and its breakpoint, or <see langword="null"/>.</summary>
    internal string? Describe(int line)
    {
        var parts = new List<string>();
        if (_editor.LineDiagnostics.TryGetValue(line, out IReadOnlyList<Diagnostic>? diagnostics))
        {
            parts.AddRange(diagnostics.Select(CodeEditor.Describe));
        }

        if (_editor.Breakpoints?.FirstOrDefault(b => b.Line == line) is { } breakpoint)
        {
            parts.Add(breakpoint.Binding switch
            {
                BreakpointBinding.Bound => string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.BreakpointBound), breakpoint.Address),
                BreakpointBinding.Unbound => Strings.BreakpointUnbound,
                _ => Strings.BreakpointNotBuilt,
            });
        }

        return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
    }

    private int? LineAt(Point position)
    {
        if (TextView is not { VisualLinesValid: true } view)
        {
            return null;
        }

        VisualLine? visual = view.GetVisualLineFromVisualTop(position.Y + view.VerticalOffset);
        return visual?.FirstDocumentLine.LineNumber;
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e) => InvalidateVisual();

    private Brush Resource(string token) => _editor.TryFindResource(token) as Brush ?? Brushes.Gray;

    private static void Draw(DrawingContext context, Geometry glyph, Brush? fill, Pen? pen, double left, double top, double size)
    {
        context.PushTransform(new TranslateTransform(left, top));
        context.PushTransform(new ScaleTransform(size, size));
        if (pen is not null)
        {
            pen = new Pen(pen.Brush, pen.Thickness / size);
        }

        context.DrawGeometry(fill, pen, glyph);
        context.Pop();
        context.Pop();
    }
}
