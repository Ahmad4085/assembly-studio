using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AssemblyStudio.App.Editor.Adornments;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;

namespace AssemblyStudio.App.Editor;

/// <summary>
/// The code editor (UI-003, ADR-0006, TASK-064): AvalonEdit with NASM highlighting (FR-011), line numbers and the current
/// line highlighted (FR-012), the standard editing commands (FR-013), block indent/unindent with Tab/Shift+Tab, auto-indent
/// to the previous line, and the tab width and tabs/spaces from Settings (FR-014). Each document has its own instance, so
/// caret, selection and undo history stay per tab (AC-006).
/// <para>
/// <see cref="BoundText"/> makes the text bindable (AvalonEdit's <c>Text</c> isn't a dependency property).
/// <see cref="TabMovesFocus"/> is the Ctrl+M mode: Tab and Shift+Tab move focus instead of indenting, so keyboard users can
/// leave the editor (no focus trap, ui-spec §8).
/// </para>
/// </summary>
public class CodeEditor : TextEditor
{
    /// <summary>The document text, two-way bindable.</summary>
    public static readonly DependencyProperty BoundTextProperty = DependencyProperty.Register(
        nameof(BoundText), typeof(string), typeof(CodeEditor),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundTextChanged));

    /// <summary>Columns per indentation level (Settings, FR-014).</summary>
    public static readonly DependencyProperty TabWidthProperty = DependencyProperty.Register(
        nameof(TabWidth), typeof(int), typeof(CodeEditor), new PropertyMetadata(4, (d, e) => ((CodeEditor)d).Options.IndentationSize = (int)e.NewValue));

    /// <summary>Indent with spaces (Settings, FR-014).</summary>
    public static readonly DependencyProperty UseSpacesProperty = DependencyProperty.Register(
        nameof(UseSpaces), typeof(bool), typeof(CodeEditor), new PropertyMetadata(true, (d, e) => ((CodeEditor)d).Options.ConvertTabsToSpaces = (bool)e.NewValue));

    /// <summary>Whether Tab moves focus instead of indenting (Ctrl+M).</summary>
    public static readonly DependencyProperty TabMovesFocusProperty = DependencyProperty.Register(
        nameof(TabMovesFocus), typeof(bool), typeof(CodeEditor), new PropertyMetadata(false));

    /// <summary>The build diagnostics of this document (FR-018); those with a line are drawn as squiggles and glyphs.</summary>
    public static readonly DependencyProperty DiagnosticsProperty = DependencyProperty.Register(
        nameof(Diagnostics), typeof(IReadOnlyList<Diagnostic>), typeof(CodeEditor), new PropertyMetadata(null, (d, _) => ((CodeEditor)d).OnDiagnosticsChanged()));

    /// <summary>The breakpoints of this document (FR-052).</summary>
    public static readonly DependencyProperty BreakpointsProperty = DependencyProperty.Register(
        nameof(Breakpoints), typeof(IReadOnlyList<EditorBreakpoint>), typeof(CodeEditor), new PropertyMetadata(null, (d, _) => ((CodeEditor)d).Refresh()));

    /// <summary>The 1-based line about to execute (FR-053), or <see langword="null"/>.</summary>
    public static readonly DependencyProperty ExecutionLineProperty = DependencyProperty.Register(
        nameof(ExecutionLine), typeof(int?), typeof(CodeEditor), new PropertyMetadata(null, (d, _) => ((CodeEditor)d).OnExecutionLineChanged()));

    /// <summary>What the execution marker shows.</summary>
    public static readonly DependencyProperty ExecutionMarkerProperty = DependencyProperty.Register(
        nameof(ExecutionMarker), typeof(ExecutionMarkerKind), typeof(CodeEditor), new PropertyMetadata(ExecutionMarkerKind.Current, (d, _) => ((CodeEditor)d).Refresh()));

    /// <summary>Toggles a breakpoint; the parameter is the 1-based line (gutter click and F9).</summary>
    public static readonly DependencyProperty ToggleBreakpointCommandProperty = DependencyProperty.Register(
        nameof(ToggleBreakpointCommand), typeof(ICommand), typeof(CodeEditor), new PropertyMetadata(null));

    /// <summary>The caret's 1-based line, for the status bar (UI-016).</summary>
    public static readonly DependencyProperty CaretLineProperty = DependencyProperty.Register(
        nameof(CaretLine), typeof(int), typeof(CodeEditor), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>The caret's 1-based column, for the status bar (UI-016).</summary>
    public static readonly DependencyProperty CaretColumnProperty = DependencyProperty.Register(
        nameof(CaretColumn), typeof(int), typeof(CodeEditor), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private readonly GlyphMargin _glyphMargin;
    private readonly ToolTip _textToolTip = new() { Placement = System.Windows.Controls.Primitives.PlacementMode.Relative };
    private bool _syncing;
    private bool _chordStarted;

    /// <summary>Creates an editor with the NASM highlighting and the editing behavior.</summary>
    public CodeEditor()
    {
        SyntaxHighlighting = NasmHighlighting.Definition;
        ShowLineNumbers = true;
        Options.HighlightCurrentLine = true;
        Options.IndentationSize = 4;
        Options.ConvertTabsToSpaces = true;
        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;
        TextArea.IndentationStrategy = new DefaultIndentationStrategy(); // a new line takes the previous line's indentation

        // Theme tokens (design tokens v1.0): selection and current line follow the theme like the syntax colors.
        TextArea.SetResourceReference(TextArea.SelectionBrushProperty, "Editor.Selection");
        TextArea.SelectionBorder = null;
        TextArea.SelectionForeground = null; // keep the syntax colors inside the selection
        TextArea.SelectionCornerRadius = 0;
        TextArea.TextView.SetResourceReference(TextView.CurrentLineBackgroundProperty, "Editor.CurrentLine");
        TextArea.TextView.CurrentLineBorder = null;
        TextArea.TextView.Margin = new Thickness(6, 0, 0, 0); // a gap between the line numbers and the code
        TextChanged += (_, _) =>
        {
            if (!_syncing)
            {
                _syncing = true;
                SetCurrentValue(BoundTextProperty, Text);
                _syncing = false;
            }
        };
        PreviewKeyDown += OnPreviewKeyDown;

        // TASK-066: the gutter (diagnostics, breakpoints, execution arrow), squiggles and the execution line.
        _glyphMargin = new GlyphMargin(this);
        TextArea.LeftMargins.Insert(0, _glyphMargin);
        TextArea.TextView.BackgroundRenderers.Add(new ExecutionLineRenderer(this));
        TextArea.TextView.BackgroundRenderers.Add(new DiagnosticRenderer(this));
        TextArea.TextView.MouseHover += OnTextMouseHover;
        TextArea.TextView.MouseHoverStopped += (_, _) => _textToolTip.IsOpen = false;
        TextArea.TextView.ToolTip = _textToolTip;
        ToolTipService.SetIsEnabled(TextArea.TextView, false); // opened by hand, only over lines with diagnostics
        // TASK-069: keyboard focus is on the inner TextArea, the element with the Text pattern that Narrator reads; give it
        // the document's name too, so Narrator announces "hello.asm, document" and not an unnamed document.
        TextArea.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty, new System.Windows.Data.Binding
        {
            Source = this,
            Path = new PropertyPath(System.Windows.Automation.AutomationProperties.NameProperty),
        });
        TextArea.SetValue(System.Windows.Automation.AutomationProperties.HelpTextProperty, Strings.EditorHelpText);
        TextArea.Caret.PositionChanged += (_, _) =>
        {
            SetCurrentValue(CaretLineProperty, TextArea.Caret.Line); // keeps the binding to the document (status bar)
            SetCurrentValue(CaretColumnProperty, TextArea.Caret.Column);
        };
    }

    /// <inheritdoc cref="CaretLineProperty"/>
    public int CaretLine
    {
        get => (int)GetValue(CaretLineProperty);
        set => SetValue(CaretLineProperty, value);
    }

    /// <inheritdoc cref="CaretColumnProperty"/>
    public int CaretColumn
    {
        get => (int)GetValue(CaretColumnProperty);
        set => SetValue(CaretColumnProperty, value);
    }

    /// <summary>The diagnostics with a line, by line.</summary>
    public IReadOnlyDictionary<int, IReadOnlyList<Diagnostic>> LineDiagnostics { get; private set; } = new Dictionary<int, IReadOnlyList<Diagnostic>>();

    /// <inheritdoc cref="DiagnosticsProperty"/>
    public IReadOnlyList<Diagnostic>? Diagnostics
    {
        get => (IReadOnlyList<Diagnostic>?)GetValue(DiagnosticsProperty);
        set => SetValue(DiagnosticsProperty, value);
    }

    /// <inheritdoc cref="BreakpointsProperty"/>
    public IReadOnlyList<EditorBreakpoint>? Breakpoints
    {
        get => (IReadOnlyList<EditorBreakpoint>?)GetValue(BreakpointsProperty);
        set => SetValue(BreakpointsProperty, value);
    }

    /// <inheritdoc cref="ExecutionLineProperty"/>
    public int? ExecutionLine
    {
        get => (int?)GetValue(ExecutionLineProperty);
        set => SetValue(ExecutionLineProperty, value);
    }

    /// <inheritdoc cref="ExecutionMarkerProperty"/>
    public ExecutionMarkerKind ExecutionMarker
    {
        get => (ExecutionMarkerKind)GetValue(ExecutionMarkerProperty);
        set => SetValue(ExecutionMarkerProperty, value);
    }

    /// <inheritdoc cref="ToggleBreakpointCommandProperty"/>
    public ICommand? ToggleBreakpointCommand
    {
        get => (ICommand?)GetValue(ToggleBreakpointCommandProperty);
        set => SetValue(ToggleBreakpointCommandProperty, value);
    }

    /// <summary>The gutter, for tests and accessibility checks.</summary>
    internal GlyphMargin GlyphMargin => _glyphMargin;

    /// <summary>"⨯ Error ASM001: message" and the next step on a second line.</summary>
    public static string Describe(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        (string glyph, string severity) = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => ("⨯", Strings.SeverityError),
            DiagnosticSeverity.Warning => ("▲", Strings.SeverityWarning),
            _ => ("ⓘ", Strings.SeverityInfo),
        };
        string text = $"{glyph} {severity} {diagnostic.Code}: {diagnostic.What}";
        return string.IsNullOrWhiteSpace(diagnostic.NextStep) ? text : text + Environment.NewLine + diagnostic.NextStep;
    }

    /// <summary>The text shown for <paramref name="line"/>'s diagnostics, or <see langword="null"/> when it has none.</summary>
    public string? DiagnosticText(int line) =>
        LineDiagnostics.TryGetValue(line, out IReadOnlyList<Diagnostic>? diagnostics)
            ? string.Join(Environment.NewLine, diagnostics.Select(Describe))
            : null;

    /// <summary>Asks the bound command to toggle a breakpoint on <paramref name="line"/> (gutter click, F9).</summary>
    public void RequestToggleBreakpoint(int line)
    {
        if (ToggleBreakpointCommand is { } command && command.CanExecute(line))
        {
            command.Execute(line);
        }
    }

    /// <summary>Ctrl+K, Ctrl+I: shows the caret line's diagnostics (ui-spec §5). Returns whether there were any.</summary>
    public bool ShowCaretDiagnostics()
    {
        string? text = DiagnosticText(TextArea.Caret.Line);
        if (text is null)
        {
            return false;
        }

        Point caret = TextArea.TextView.GetVisualPosition(TextArea.Caret.Position, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom) - TextArea.TextView.ScrollOffset;
        ShowToolTip(text, caret);
        return true;
    }

    private void ShowToolTip(string text, Point at)
    {
        _textToolTip.Content = text;
        _textToolTip.PlacementTarget = TextArea.TextView;
        _textToolTip.HorizontalOffset = at.X;
        _textToolTip.VerticalOffset = at.Y + 2;
        _textToolTip.IsOpen = true;
    }

    private void OnTextMouseHover(object sender, MouseEventArgs e)
    {
        if (TextArea.TextView.GetPositionFloor(e.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset) is { } position
            && DiagnosticText(position.Line) is { } text)
        {
            ShowToolTip(text, e.GetPosition(TextArea.TextView));
            e.Handled = true;
        }
    }

    private void OnDiagnosticsChanged()
    {
        LineDiagnostics = (Diagnostics ?? [])
            .Where(d => d.Line is > 0)
            .GroupBy(d => d.Line!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Diagnostic>)[.. g]);
        Refresh();
    }

    private void OnExecutionLineChanged()
    {
        if (ExecutionLine is int line && line >= 1 && line <= Document.LineCount)
        {
            ScrollToLine(line); // keep the execution line in view while stepping
        }

        Refresh();
    }

    private void Refresh()
    {
        _glyphMargin?.InvalidateVisual();
        TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Background);
        TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
    }

    /// <inheritdoc cref="BoundTextProperty"/>
    public string BoundText
    {
        get => (string)GetValue(BoundTextProperty);
        set => SetValue(BoundTextProperty, value);
    }

    /// <inheritdoc cref="TabWidthProperty"/>
    public int TabWidth
    {
        get => (int)GetValue(TabWidthProperty);
        set => SetValue(TabWidthProperty, value);
    }

    /// <inheritdoc cref="UseSpacesProperty"/>
    public bool UseSpaces
    {
        get => (bool)GetValue(UseSpacesProperty);
        set => SetValue(UseSpacesProperty, value);
    }

    /// <inheritdoc cref="TabMovesFocusProperty"/>
    public bool TabMovesFocus
    {
        get => (bool)GetValue(TabMovesFocusProperty);
        set => SetValue(TabMovesFocusProperty, value);
    }

    /// <summary>Redraws the text, for example after the theme changed the syntax brushes.</summary>
    public void Redraw() => TextArea.TextView.Redraw();

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ForegroundProperty)
        {
            TextArea.Caret.CaretBrush = Foreground;
            Redraw(); // the theme changed: the Syntax.* brushes are looked up again
        }
    }

    private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (CodeEditor)d;
        string text = (string?)e.NewValue ?? string.Empty;
        if (!editor._syncing && editor.Text != text)
        {
            editor._syncing = true;
            editor.Text = text;
            editor._syncing = false;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F9 && Keyboard.Modifiers == ModifierKeys.None)
        {
            RequestToggleBreakpoint(TextArea.Caret.Line); // FR-052: F9 toggles a breakpoint on the caret line
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.K or Key.I)
        {
            if (e.Key == Key.K)
            {
                _chordStarted = true;
                e.Handled = true;
                return;
            }

            if (_chordStarted)
            {
                _chordStarted = false;
                ShowCaretDiagnostics(); // Ctrl+K, Ctrl+I
                e.Handled = true;
                return;
            }
        }

        if (e.Key is not (Key.LeftCtrl or Key.RightCtrl))
        {
            _chordStarted = false;
        }

        if (TabMovesFocus && e.Key == Key.Tab && (Keyboard.Modifiers & ~ModifierKeys.Shift) == ModifierKeys.None)
        {
            var direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
            ((UIElement)(Keyboard.FocusedElement ?? this)).MoveFocus(new TraversalRequest(direction));
            e.Handled = true;
        }
    }
}
