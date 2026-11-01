using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor;
using AssemblyStudio.App.Editor.Adornments;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.App.Tests.Editor;

/// <summary>
/// TASK-066: diagnostics in the editor (AC-013 UI part, FR-018), the breakpoint margin (FR-052), the execution line and
/// arrow (FR-053), the stale-session banner, and the non-color cues of the glyphs (design tokens §3).
/// </summary>
public sealed class AdornmentTests
{
    private const string Program =
        "org 100h\n" +
        "start:\n" +
        "    mov ax, bogus     ; line 3: warning\n" +
        "    mov dx, msg\n" +
        "    mov ah, 09h\n" +
        "    int 21h\n" +
        "    movx ax, 1        ; line 7: error\n" +
        "    mov ax, 4C00h\n" +
        "    int 21h\n" +
        "msg db 'Hi$'\n";

    private static readonly Diagnostic Warning = new("ASM002", DiagnosticSeverity.Warning, "label alone on a line", null, 3, "Check this line.");
    private static readonly Diagnostic Error = new("ASM001", DiagnosticSeverity.Error, "parser: instruction expected", null, 7, "Fix the line.");
    private static readonly Diagnostic Build = new("BLD004", DiagnosticSeverity.Error, "the build failed", null, null, "See the output.");

    private static void WithEditor(Action<CodeEditor, Window> body)
    {
        StaTestThread.Run(() =>
        {
            var editor = new CodeEditor { BoundText = Program };
            var window = new Window { Content = editor, Width = 700, Height = 300 };
            try
            {
                window.Show();
                window.UpdateLayout();
                body(editor, window);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>The geometry drawings a background renderer produces for the visible lines.</summary>
    private static List<GeometryDrawing> Drawings(IBackgroundRendererLike renderer, CodeEditor editor)
    {
        var visual = new DrawingVisual();
        using (DrawingContext context = visual.RenderOpen())
        {
            renderer.Draw(editor.TextArea.TextView, context);
        }

        var found = new List<GeometryDrawing>();
        void Walk(Drawing? drawing)
        {
            if (drawing is DrawingGroup group)
            {
                foreach (Drawing child in group.Children)
                {
                    Walk(child);
                }
            }
            else if (drawing is GeometryDrawing geometry)
            {
                found.Add(geometry);
            }
        }

        Walk(VisualTreeHelper.GetDrawing(visual));
        return found;
    }

    private interface IBackgroundRendererLike
    {
        void Draw(ICSharpCode.AvalonEdit.Rendering.TextView view, DrawingContext context);
    }

    private sealed class Adapter(ICSharpCode.AvalonEdit.Rendering.IBackgroundRenderer inner) : IBackgroundRendererLike
    {
        public void Draw(ICSharpCode.AvalonEdit.Rendering.TextView view, DrawingContext context) => inner.Draw(view, context);
    }

    [Fact]
    public void AC_013_an_error_and_a_warning_are_marked_on_their_lines_with_squiggles_glyphs_and_text()
    {
        WithEditor((editor, _) =>
        {
            editor.Diagnostics = [Warning, Error, Build];
            editor.TextArea.TextView.EnsureVisualLines();

            Assert.Equal([3, 7], editor.LineDiagnostics.Keys.Order()); // the build-level message has no line
            List<GeometryDrawing> squiggles = Drawings(new Adapter(new DiagnosticRenderer(editor)), editor);
            Assert.Equal(2, squiggles.Count);
            Assert.Contains(squiggles, d => d.Pen.Brush == Application.Current.Resources["Status.Error"]);
            Assert.Contains(squiggles, d => d.Pen.Brush == Application.Current.Resources["Status.Warning"]);

            Assert.Equal("⨯ Error ASM001: parser: instruction expected" + Environment.NewLine + "Fix the line.", editor.DiagnosticText(7));
            Assert.StartsWith("▲ Warning ASM002", editor.GlyphMargin.Describe(3), StringComparison.Ordinal);
            Assert.Null(editor.DiagnosticText(5));

            editor.Diagnostics = [];
            Assert.Empty(editor.LineDiagnostics);
            Assert.Empty(Drawings(new Adapter(new DiagnosticRenderer(editor)), editor));
        });
    }

    /// <summary>The keyboard route to a line's message (ui-spec §5: Ctrl+K, Ctrl+I calls <see cref="CodeEditor.ShowCaretDiagnostics"/>).
    /// The chord itself reads the real keyboard's modifier state, so it is checked by hand, not here.</summary>
    [Fact]
    public void The_caret_lines_diagnostics_can_be_shown_without_the_mouse()
    {
        WithEditor((editor, _) =>
        {
            editor.Diagnostics = [Error];
            editor.TextArea.Focus();
            editor.CaretOffset = editor.Document.GetLineByNumber(7).Offset;

            Assert.True(editor.ShowCaretDiagnostics());

            var tip = (System.Windows.Controls.ToolTip)editor.TextArea.TextView.ToolTip;
            Assert.True(tip.IsOpen);
            Assert.Contains("ASM001", (string)tip.Content, StringComparison.Ordinal);
            tip.IsOpen = false;

            editor.CaretOffset = 0;
            Assert.False(editor.ShowCaretDiagnostics()); // nothing on line 1
        });
    }

    [Fact]
    public void FR_052_F9_and_the_gutter_toggle_breakpoints_through_the_document()
    {
        StaTestThread.Run(() =>
        {
            var document = new DocumentViewModel(Program, null, DocumentEncoding.Utf8, LineEnding.CrLf);
            var editor = new CodeEditor { BoundText = Program, ToggleBreakpointCommand = document.ToggleBreakpointCommand };
            var window = new Window { Content = editor, Width = 700, Height = 300 };
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.TextArea.Focus();
                editor.CaretOffset = editor.Document.GetLineByNumber(4).Offset;

                Press(editor, Key.F9, ModifierKeys.None);
                Assert.Equal([new EditorBreakpoint(4)], document.Breakpoints);

                editor.RequestToggleBreakpoint(6); // a click in the gutter on line 6
                Assert.Equal([4, 6], document.Breakpoints.Select(b => b.Line));
                Press(editor, Key.F9, ModifierKeys.None);
                Assert.Equal([6], document.Breakpoints.Select(b => b.Line));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Breakpoints_show_their_state_in_the_gutter_text()
    {
        WithEditor((editor, _) =>
        {
            editor.Breakpoints = [new EditorBreakpoint(4, BreakpointBinding.Bound, "0800:0103"), new EditorBreakpoint(2, BreakpointBinding.Unbound), new EditorBreakpoint(5)];

            Assert.Equal("Breakpoint at 0800:0103", editor.GlyphMargin.Describe(4));
            Assert.Equal(Strings.BreakpointUnbound, editor.GlyphMargin.Describe(2));
            Assert.Equal(Strings.BreakpointNotBuilt, editor.GlyphMargin.Describe(5));
            Assert.Null(editor.GlyphMargin.Describe(1));
        });
    }

    [Fact]
    public void FR_053_the_execution_line_is_highlighted_and_scrolled_into_view()
    {
        StaTestThread.Run(() =>
        {
            string longProgram = string.Concat(Enumerable.Repeat("    nop\n", 200));
            var editor = new CodeEditor { BoundText = longProgram };
            var window = new Window { Content = editor, Width = 500, Height = 200 };
            try
            {
                window.Show();
                window.UpdateLayout();

                editor.ExecutionLine = 150;
                window.UpdateLayout();
                editor.TextArea.TextView.EnsureVisualLines();

                Assert.Contains(editor.TextArea.TextView.VisualLines, v => v.FirstDocumentLine.LineNumber == 150);
                List<GeometryDrawing> fills = Drawings(new Adapter(new ExecutionLineRenderer(editor)), editor);
                Assert.Single(fills);
                Assert.Same(Application.Current.Resources["Editor.ExecutionLine"], fills[0].Brush);

                editor.ExecutionLine = null;
                Assert.Empty(Drawings(new Adapter(new ExecutionLineRenderer(editor)), editor));
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>DEF-013: switching the theme while squiggles are shown must not fail while drawing (a theme brush can't
    /// always be frozen).</summary>
    [Fact]
    public void DEF_013_switching_the_theme_with_squiggles_shown_redraws_them_in_every_theme()
    {
        WithEditor((editor, window) =>
        {
            var manager = new ThemeManager(Application.Current.Resources);
            try
            {
                editor.Diagnostics = [Warning, Error, new Diagnostic("ASM003", DiagnosticSeverity.Info, "note", null, 5, string.Empty)];
                foreach (AppTheme theme in new[] { AppTheme.Light, AppTheme.HighContrast, AppTheme.Dark })
                {
                    manager.Apply(theme);
                    window.UpdateLayout();
                    editor.TextArea.TextView.EnsureVisualLines();
                    Assert.Equal(3, Drawings(new Adapter(new DiagnosticRenderer(editor)), editor).Count);
                }
            }
            finally
            {
                manager.Apply(AppTheme.Dark); // the application resources are shared by every WPF test
            }
        });
    }

    [Fact]
    public void Glyphs_and_squiggles_differ_by_shape_not_only_by_color()
    {
        Geometry[] glyphs = [GlyphShapes.Breakpoint, GlyphShapes.Arrow, GlyphShapes.Error, GlyphShapes.Warning, GlyphShapes.Info, GlyphShapes.Ended];
        double[] areas = [.. glyphs.Select(g => Math.Round(g.GetArea(), 3))];
        Assert.Equal(glyphs.Length, areas.Distinct().Count());                                      // six different shapes
        Assert.Equal([GlyphShapes.Error, GlyphShapes.Warning, GlyphShapes.Info],
            new[] { DiagnosticSeverity.Error, DiagnosticSeverity.Warning, DiagnosticSeverity.Info }.Select(GlyphShapes.For));

        Point start = new(0, 10);
        string error = DiagnosticRenderer.Underline(start, 30, DiagnosticSeverity.Error).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string warning = DiagnosticRenderer.Underline(start, 30, DiagnosticSeverity.Warning).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string info = DiagnosticRenderer.Underline(start, 30, DiagnosticSeverity.Info).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(3, new[] { error, warning, info }.Distinct().Count()); // wavy (short), wavy (long), straight dotted
    }

    [Fact]
    public void Editing_while_a_session_runs_shows_the_stale_banner_until_the_session_ends()
    {
        StaTestThread.Run(() =>
        {
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            var window = new MainWindow(new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace), persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = Program;
                window.Show();
                window.UpdateLayout();
                var banner = (FrameworkElement)FindNamed(window, "StaleSessionBanner")!;
                Assert.False(banner.IsVisible);

                document.HasSession = true;
                document.Text += "nop\n";
                window.UpdateLayout();
                Assert.True(document.IsSessionStale);
                Assert.True(banner.IsVisible);

                document.HasSession = false;
                window.UpdateLayout();
                Assert.False(document.IsSessionStale);
                Assert.False(banner.IsVisible);
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    [Fact]
    public void Renders_diagnostics_breakpoints_and_the_execution_arrow_for_review()
    {
        StaTestThread.Run(() =>
        {
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            var window = new MainWindow(new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace), persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = Program;
                document.Diagnostics = [Warning, Error];
                document.Breakpoints = [new EditorBreakpoint(4, BreakpointBinding.Bound, "0800:0103"), new EditorBreakpoint(2, BreakpointBinding.Unbound)];
                document.ExecutionLine = 5;
                window.Show();
                window.UpdateLayout();
                CodeEditor editor = window.ActiveEditor()!;
                Assert.Equal(2, editor.LineDiagnostics.Count); // bound from the document
                Assert.Equal(5, editor.ExecutionLine);
                Render(window, "editor-adornments.png");
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    private static void Press(CodeEditor editor, Key key, ModifierKeys modifiers)
    {
        Assert.Equal(ModifierKeys.None, modifiers); // Keyboard.Modifiers reads the real keyboard: only unmodified keys can be simulated
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(editor)!, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        editor.TextArea.RaiseEvent(args);
        Assert.True(args.Handled);
    }

    private static DependencyObject? FindNamed(DependencyObject parent, string name)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement { Name: var n } && n == name)
            {
                return child;
            }

            if (FindNamed(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static void Render(Window window, string fileName)
    {
        var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string dir = Path.Combine(MainWindowSmokeTests.RepoRoot(), "artifacts", "ui");
        Directory.CreateDirectory(dir);
        using FileStream file = File.Create(Path.Combine(dir, fileName));
        encoder.Save(file);
    }
}
