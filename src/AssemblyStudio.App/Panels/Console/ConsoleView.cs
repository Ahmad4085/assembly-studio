using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace AssemblyStudio.App.Panels.Console;

/// <summary>
/// The console panel's view (UI-010): a read-only text view of <see cref="ConsoleViewModel.Screen"/> that updates only the
/// lines an output changed (a flood stays cheap, AC-045), follows the end of the output, flashes its border on BEL, and
/// passes typed keys to the program while it runs or waits (FR-064). Ctrl+C there is the program's Ctrl-C while it is
/// active and Copy otherwise; Ctrl+Shift+C always copies (D-015).
/// </summary>
public sealed class ConsoleView : TextEditor
{
    /// <summary>The console to show.</summary>
    public static readonly DependencyProperty ConsoleProperty = DependencyProperty.Register(
        nameof(Console), typeof(ConsoleViewModel), typeof(ConsoleView), new PropertyMetadata(null, (d, e) => ((ConsoleView)d).Attach((ConsoleViewModel?)e.OldValue, (ConsoleViewModel?)e.NewValue)));

    private readonly DispatcherTimer _bellTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

    /// <summary>Creates the view.</summary>
    public ConsoleView()
    {
        IsReadOnly = true;
        ShowLineNumbers = false;
        WordWrap = false;
        HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
        VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;
        BorderThickness = new Thickness(2);
        BorderBrush = Brushes.Transparent;
        TextArea.TextView.Margin = new Thickness(6, 2, 0, 2);
        SetResourceReference(BackgroundProperty, "Surface.Editor");
        SetResourceReference(ForegroundProperty, "Text.Primary");
        TextArea.SetResourceReference(ICSharpCode.AvalonEdit.Editing.TextArea.SelectionBrushProperty, "Editor.Selection");
        _bellTimer.Tick += (_, _) =>
        {
            _bellTimer.Stop();
            BorderBrush = Brushes.Transparent;
        };
        CommandBindings.Add(new CommandBinding(CopyAll, (_, _) => CopySelectionOrAll()));
        InputBindings.Add(new KeyBinding(CopyAll, Key.C, ModifierKeys.Control | ModifierKeys.Shift));
    }

    /// <summary>Ctrl+Shift+C: copies the selection, or the whole console when nothing is selected.</summary>
    public static RoutedUICommand CopyAll { get; } = new(Shell.Strings.ConsoleCopy.Replace("_", string.Empty, StringComparison.Ordinal), nameof(CopyAll), typeof(ConsoleView));

    /// <inheritdoc cref="ConsoleProperty"/>
    public ConsoleViewModel? Console
    {
        get => (ConsoleViewModel?)GetValue(ConsoleProperty);
        set => SetValue(ConsoleProperty, value);
    }

    /// <summary>Copies the selection, or everything when nothing is selected.</summary>
    public void CopySelectionOrAll()
    {
        string text = SelectionLength > 0 ? SelectedText : Text;
        if (text.Length > 0)
        {
            Clipboard.SetText(text);
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPreviewKeyDown(e);
        if (!e.Handled && HandleKey(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPreviewTextInput(e);
        if (!e.Handled && Console is { } console && console.SendInput(ConsoleKeys.FromText(e.Text)))
        {
            e.Handled = true;
        }
    }

    /// <summary>The console's handling of one key press (D-015, FR-064); returns whether it took the key.</summary>
    internal bool HandleKey(Key key, ModifierKeys modifiers)
    {
        if (Console is not { } console)
        {
            return false;
        }

        if (key == Key.C && modifiers == ModifierKeys.Control)
        {
            return console.SendCtrlC(); // otherwise the editor's Copy handles it
        }

        return ConsoleKeys.FromKey(key, modifiers) is { } bytes && console.SendInput(bytes);
    }

    private void Attach(ConsoleViewModel? old, ConsoleViewModel? console)
    {
        if (old is not null)
        {
            old.Changed -= OnChanged;
            old.Cleared -= OnCleared;
        }

        Document.Text = console?.Text ?? string.Empty;
        if (console is not null)
        {
            console.Changed += OnChanged;
            console.Cleared += OnCleared;
        }
    }

    private void OnCleared(object? sender, EventArgs e) => Document.Text = string.Empty;

    private void OnChanged(object? sender, ConsoleChange change)
    {
        ConsoleScreen screen = ((ConsoleViewModel)sender!).Screen;
        TextDocument document = Document;
        document.BeginUpdate();
        try
        {
            if (change.RemovedLines >= document.LineCount || change.FirstChangedLine == 0)
            {
                document.Text = screen.Text;
            }
            else
            {
                if (change.RemovedLines > 0)
                {
                    document.Remove(0, document.GetLineByNumber(change.RemovedLines + 1).Offset);
                }

                int start = change.FirstChangedLine < document.LineCount ? document.GetLineByNumber(change.FirstChangedLine + 1).Offset : document.TextLength;
                document.Replace(start, document.TextLength - start, screen.TextFrom(change.FirstChangedLine));
            }
        }
        finally
        {
            document.EndUpdate();
        }

        if (SelectionLength == 0)
        {
            CaretOffset = document.TextLength;
        }

        ScrollToEnd();
        if (change.Bells > 0)
        {
            SetResourceReference(BorderBrushProperty, "Status.Warning"); // visual BEL
            _bellTimer.Stop();
            _bellTimer.Start();
        }
    }
}
