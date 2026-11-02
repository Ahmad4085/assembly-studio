using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor.Adornments;
using AssemblyStudio.App.Panels.Machine;
using AssemblyStudio.App.Panels.Session;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Commands;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.App.Tests.Settings;
using AssemblyStudio.Toolchain.Nasm;
using Xunit;

namespace AssemblyStudio.App.Tests.Panels;

/// <summary>
/// TASK-083: the current-instruction strip (UI-007), runtime messages (UI-012), execution history (UI-013) and breakpoints
/// (UI-014) panels over the real NASM and the real session: AC-024, AC-028, AC-029, AC-031 (UI parts), AC-048, AC-049, the
/// instruction budget with Continue/Stop (FR-042), the end of the program (FR-041) and breakpoint enable/disable (FR-052).
/// </summary>
[Collection(nameof(AppProcessTests))] // pumps the dispatcher while waiting for snapshots
public sealed class SessionPanelsTests
{
    private static void WithRun(string text, Action<BuildRunViewModel, DocumentViewModel> body, ulong budget = 0)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            DocumentViewModel document = workspace.New();
            document.Text = text;
            using BuildRunViewModel run = budget == 0
                ? BuildRunTests.Create(workspace)
                : new BuildRunViewModel(workspace,
                    () => new NasmAssembler(new NasmOptions { BundledPath = SettingsTests.BundledNasm }),
                    build => SessionFactory.Create(build, budget));
            body(run, document);
        });
    }

    private static void Step(BuildRunViewModel run)
    {
        ulong before = run.InstructionCount;
        run.StepIntoCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State != RunState.Stepping && run.InstructionCount >= before, "a step");
    }

    private static void RunToHalt(BuildRunViewModel run)
    {
        run.RunCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State is RunState.HaltedError or RunState.HaltedUnsupported, "the halt");
    }

    [Fact]
    public void AC_024_the_strip_shows_the_next_instruction_and_moves_with_each_step()
    {
        WithRun("org 100h\n    mov ax, 1234h\n    jmp skip\n    nop\nskip:\n    int 20h\n", (run, document) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Assert.True(run.Strip.IsVisible);
            Assert.StartsWith("0800:0100   B8 34 12", run.Strip.Text, StringComparison.Ordinal);
            Assert.EndsWith("mov ax, 1234h", run.Strip.Text, StringComparison.Ordinal);
            Assert.Null(run.Strip.Note);
            Assert.Equal(2, document.ExecutionLine);

            Step(run); // mov
            Assert.StartsWith("0800:0103", run.Strip.Text, StringComparison.Ordinal);
            Assert.Contains("jmp", run.Strip.Text, StringComparison.Ordinal);
            Assert.Equal(3, document.ExecutionLine);

            Step(run); // jmp: to the target, past the nop
            Assert.Contains("int 20h", run.Strip.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(6, document.ExecutionLine);
            Assert.Equal("Program loaded at 0800:0100.", Assert.Single(run.Messages.Items).Text); // steps aren't logged

            run.StopCommand.Execute(null);
            Assert.False(run.Strip.IsVisible);
        });
    }

    [Fact]
    public void AC_028_an_unsupported_instruction_halts_before_it_with_CS_IP_the_bytes_and_the_reason()
    {
        WithRun("org 100h\n    mov ax, 1\n    db 0Fh, 0A2h\n    int 20h\n", (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            RunToHalt(run);

            Assert.Equal(RunState.HaltedUnsupported, run.State);
            SessionMessage message = run.Messages.Items[^1];
            Assert.Equal((SessionMessageKind.Error, "RTE010"), (message.Kind, message.Code));
            Assert.Contains("bytes at CS:IP: 0F A2 CD 20", message.Text, StringComparison.Ordinal);
            Assert.Contains("at 0800:0103", message.Text, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(message.NextStep));
            Assert.Contains("RTE010", run.Strip.HaltText, StringComparison.Ordinal);
            Assert.StartsWith("0800:0103   0F A2", run.Strip.Text, StringComparison.Ordinal);
            Assert.Equal(CpuPanelState.Current, run.Cpu.State); // the state stays inspectable
            Assert.Equal("0001", run.Cpu.Registers.Single(r => r.Name == "AX").Text);
            Assert.StartsWith("RTE010", run.StatusText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AC_029_an_unsupported_DOS_function_halts_with_its_name()
    {
        WithRun("org 100h\n    mov ah, 3Dh\n    int 21h\n    int 20h\n", (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            RunToHalt(run);

            Assert.Equal(RunState.HaltedUnsupported, run.State);
            SessionMessage message = run.Messages.Items[^1];
            Assert.Equal("RTE021", message.Code);
            Assert.Contains("INT 21h function 3Dh", message.Text, StringComparison.Ordinal);
            Assert.Contains("at 0800:0102", message.Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AC_031_a_divide_by_zero_halts_with_a_divide_error_at_that_CS_IP()
    {
        WithRun("org 100h\n    mov ax, 5\n    mov bl, 0\n    div bl\n    int 20h\n", (run, document) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Step(run);
            Step(run); // paused before div bl with BL=0
            Step(run);

            Assert.Equal(RunState.HaltedError, run.State);
            SessionMessage message = run.Messages.Items[^1];
            Assert.Equal("RTE001", message.Code);
            Assert.StartsWith("Divide error", message.Text, StringComparison.Ordinal);
            Assert.Contains("at 0800:0105", message.Text, StringComparison.Ordinal);
            Assert.Equal(ExecutionMarkerKind.Fault, document.ExecutionMarker);
            Assert.Equal(4, document.ExecutionLine);
        });
    }

    [Fact]
    public void FR_042_the_instruction_budget_pauses_with_Continue_and_Stop()
    {
        WithRun("org 100h\nagain:\n    inc ax\n    jmp again\n", (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "the budget");

            Assert.True(run.Messages.OffersBudgetChoice);
            SessionMessage budget = run.Messages.Items[^1];
            Assert.Equal(SessionMessageKind.Warning, budget.Kind);
            Assert.Contains("1,000", budget.Text, StringComparison.Ordinal);
            Assert.Equal(1000UL, run.InstructionCount);

            run.RunCommand.Execute(null); // Continue: another budget
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused && run.InstructionCount == 2000, "the second budget");
            Assert.Equal(2, run.Messages.Items.Count(m => m.Kind == SessionMessageKind.Warning));

            run.StopCommand.Execute(null); // Stop
            Assert.False(run.Messages.OffersBudgetChoice);
            Assert.Equal(RunState.Editing, run.State);
        }, budget: 1000);
    }

    [Fact]
    public void FR_041_the_end_of_the_program_names_the_termination_path_and_exit_code()
    {
        WithRun(BuildRunTests.Hello, (run, _) =>
        {
            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Terminated, "the end");
            Assert.Equal("Program ended through INT 21h function 4Ch with exit code 0.", run.Messages.Items[^1].Text);
        });
    }

    [Fact]
    public void AC_049_and_AC_048_the_history_lists_the_steps_in_order_and_is_not_current_while_running()
    {
        WithRun("org 100h\nagain:\n    inc ax\n    dec bx\n    jmp again\n", (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Assert.True(run.History.IsEmpty);
            for (int i = 0; i < 20; i++)
            {
                Step(run);
            }

            Assert.Equal(20, run.History.Rows.Count);
            string[] expected = [.. Enumerable.Range(0, 20).Select(i => (i % 3) switch { 0 => "inc ax", 1 => "dec bx", _ => "jmp" })];
            Assert.Equal(expected, run.History.Rows.Select(r => r.Disassembly.Split(' ')[0] == "jmp" ? "jmp" : r.Disassembly));
            Assert.Equal(["0800:0100", "0800:0101", "0800:0102", "0800:0100"], run.History.Rows.Take(4).Select(r => r.Address));
            Assert.Equal([3, 4, 5, 3], run.History.Rows.Take(4).Select(r => r.Line!.Value));

            int? selected = null;
            run.History.GoToLineRequested += (_, line) => selected = line;
            run.History.ActivateCommand.Execute(run.History.Rows[1]);
            Assert.Equal(4, selected);

            run.RunCommand.Execute(null); // AC-048
            BuildRunTests.WaitUntil(() => run.History.IsNotCurrent && run.Strip.IsNotCurrent, "not current");
            run.PauseCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "Paused");
            Assert.False(run.History.IsNotCurrent);
            Assert.False(run.Strip.IsNotCurrent);
            Assert.Equal(1000, run.History.Rows.Count); // the last 1,000 (FR-058)
            Assert.Contains(run.Messages.Items, m => m.Text.StartsWith(Strings.MessagePaused, StringComparison.Ordinal));
        });
    }

    [Fact]
    public void FR_052_the_breakpoints_panel_lists_marks_the_hit_one_and_disables_and_removes()
    {
        WithRun(BuildRunTests.Loop, (run, document) =>
        {
            document.ToggleBreakpointCommand.Execute(5);
            document.ToggleBreakpointCommand.Execute(4);
            Assert.Equal(["Untitled:4", "Untitled:5"], run.Breakpoints.Items.Select(i => i.Location));
            Assert.Equal(Strings.BreakpointStateNotBuilt, run.Breakpoints.Items[0].State);

            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Assert.Equal([Strings.BreakpointStateUnbound, "0800:0103"], run.Breakpoints.Items.Select(i => i.State));

            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "the breakpoint");
            Assert.True(run.Breakpoints.Items.Single(i => i.Breakpoint.Line == 5).IsHit);
            Assert.StartsWith(Strings.MessageBreakpoint + " at line 5", run.Messages.Items[^1].Text, StringComparison.Ordinal);

            run.Breakpoints.ToggleEnabledCommand.Execute(run.Breakpoints.Items.Single(i => i.Breakpoint.Line == 5)); // disable
            Assert.Equal(Strings.BreakpointStateDisabled, run.Breakpoints.Items.Single(i => i.Breakpoint.Line == 5).State);
            Assert.False(document.Breakpoints.Single(b => b.Line == 5).IsEnabled);
            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Terminated, "the end: the disabled breakpoint doesn't stop");

            run.Breakpoints.RemoveCommand.Execute(run.Breakpoints.Items[0]);
            Assert.Equal([5], document.Breakpoints.Select(b => b.Line));
            BreakpointItem? target = null;
            run.Breakpoints.GoToRequested += (_, item) => target = item;
            run.Breakpoints.ActivateCommand.Execute(run.Breakpoints.Items[0]);
            Assert.Equal(5, target!.Breakpoint.Line);
        });
    }

    [Fact]
    public void A_halt_shows_the_runtime_tab_and_the_panels_render_for_review()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: BuildRunTests.Create(workspace));
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = "org 100h\n    mov ax, 5\n    mov bl, 0\n    div bl\n    int 20h\n";
                window.Show();
                window.UpdateLayout();
                BuildRunViewModel run = shell.BuildRun;
                run.BuildCommand.Execute(null);
                BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
                window.UpdateLayout();
                Assert.True(((FrameworkElement)window.FindName("InstructionStrip")).IsVisible);

                RunToHalt(run);
                window.UpdateLayout();
                Assert.Equal(BottomTab.Runtime, shell.BottomTab);
                var messages = (ListBox)window.FindName("MessagesList");
                Assert.True(messages.IsVisible);
                var last = (ListBoxItem)messages.ItemContainerGenerator.ContainerFromIndex(messages.Items.Count - 1);
                Assert.Equal(run.Messages.Items[^1].AccessibleName, System.Windows.Automation.AutomationProperties.GetName(last));
                Assert.True(((FrameworkElement)window.FindName("StripHalt")).IsVisible);
                Render(window, "runtime-halt.png");

                shell.GoToCommand.Execute(ShellPanel.History);
                window.UpdateLayout();
                Render(window, "history.png");
            }
            finally
            {
                document.MarkSaved("x.asm");
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
