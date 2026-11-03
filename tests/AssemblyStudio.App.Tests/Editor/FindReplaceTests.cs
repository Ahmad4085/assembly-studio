using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor;
using AssemblyStudio.App.Editor.Search;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.App.Tests.Panels;
using Xunit;

namespace AssemblyStudio.App.Tests.Editor;

/// <summary>
/// TASK-065: find, replace and go to line (UI-004; FR-015, FR-016, FR-017; AC-011): the matching rules on exact offsets,
/// the bar's find next/previous with wrap-around, replace and replace all, Go to Line with its range check, and the bar in
/// the real window (keyboard, focus, one undo step for Replace All).
/// </summary>
public sealed class FindReplaceTests
{
    // Offsets: line 1 starts at 0, line 2 at 10, line 3 at 22. "ax" also occurs inside "max" (19) and as "AX" (14).
    private const string Text = "mov ax, 1\nMOV AX, max\nadd ax, ax\n";

    [Theory]
    [InlineData(false, false, new[] { 4, 14, 19, 26, 30 })]
    [InlineData(true, false, new[] { 4, 19, 26, 30 })]
    [InlineData(false, true, new[] { 4, 14, 26, 30 })]
    [InlineData(true, true, new[] { 4, 26, 30 })]
    public void AC_011_matches_follow_match_case_and_whole_word(bool matchCase, bool wholeWord, int[] expected) =>
        Assert.Equal(expected, SearchRules.FindAll(Text, "ax", new SearchOptions(matchCase, wholeWord)));

    [Fact]
    public void Matches_do_not_overlap_and_a_whole_word_may_not_touch_a_NASM_name_character()
    {
        Assert.Equal([0, 2], SearchRules.FindAll("aaaa", "aa", default));
        Assert.Empty(SearchRules.FindAll(".next _ax ax_ $ax", "next", new SearchOptions(false, true))); // . is part of a local label
        Assert.Equal([13], SearchRules.FindAll("_ax ax_ $ax [ax+1]", "ax", new SearchOptions(false, true)));
        Assert.Empty(SearchRules.FindAll(Text, string.Empty, default));
    }

    [Fact]
    public void AC_011_find_next_and_previous_go_through_every_match_and_wrap_around()
    {
        var target = new FakeTarget(Text);
        var find = new FindReplaceViewModel(() => target);
        find.FindCommand.Execute(null);
        find.Query = "ax"; // searches as you type, from the caret (0)
        Assert.Equal((4, 2), target.Selection);
        Assert.Equal("Match 1 of 5", find.Status);

        List<int> visited = [];
        for (int i = 0; i < 5; i++)
        {
            find.FindNextCommand.Execute(null);
            visited.Add(target.Selection.Start);
        }

        Assert.Equal([14, 19, 26, 30, 4], visited);
        Assert.Equal("Match 1 of 5 (wrapped around)", find.Status);

        find.FindPreviousCommand.Execute(null); // before 4: wraps to the last match
        Assert.Equal(30, target.Selection.Start);
        Assert.Equal("Match 5 of 5 (wrapped around)", find.Status);
        find.FindPreviousCommand.Execute(null);
        Assert.Equal((26, "Match 4 of 5"), (target.Selection.Start, find.Status));

        find.MatchCase = true; // the option applies at once: the selected match still matches
        Assert.Equal((26, "Match 3 of 4"), (target.Selection.Start, find.Status));
        find.WholeWord = true;
        find.FindPreviousCommand.Execute(null);
        Assert.Equal((4, "Match 1 of 3"), (target.Selection.Start, find.Status)); // "max" (19) is skipped
    }

    [Fact]
    public void AC_011_replace_replaces_only_a_selected_match_then_selects_the_next()
    {
        var target = new FakeTarget(Text);
        var find = new FindReplaceViewModel(() => target) { };
        find.OpenReplaceCommand.Execute(null);
        find.WholeWord = true;
        find.Query = "ax";
        find.Replacement = "bx";
        target.Selection = (0, 0); // nothing selected: Replace first finds

        find.ReplaceCommand.Execute(null);
        Assert.Equal(Text, target.Text);
        Assert.Equal((4, 2), target.Selection);

        find.ReplaceCommand.Execute(null);
        Assert.Equal("mov bx, 1\nMOV AX, max\nadd ax, ax\n", target.Text);
        Assert.Equal((14, 2), target.Selection); // the next whole-word match ("AX")
        Assert.Equal("Match 1 of 3", find.Status);
    }

    [Fact]
    public void AC_011_replace_all_replaces_every_match_once()
    {
        var target = new FakeTarget(Text);
        var find = new FindReplaceViewModel(() => target);
        find.OpenReplaceCommand.Execute(null);
        find.WholeWord = true;
        find.Query = "ax";
        find.Replacement = "eax"; // longer than the match: offsets must not shift between replacements

        find.ReplaceAllCommand.Execute(null);

        Assert.Equal("mov eax, 1\nMOV eax, max\nadd eax, eax\n", target.Text);
        Assert.Equal(1, target.ReplaceAllCalls); // one undo step in the editor
        Assert.Equal("Replaced 4", find.Status);

        find.ReplaceAllCommand.Execute(null); // "eax" isn't "ax" as a whole word
        Assert.Equal("No matches for \"ax\"", find.Status);
    }

    [Fact]
    public void Opening_the_bar_takes_a_one_line_selection_as_the_query()
    {
        var target = new FakeTarget(Text) { Selection = (18, 3) }; // "max"
        var find = new FindReplaceViewModel(() => target);
        bool? focusedReplace = null;
        find.FocusRequested += (_, replace) => focusedReplace = replace;

        find.FindCommand.Execute(null);
        Assert.Equal(("max", "Match 1 of 1", false), (find.Query, find.Status, focusedReplace));
        Assert.Equal((18, 3), target.Selection); // not moved

        target.Selection = (0, 12); // two lines: kept out of the query
        find.OpenReplaceCommand.Execute(null);
        Assert.Equal("max", find.Query);
        Assert.True(find.IsReplaceShown);
        Assert.True(focusedReplace); // the query is there: on to the replacement
    }

    [Fact]
    public void No_document_disables_the_commands_and_no_match_says_so()
    {
        ISearchTarget? none = null;
        var find = new FindReplaceViewModel(() => none);
        Assert.False(find.FindCommand.CanExecute(null));
        Assert.False(find.GoToLineCommand.CanExecute(null));

        var target = new FakeTarget(Text);
        find = new FindReplaceViewModel(() => target);
        find.FindCommand.Execute(null);
        find.Query = "zz";
        Assert.Equal(("No matches for \"zz\"", (0, 0)), (find.Status, target.Selection));
    }

    [Theory]
    [InlineData("3", 3, true, 3)]
    [InlineData(" 2 ", 3, true, 2)]
    [InlineData("1", 1, true, 1)]
    [InlineData("0", 3, false, 0)]
    [InlineData("4", 3, false, 4)]
    [InlineData("-1", 3, false, 0)]
    [InlineData("2.5", 3, false, 0)]
    [InlineData("", 3, false, 0)]
    [InlineData("two", 3, false, 0)]
    public void AC_011_go_to_line_accepts_only_a_line_of_the_document(string text, int lineCount, bool valid, int line)
    {
        Assert.Equal(valid, FindReplaceViewModel.TryParseLine(text, lineCount, out int parsed));
        if (valid)
        {
            Assert.Equal(line, parsed);
        }
    }

    [Fact]
    public void AC_011_go_to_line_moves_the_caret_to_the_line_start()
    {
        var target = new FakeTarget(Text);
        var find = new FindReplaceViewModel(() => target) { AskLine = (count, current) => count == 4 && current == 1 ? 3 : null };
        find.GoToLineCommand.Execute(null);
        Assert.Equal(3, target.WentToLine);

        target.WentToLine = null;
        find.AskLine = (_, _) => null; // cancelled
        find.GoToLineCommand.Execute(null);
        Assert.Null(target.WentToLine);
    }

    [Fact]
    public void The_Go_to_Line_dialog_keeps_an_invalid_entry_open_with_the_reason()
    {
        StaTestThread.Run(() =>
        {
            var dialog = new GoToLineDialog(lineCount: 12, currentLine: 5);
            Assert.Equal("5", dialog.Entry);

            dialog.Entry = "13";
            Assert.False(dialog.TryAccept());
            Assert.Equal("Enter a whole number from 1 to 12.", dialog.ErrorText);
            Assert.Null(dialog.Line);

            dialog.Entry = "12";
            Assert.True(dialog.TryAccept());
            Assert.Equal(12, dialog.Line);
            dialog.Close();
        });
    }

    [Fact]
    public void UI_004_the_bar_in_the_window_finds_replaces_all_in_one_undo_step_and_Esc_returns_to_the_editor()
    {
        StaTestThread.Run(() =>
        {
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = Text;
                window.Show();
                window.UpdateLayout();
                CodeEditor editor = window.ActiveEditor()!;
                var bar = (FrameworkElement)window.FindName("FindBar");
                var findBox = (TextBox)window.FindName("FindBox");
                Assert.False(bar.IsVisible);

                shell.Find.OpenReplaceCommand.Execute(null); // Ctrl+H
                Assert.True(bar.IsVisible);
                Assert.Same(findBox, FocusManager.GetFocusedElement(window)); // no query yet: the find box
                findBox.Text = "ax"; // typed
                Assert.Equal((4, 2), (editor.SelectionStart, editor.SelectionLength));

                RunBinding(findBox, Key.Enter); // Enter in the find box: next match
                Assert.Equal(14, editor.SelectionStart);
                RunBinding(editor.TextArea, Key.F3); // F3 in the editor
                Assert.Equal(19, editor.SelectionStart);

                shell.Find.WholeWord = true;
                shell.Find.Replacement = "bx";
                ((Button)window.FindName("ReplaceAllButton")).Command.Execute(null);
                Assert.Equal("mov bx, 1\nMOV bx, max\nadd bx, bx\n", editor.Text);
                Assert.Equal(editor.Text, document.Text);
                window.UpdateLayout();
                ConsoleTests.Render(window, "find-replace-bar.png");
                editor.Undo(); // Replace All is one undo step
                Assert.Equal(Text, editor.Text);

                findBox.Focus();
                RunBinding(findBox, Key.Escape); // the bar's Esc, before the window's Esc (Cancel build)
                Assert.False(shell.Find.IsOpen);
                Assert.Same(editor.TextArea, FocusManager.GetFocusedElement(window));

                shell.Find.AskLine = (_, _) => 3;
                shell.Find.GoToLineCommand.Execute(null); // Ctrl+G
                Assert.Equal((3, 1), (editor.TextArea.Caret.Line, editor.TextArea.Caret.Column));
                Assert.Equal(0, editor.SelectionLength);
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    /// <summary>
    /// What a key press at <paramref name="target"/> runs: the first key binding for it on the way up from the target, as WPF
    /// routes the key, which must exist and be able to run; then runs it. Not a simulated key press: WPF matches those against
    /// the real keyboard's modifier state, so a key held on the PC made the test fail (DEF-022, TASK-109).
    /// </summary>
    private static void RunBinding(DependencyObject target, Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        for (DependencyObject? element = target; element is not null; element = VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element))
        {
            if (element is UIElement ui && ui.InputBindings.OfType<KeyBinding>().FirstOrDefault(b => b.Key == key && b.Modifiers == modifiers) is { } binding)
            {
                Assert.NotNull(binding.Command);
                Assert.True(binding.Command.CanExecute(binding.CommandParameter), $"{modifiers}+{key} can't run here");
                binding.Command.Execute(binding.CommandParameter);
                return;
            }
        }

        Assert.Fail($"No key binding for {modifiers}+{key} from {target.GetType().Name} up");
    }

    /// <summary>An editor stand-in: text and selection, with the replace calls counted.</summary>
    private sealed class FakeTarget(string text) : ISearchTarget
    {
        public string Text { get; private set; } = text;

        public (int Start, int Length) Selection { get; set; }

        public int ReplaceAllCalls { get; private set; }

        public int? WentToLine { get; set; }

        public int SelectionStart => Selection.Start;

        public int SelectionLength => Selection.Length;

        public int LineCount => Text.Split('\n').Length;

        public int CaretLine => 1;

        public void SelectRange(int start, int length) => Selection = (start, length);

        public void Replace(int start, int length, string replacement)
        {
            Text = Text[..start] + replacement + Text[(start + length)..];
            Selection = (start + replacement.Length, 0);
        }

        public void ReplaceAll(IReadOnlyList<int> starts, int length, string replacement)
        {
            ReplaceAllCalls++;
            for (int i = starts.Count - 1; i >= 0; i--)
            {
                Text = Text[..starts[i]] + replacement + Text[(starts[i] + length)..];
            }
        }

        public void GoToLine(int line) => WentToLine = line;

        public void Focus()
        {
        }
    }
}
