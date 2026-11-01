using System.Windows;
using System.Windows.Media;
using AssemblyStudio.Core;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace AssemblyStudio.App.Editor.Adornments;

/// <summary>
/// FR-018 squiggles: a wavy underline under the text of each line that has a diagnostic (NASM reports lines only), in the
/// severity's color. The line style differs too: errors are wavy, warnings wavy with a longer period, infos dotted, so the
/// underline alone doesn't depend on color.
/// </summary>
public sealed class DiagnosticRenderer(CodeEditor editor) : IBackgroundRenderer
{
    /// <inheritdoc />
    public KnownLayer Layer => KnownLayer.Selection;

    /// <inheritdoc />
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(drawingContext);
        if (editor.LineDiagnostics.Count == 0 || !textView.VisualLinesValid)
        {
            return;
        }

        foreach (VisualLine visual in textView.VisualLines)
        {
            int line = visual.FirstDocumentLine.LineNumber;
            if (!editor.LineDiagnostics.TryGetValue(line, out IReadOnlyList<Diagnostic>? diagnostics))
            {
                continue;
            }

            DiagnosticSeverity severity = diagnostics.Max(d => d.Severity);
            DocumentLine documentLine = visual.FirstDocumentLine;
            string text = editor.Document.GetText(documentLine);
            int start = documentLine.Offset + (text.Length - text.TrimStart().Length);
            int end = documentLine.Offset + text.TrimEnd().Length;
            if (end <= start)
            {
                start = documentLine.Offset;
                end = documentLine.EndOffset;
            }

            var segment = new TextSegment { StartOffset = start, EndOffset = end };
            var brush = editor.TryFindResource(GlyphShapes.Token(severity)) as Brush ?? Brushes.Red;
            foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                double width = Math.Max(rect.Width, 6);
                drawingContext.DrawGeometry(null, Pen(brush, severity), Underline(rect.BottomLeft, width, severity));
            }
        }
    }

    /// <summary>The underline path: a zigzag (errors, warnings) or a straight line (infos, dotted by the pen).</summary>
    internal static Geometry Underline(Point start, double width, DiagnosticSeverity severity)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            double y = start.Y - 1;
            context.BeginFigure(new Point(start.X, y), false, false);
            if (severity == DiagnosticSeverity.Info)
            {
                context.LineTo(new Point(start.X + width, y), true, false);
            }
            else
            {
                double step = severity == DiagnosticSeverity.Error ? 2 : 3;
                bool up = true;
                for (double x = start.X + step; x <= start.X + width; x += step)
                {
                    context.LineTo(new Point(x, up ? y - step : y), true, false);
                    up = !up;
                }
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private static Pen Pen(Brush brush, DiagnosticSeverity severity)
    {
        var pen = new Pen(brush, 1);
        if (severity == DiagnosticSeverity.Info)
        {
            pen.DashStyle = DashStyles.Dot;
        }

        if (pen.CanFreeze)
        {
            pen.Freeze(); // DEF-013: a theme brush (high contrast, during a theme switch) can't always be frozen
        }

        return pen;
    }
}

/// <summary>FR-053: the execution line background (<c>Editor.ExecutionLine</c>) behind the line about to execute.</summary>
public sealed class ExecutionLineRenderer(CodeEditor editor) : IBackgroundRenderer
{
    /// <inheritdoc />
    public KnownLayer Layer => KnownLayer.Background;

    /// <inheritdoc />
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(drawingContext);
        if (editor.ExecutionLine is not int line || line < 1 || line > editor.Document.LineCount || !textView.VisualLinesValid)
        {
            return;
        }

        var brush = editor.TryFindResource("Editor.ExecutionLine") as Brush;
        DocumentLine documentLine = editor.Document.GetLineByNumber(line);
        foreach (Rect rect in BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, textView.GetOrConstructVisualLine(documentLine), 0, 1000))
        {
            drawingContext.DrawRectangle(brush, null, new Rect(0, rect.Top, textView.ActualWidth, rect.Height));
        }
    }
}
