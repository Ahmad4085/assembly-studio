using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Xunit;

namespace AssemblyStudio.App.Tests.Editor;

/// <summary>
/// TASK-064: the AvalonEdit code editor: NASM highlighting and line numbers (AC-009, FR-011, FR-012), editing commands,
/// block indent and auto-indent (AC-010, FR-013, FR-014), the Ctrl+M Tab mode, and zoom (AC-046, FR-022). The NFR-001
/// latency measurement is in <see cref="EditorLatencyTests"/>.
/// </summary>
public sealed class EditorTests
{
    private const string AllTokens =
        "%include \"io.inc\"\n" +
        "org 100h\n" +
        "start:\n" +
        "    mov ax, 0x10      ; comment\n" +
        "    mov bl, 'A'\n" +
        "    add al, 1010b\n" +
        "    int 21h\n" +
        "    jmp start\n" +
        "message db 'Hello', 13, 10, '$'\n";

    /// <summary>The highlighting color of the first occurrence of <paramref name="token"/> on <paramref name="line"/>.</summary>
    private static string? ColorAt(TextDocument document, int line, string token, int occurrence = 0)
    {
        var highlighter = new DocumentHighlighter(document, NasmHighlighting.Definition);
        HighlightedLine highlighted = highlighter.HighlightLine(line);
        string text = document.GetText(document.GetLineByNumber(line));
        int column = -1;
        for (int i = 0; i <= occurrence; i++)
        {
            column = text.IndexOf(token, column + 1, StringComparison.Ordinal);
        }

        Assert.True(column >= 0, $"'{token}' not on line {line}");
        int offset = document.GetLineByNumber(line).Offset + column;
        return highlighted.Sections.Where(s => s.Offset <= offset && offset < s.Offset + s.Length).Select(s => s.Color.Name).LastOrDefault();
    }

    [Fact]
    public void AC_009_every_token_category_gets_its_style()
    {
        var document = new TextDocument(AllTokens);

        Assert.Equal("Directive", ColorAt(document, 1, "%include"));
        Assert.Equal("String", ColorAt(document, 1, "\"io.inc\""));
        Assert.Equal("Directive", ColorAt(document, 2, "org"));
        Assert.Equal("Number", ColorAt(document, 2, "100h"));
        Assert.Equal("Label", ColorAt(document, 3, "start"));
        Assert.Equal("Mnemonic", ColorAt(document, 4, "mov"));
        Assert.Equal("Register", ColorAt(document, 4, "ax"));
        Assert.Equal("Number", ColorAt(document, 4, "0x10"));
        Assert.Equal("Comment", ColorAt(document, 4, "; comment"));
        Assert.Equal("Number", ColorAt(document, 5, "'A'"));       // a character literal is a number in NASM
        Assert.Equal("Number", ColorAt(document, 6, "1010b"));
        Assert.Equal("Mnemonic", ColorAt(document, 7, "int"));
        Assert.Equal("Number", ColorAt(document, 7, "21h"));
        Assert.Equal("Label", ColorAt(document, 8, "start"));      // a label reference after a jump
        Assert.Equal("Label", ColorAt(document, 9, "message"));    // a data label without a colon
        Assert.Equal("Directive", ColorAt(document, 9, "db"));
        Assert.Equal("String", ColorAt(document, 9, "'Hello'"));
        Assert.Equal("Number", ColorAt(document, 9, "13"));
    }

    [Fact]
    public void A_character_literal_doesnt_start_a_string_and_the_rest_of_the_line_keeps_its_colors()
    {
        var document = new TextDocument("cmp al, 'a' ; lower case\nmov dl, \"x\"\n");

        Assert.Equal("Number", ColorAt(document, 1, "'a'"));
        Assert.Equal("Comment", ColorAt(document, 1, "; lower case"));
        Assert.Equal("Number", ColorAt(document, 2, "\"x\""));
    }

    [Fact]
    public void Keywords_are_case_insensitive_and_text_in_comments_and_strings_is_not_code()
    {
        var document = new TextDocument("MOV AX, BX ; mov ax\ndb 'mov ax'\n");

        Assert.Equal("Mnemonic", ColorAt(document, 1, "MOV"));
        Assert.Equal("Register", ColorAt(document, 1, "AX"));
        Assert.Equal("Comment", ColorAt(document, 1, "mov"));
        Assert.Equal("String", ColorAt(document, 2, "mov ax"));
    }

    [Fact]
    public void AC_009_line_numbers_and_the_current_line_are_shown_and_colors_come_from_the_theme()
    {
        StaTestThread.Run(() =>
        {
            var editor = new CodeEditor { BoundText = AllTokens };
            var window = new Window { Content = editor, Width = 500, Height = 300 };
            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.True(editor.ShowLineNumbers);
                Assert.True(editor.Options.HighlightCurrentLine);
                Assert.Contains(editor.TextArea.LeftMargins, m => m is ICSharpCode.AvalonEdit.Editing.LineNumberMargin);
                foreach (string category in NasmHighlighting.Categories)
                {
                    HighlightingColor color = NasmHighlighting.Definition.GetNamedColor(category);
                    Assert.Same(Application.Current.Resources["Syntax." + category], color.Foreground.GetBrush(null));
                }
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AC_010_undo_redo_cut_copy_paste_and_select_all_work_through_the_standard_commands()
    {
        WithEditor("mov ax, 1\n", editor =>
        {
            ICSharpCode.AvalonEdit.Editing.TextArea area = editor.TextArea;
            editor.CaretOffset = editor.Text.Length;
            area.PerformTextInput("int 21h");
            ApplicationCommands.Undo.Execute(null, area);
            Assert.Equal("mov ax, 1\n", editor.Text);
            ApplicationCommands.Redo.Execute(null, area);
            Assert.Equal("mov ax, 1\nint 21h", editor.Text);

            ApplicationCommands.SelectAll.Execute(null, area);
            Assert.Equal(editor.Text.Length, editor.SelectionLength);

            // The Windows clipboard is shared with every other program on the machine; when one holds it, AvalonEdit's cut and
            // copy do nothing and Clipboard.GetText throws. Retry the clipboard part a few times before failing (2026-10-31).
            WithClipboardRetry(() =>
            {
                editor.Text = "mov ax, 1\nint 21h";
                editor.Select(0, 3);
                ApplicationCommands.Cut.Execute(null, area);
                Assert.Equal(" ax, 1\nint 21h", editor.Text);
                editor.CaretOffset = editor.Text.Length;
                ApplicationCommands.Paste.Execute(null, area);
                Assert.Equal(" ax, 1\nint 21hmov", editor.Text);
                editor.Select(0, 3);
                ApplicationCommands.Copy.Execute(null, area);
                Assert.Equal(" ax", Clipboard.GetText());
            });
        });
    }

    private static void WithClipboardRetry(Action body)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                body();
                return;
            }
            catch (Exception e) when (attempt < 5 && e is System.Runtime.InteropServices.COMException or Xunit.Sdk.EqualException)
            {
                Thread.Sleep(200);
            }
        }
    }

    [Fact]
    public void AC_010_Tab_and_Shift_Tab_indent_and_unindent_the_selected_lines_and_new_lines_keep_the_indentation()
    {
        WithEditor("mov ax, 1\nmov bx, 2\n", editor =>
        {
            editor.Select(0, 12); // across the first two lines
            System.Windows.Documents.EditingCommands.TabForward.Execute(null, editor.TextArea); // Tab
            Assert.Equal("    mov ax, 1\n    mov bx, 2\n", editor.Text);

            System.Windows.Documents.EditingCommands.TabBackward.Execute(null, editor.TextArea); // Shift+Tab
            Assert.Equal("mov ax, 1\nmov bx, 2\n", editor.Text);

            editor.Text = "    mov ax, 1";
            editor.CaretOffset = editor.Text.Length;
            editor.TextArea.PerformTextInput("\n");
            editor.TextArea.PerformTextInput("int 21h");
            Assert.Equal("    mov ax, 1\n    int 21h", editor.Text.ReplaceLineEndings("\n")); // auto-indent
        });
    }

    [Fact]
    public void FR_014_tab_width_and_tabs_or_spaces_come_from_the_settings()
    {
        WithEditor("x", editor =>
        {
            editor.TabWidth = 8;
            editor.UseSpaces = false;
            Assert.Equal((8, false), (editor.Options.IndentationSize, editor.Options.ConvertTabsToSpaces));
            editor.CaretOffset = 0;
            editor.TextArea.PerformTextInput("\t");
            Assert.Equal("\tx", editor.Text);

            editor.UseSpaces = true;
            editor.TabWidth = 2;
            editor.Text = "x\ny";
            editor.SelectAll();
            System.Windows.Documents.EditingCommands.TabForward.Execute(null, editor.TextArea); // Tab on a multi-line selection
            Assert.Equal("  x\n  y", editor.Text);
        });
    }

    [Fact]
    public void Ctrl_M_makes_Tab_move_focus_instead_of_indenting()
    {
        StaTestThread.Run(() =>
        {
            var editor = new CodeEditor { BoundText = "mov ax, 1" };
            var after = new Button { Content = "next" };
            var window = new Window { Content = new StackPanel { Children = { editor, after } }, Width = 400, Height = 200 };
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.TextArea.Focus();
                editor.TabMovesFocus = true;

                var tab = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(editor)!, 0, Key.Tab) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                editor.TextArea.RaiseEvent(tab);

                Assert.True(tab.Handled);
                Assert.Equal("mov ax, 1", editor.Text); // no tab inserted
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AC_046_zoom_steps_between_50_and_300_percent_and_resets_to_100()
    {
        var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test",
            new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore()));
        Assert.Equal(100, shell.ZoomPercent);
        Assert.False(shell.ZoomResetCommand.CanExecute(null));

        var seen = new List<int>();
        while (shell.ZoomInCommand.CanExecute(null))
        {
            shell.ZoomInCommand.Execute(null);
            seen.Add(shell.ZoomPercent);
        }

        Assert.Equal([110, 125, 150, 175, 200, 250, 300], seen);
        shell.ZoomResetCommand.Execute(null);
        while (shell.ZoomOutCommand.CanExecute(null))
        {
            shell.ZoomOutCommand.Execute(null);
        }

        Assert.Equal(50, shell.ZoomPercent);
        shell.ZoomResetCommand.Execute(null);
        Assert.Equal(100, shell.ZoomPercent);
    }

    [Fact]
    public void AC_046_zoom_is_saved_with_the_layout_and_an_invalid_value_falls_back_to_100()
    {
        var store = new ShellViewModelTests.MemoryLayoutStore(null);
        var shell = new ShellViewModel(store, "test", new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore()));
        shell.ZoomInCommand.Execute(null);
        shell.ZoomInCommand.Execute(null);

        LayoutState saved = shell.CaptureLayout(1440, 900, false);

        Assert.Equal(125, saved.ZoomPercent);
        Assert.Equal(125, new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(saved), "test").ZoomPercent);
        Assert.Equal(100, (LayoutState.Default with { ZoomPercent = 333 }).Sanitized().ZoomPercent);
    }

    [Fact]
    public void AC_046_zoom_scales_the_editor_and_code_fonts()
    {
        var resources = new ResourceDictionary();

        EditorAppearance.Apply(resources, AppSettings.Default, textScale: 1.25, zoom: 2.0);
        EditorAppearance.ApplyCodeZoom(resources, codeSize: 14 * 1.25, zoom: 2.0);

        Assert.Equal(14 * 1.25 * 2.0, resources["FontSize.Editor"]);
        Assert.Equal(14 * 1.25 * 2.0, resources["FontSize.Code"]);
        Assert.Equal(AppSettings.Default.TabWidth, resources["Editor.TabWidth"]);
        Assert.Equal(AppSettings.Default.UseSpaces, resources["Editor.UseSpaces"]);
    }

    [Fact]
    public void The_main_window_has_the_zoom_and_Ctrl_M_shortcuts_and_renders_the_highlighted_editor()
    {
        StaTestThread.Run(() =>
        {
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            ResourceDictionary resources = Application.Current.Resources;
            object oldSize = resources["FontSize.Editor"];
            try
            {
                DocumentViewModel document = workspace.New();
                document.Text = AllTokens;
                window.Show();
                window.UpdateLayout();
                var bindings = window.InputBindings.OfType<KeyBinding>().ToList();
                Assert.Contains(bindings, b => b.Key == Key.OemPlus && b.Modifiers == ModifierKeys.Control && b.Command == shell.ZoomInCommand);
                Assert.Contains(bindings, b => b.Key == Key.OemMinus && b.Modifiers == ModifierKeys.Control && b.Command == shell.ZoomOutCommand);
                Assert.Contains(bindings, b => b.Key == Key.D0 && b.Modifiers == ModifierKeys.Control && b.Command == shell.ZoomResetCommand);
                Assert.Contains(bindings, b => b.Key == Key.M && b.Modifiers == ModifierKeys.Control && b.Command == shell.ToggleTabMovesFocusCommand);

                CodeEditor editor = window.ActiveEditor()!;
                Assert.Equal(AllTokens, editor.Text);
                editor.Text += "    nop\n";
                Assert.EndsWith("    nop\n", document.Text, StringComparison.Ordinal); // edits reach the document
                shell.ToggleTabMovesFocusCommand.Execute(null);
                Assert.True(editor.TabMovesFocus);
                Render(window, "editor-highlighting.png");

                resources["FontSize.Editor"] = 14 * 3.0; // as the App applies 300 %
                window.UpdateLayout();
                Assert.Equal(42, editor.FontSize);
                Render(window, "editor-zoom-300.png");
            }
            finally
            {
                resources["FontSize.Editor"] = oldSize;
                document_cleanup(workspace);
                window.Close();
            }
        });

        static void document_cleanup(WorkspaceViewModel workspace)
        {
            foreach (DocumentViewModel d in workspace.Documents)
            {
                d.MarkSaved(d.FilePath ?? "x.asm");
            }
        }
    }

    private static void WithEditor(string text, Action<CodeEditor> body)
    {
        StaTestThread.Run(() =>
        {
            var editor = new CodeEditor { BoundText = text };
            var window = new Window { Content = editor, Width = 400, Height = 200 };
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.TextArea.Focus();
                body(editor);
            }
            finally
            {
                window.Close();
            }
        });
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
