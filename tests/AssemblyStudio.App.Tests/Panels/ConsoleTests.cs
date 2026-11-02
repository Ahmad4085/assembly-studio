using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Panels.Console;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Commands;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.App.Tests.Settings;
using AssemblyStudio.Toolchain.Nasm;
using Xunit;

namespace AssemblyStudio.App.Tests.Panels;

/// <summary>
/// TASK-084: the program console (UI-010): the DOS teletype rendering (FR-063), the cap (NFR-015), key translation
/// (FR-064), and the UI parts of AC-022, AC-023 and AC-045 with the real NASM and the real session, plus D-015 routing.
/// </summary>
[Collection(nameof(AppProcessTests))] // pumps the dispatcher while waiting for snapshots
public sealed class ConsoleTests
{
    private const string Flood =
        "org 100h\n" +
        "again:\n" +
        "    mov dl, 'A'\n" +
        "    mov ah, 2\n" +
        "    int 21h\n" +
        "    jmp again\n";

    private const string ControlCharacters =
        "org 100h\n" +
        "    mov dl, 7\n" +
        "    mov ah, 2\n" +
        "    int 21h\n" +
        "    mov ah, 9\n" +
        "    mov dx, msg\n" +
        "    int 21h\n" +
        "    mov ax, 4C00h\n" +
        "    int 21h\n" +
        "msg db 'A', 9, 'B', 13, 'C', 8, 'D', 13, 10, '$'\n";

    // ---- the screen ----

    [Fact]
    public void FR_063_CR_LF_BS_and_TAB_behave_like_a_DOS_screen_and_BEL_is_not_printed()
    {
        var screen = new ConsoleScreen();

        ConsoleChange change = screen.Write("A\tB\rC\bD\r\nnext\nx\a");

        Assert.Equal("D       B\nnext\n    x", screen.Text); // TAB to column 8; CR overwrites; BS doesn't erase; LF keeps the column
        Assert.Equal(1, change.Bells);
        Assert.Equal(1, screen.BellCount);
        Assert.Equal(new ConsoleChange(0, 0, 1), change);
        Assert.Equal(5, screen.Column);
    }

    [Fact]
    public void D_019_a_line_wraps_after_column_80_as_on_the_DOS_screen()
    {
        var screen = new ConsoleScreen();
        screen.Write(new string('x', 200));
        Assert.Equal([80, 80, 40], screen.Text.Split('\n').Select(l => l.Length));
        Assert.Equal(40, screen.Column);

        var exact = new ConsoleScreen();
        exact.Write(new string('y', 80) + "\r\nz");
        Assert.Equal([new string('y', 80), string.Empty, "z"], exact.Text.Split('\n')); // 80 characters then CR LF: an empty line, as in DOS
    }

    [Fact]
    public void Control_codes_show_as_their_code_page_437_symbols()
    {
        var screen = new ConsoleScreen();
        screen.Write("\u0001\u0003\u001B\u007F");
        Assert.Equal("☺♥←⌂", screen.Text);
    }

    [Fact]
    public void A_change_reports_the_first_line_that_changed()
    {
        var screen = new ConsoleScreen();
        screen.Write("one\r\ntwo");
        ConsoleChange change = screen.Write("!\r\nthree");
        Assert.Equal(new ConsoleChange(0, 1, 0), change); // "two" became "two!", then "three"
        Assert.Equal("one\ntwo!\nthree", screen.Text);
    }

    [Fact]
    public void NFR_015_the_oldest_output_is_trimmed_at_the_cap()
    {
        var screen = new ConsoleScreen(capacity: 20);
        screen.Write("line 1\r\nline 2\r\nline 3\r\n");
        ConsoleChange change = screen.Write("line 4\r\n");

        Assert.Equal("line 3\nline 4\n", screen.Text);
        Assert.True(screen.Text.Length <= 20);
        Assert.Equal(1, change.RemovedLines); // "line 1" already went with the first write (21 characters)
        Assert.Equal(14, screen.TrimmedCharacters);

        screen.Write(new string('x', 50)); // one line longer than the cap keeps its end
        Assert.Equal(20, screen.Text.Length);
        Assert.EndsWith("xxxxx", screen.Text, StringComparison.Ordinal);

        screen.Clear();
        Assert.True(screen.IsEmpty);
        Assert.False(screen.HasTrimmed);
    }

    // ---- keys ----

    [Theory]
    [InlineData(Key.Enter, ModifierKeys.None, new byte[] { 0x0D })]
    [InlineData(Key.Back, ModifierKeys.None, new byte[] { 0x08 })]
    [InlineData(Key.Tab, ModifierKeys.None, new byte[] { 0x09 })]
    [InlineData(Key.Escape, ModifierKeys.None, new byte[] { 0x1B })]
    [InlineData(Key.Z, ModifierKeys.Control, new byte[] { 0x1A })]
    [InlineData(Key.Up, ModifierKeys.None, new byte[] { 0x00, 0x48 })]
    [InlineData(Key.F3, ModifierKeys.None, new byte[] { 0x00, 0x3D })]
    [InlineData(Key.Delete, ModifierKeys.None, new byte[] { 0x00, 0x53 })]
    public void FR_064_keys_become_the_bytes_a_DOS_program_reads(Key key, ModifierKeys modifiers, byte[] expected) =>
        Assert.Equal(expected, ConsoleKeys.FromKey(key, modifiers));

    [Theory]
    [InlineData(Key.F1, ModifierKeys.None)]        // Help stays with the IDE
    [InlineData(Key.F6, ModifierKeys.None)]        // next pane: the console never traps the keyboard
    [InlineData(Key.F5, ModifierKeys.Shift)]       // Stop
    [InlineData(Key.C, ModifierKeys.Control)]      // routed by D-015, not translated
    [InlineData(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] // Copy
    [InlineData(Key.A, ModifierKeys.None)]         // a character: comes as text
    public void Keys_for_the_IDE_are_not_taken(Key key, ModifierKeys modifiers) => Assert.Null(ConsoleKeys.FromKey(key, modifiers));

    [Fact]
    public void Typed_text_is_sent_in_code_page_437()
    {
        Assert.Equal("Ada"u8.ToArray(), ConsoleKeys.FromText("Ada"));
        Assert.Equal(new byte[] { 0x82, 0x9C }, ConsoleKeys.FromText("é£"));
        Assert.Empty(ConsoleKeys.FromText("€\r")); // not in code page 437; control characters come as keys
    }

    // ---- the panel with the real session ----

    private static void WithWindow(Action<MainWindow, ShellViewModel, DocumentViewModel> body, int consoleCapacity = ConsoleScreen.DefaultCapacity)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            var run = new BuildRunViewModel(workspace,
                () => new NasmAssembler(new NasmOptions { BundledPath = SettingsTests.BundledNasm }),
                build => SessionFactory.Create(build, AppSettings.Default.InstructionBudget),
                consoleCapacity: consoleCapacity);
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: run);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                window.Show();
                window.UpdateLayout();
                body(window, shell, document);
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    [Fact]
    public void AC_022_the_hello_sample_shows_exactly_its_output_and_the_end_line_in_the_console_tab()
    {
        WithWindow((window, shell, document) =>
        {
            shell.GoToCommand.Execute(ShellPanel.Problems);
            document.Text = BuildRunTests.Hello;
            shell.BuildRun.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.Terminated, "the program to end");
            window.UpdateLayout();

            Assert.Equal(BottomTab.Console, shell.BottomTab); // the console is shown while a program runs
            var view = (ConsoleView)window.FindName("ConsoleOutput");
            Assert.Equal("Hello, World!\n", view.Text); // the view shows the screen
            Assert.Equal("— program ended (exit code 0) —", ((System.Windows.Controls.TextBlock)window.FindName("ConsoleEnded")).Text);
            Assert.False(shell.BuildRun.Console.IsEmpty);

            shell.BuildRun.ResetCommand.Execute(null); // FR-063: cleared on Reset
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.Ready, "Ready");
            Assert.Equal(string.Empty, view.Text);
            Assert.True(shell.BuildRun.Console.IsEmpty);
            Assert.Null(shell.BuildRun.Console.EndedText);
        });
    }

    [Fact]
    public void AC_023_the_console_shows_the_waiting_indicator_takes_focus_echoes_the_input_and_the_program_reads_it()
    {
        WithWindow((window, shell, document) =>
        {
            ConsoleViewModel console = shell.BuildRun.Console;
            document.Text = BuildRunTests.ReadSample("keyboard-input");
            shell.BuildRun.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.WaitingForInput, "the name prompt");
            window.UpdateLayout();

            var view = (ConsoleView)window.FindName("ConsoleOutput");
            Assert.True(console.IsWaiting);
            Assert.True(((FrameworkElement)window.FindName("ConsoleWaiting")).IsVisible);
            Assert.True(view.IsAncestorOf((DependencyObject)FocusManager.GetFocusedElement(window)!) || FocusManager.GetFocusedElement(window) == view);
            Assert.Equal("What is your name? ", view.Text);

            Assert.True(console.SendInput(ConsoleKeys.FromText("Ada"))); // typed text
            Assert.True(view.HandleKey(Key.Enter, ModifierKeys.None));  // Enter
            BuildRunTests.WaitUntil(() => view.Text.Contains("Press a key: ", StringComparison.Ordinal) && shell.BuildRun.State == RunState.WaitingForInput, "the key prompt");
            Assert.True(console.SendInput(ConsoleKeys.FromText("y")));
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.Terminated, "the program to end");

            Assert.Equal("What is your name? Ada\nHello, Ada!\nPress a key: y\nYou pressed: y\n", view.Text);
            Assert.False(console.IsWaiting);
            Assert.False(console.SendInput([0x41])); // not active any more: keys stay with the IDE
            Render(window, "console-input.png");
        });
    }

    [Fact]
    public void D_015_Ctrl_C_in_the_console_of_a_waiting_program_breaks_it_and_copies_otherwise()
    {
        WithWindow((window, shell, document) =>
        {
            document.Text = BuildRunTests.ReadSample("keyboard-input");
            shell.BuildRun.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.WaitingForInput, "the name prompt");
            var view = (ConsoleView)window.FindName("ConsoleOutput");

            Assert.True(view.HandleKey(Key.C, ModifierKeys.Control)); // delivered to the program as Ctrl-C
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.Terminated, "the break");
            window.UpdateLayout();
            Assert.Contains("^C", view.Text, StringComparison.Ordinal); // DOS echoes ^C and ends the program (INT 23h)

            Assert.False(view.HandleKey(Key.C, ModifierKeys.Control)); // no active program: Ctrl+C is the editor's Copy
            Assert.Null(ConsoleKeys.FromKey(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            Assert.Contains(view.InputBindings.OfType<KeyBinding>(), b => b.Key == Key.C && b.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && b.Command == ConsoleView.CopyAll);
            Assert.True(window.ResolveShortcut(Key.Cancel, ModifierKeys.Control) is not null); // Ctrl+Break from anywhere (TASK-080)
        });
    }

    [Fact]
    public void Control_characters_from_a_program_render_and_BEL_flashes_without_printing()
    {
        WithWindow((window, shell, document) =>
        {
            document.Text = ControlCharacters;
            shell.BuildRun.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.Terminated, "the program to end");
            window.UpdateLayout();
            var view = (ConsoleView)window.FindName("ConsoleOutput");
            Assert.Equal("D       B\n", view.Text);
            Assert.Equal(1, shell.BuildRun.Console.Screen.BellCount);
            Assert.NotEqual(Brushes.Transparent, view.BorderBrush); // the visual bell (300 ms)
        });
    }

    [Fact]
    public void AC_045_an_output_flood_stays_within_the_cap_the_UI_stays_responsive_and_Stop_works()
    {
        WithWindow((window, shell, document) =>
        {
            ConsoleViewModel console = shell.BuildRun.Console;
            document.Text = Flood;
            shell.BuildRun.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => console.TrimmedNotice is not null, "the cap to be reached", seconds: 60);
            var view = (ConsoleView)window.FindName("ConsoleOutput");

            for (int i = 0; i < 5; i++) // the UI thread keeps answering input while the flood goes on
            {
                DateTime until = DateTime.UtcNow.AddMilliseconds(200);
                BuildRunTests.WaitUntil(() => DateTime.UtcNow >= until, "200 ms of flood");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                bool answered = false;
                window.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => answered = true);
                BuildRunTests.WaitUntil(() => answered, "an input-priority callback", seconds: 5);
                Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"UI round trip {stopwatch.ElapsedMilliseconds} ms");
                Assert.True(console.Screen.Text.Length <= 4096);
                Assert.True(view.Document.TextLength <= 4096);
            }

            Assert.Equal(RunState.Running, shell.BuildRun.State);
            Assert.True(window.TryRunShortcut(Key.F5, ModifierKeys.Shift)); // Stop
            Assert.Equal(RunState.Editing, shell.BuildRun.State);
            Assert.Contains("4,096", console.TrimmedNotice!, StringComparison.Ordinal);
            string[] lines = console.Screen.Text.Split('\n');
            Assert.All(lines, line => Assert.True(line.TrimEnd('\r').Length <= ConsoleScreen.Width)); // wrapped at 80 (D-019)
            Assert.EndsWith("AAAA", string.Concat(lines.Select(line => line.TrimEnd('\r'))), StringComparison.Ordinal); // Stop may land at any column
        }, consoleCapacity: 4096);
    }

    internal static void Render(Window window, string fileName)
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
