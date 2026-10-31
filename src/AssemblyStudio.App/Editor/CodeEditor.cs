using System.Windows;
using System.Windows.Input;
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

    private bool _syncing;

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
        if (TabMovesFocus && e.Key == Key.Tab && (Keyboard.Modifiers & ~ModifierKeys.Shift) == ModifierKeys.None)
        {
            var direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
            ((UIElement)(Keyboard.FocusedElement ?? this)).MoveFocus(new TraversalRequest(direction));
            e.Handled = true;
        }
    }
}
