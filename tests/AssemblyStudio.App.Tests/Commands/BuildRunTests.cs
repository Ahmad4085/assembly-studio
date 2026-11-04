using System.Windows.Threading;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor.Adornments;
using AssemblyStudio.App.Panels.Problems;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.App.Tests.Settings;
using AssemblyStudio.Content;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm;
using Xunit;

namespace AssemblyStudio.App.Tests.Commands;

/// <summary>
/// TASK-080: the build and session commands over the real NASM and the real 8086 session: AC-017 and AC-013 (UI flow),
/// Run to the end, stepping, breakpoints, Stop and Reset, cancelling a build, and a missing assembler. No fake machine data:
/// every state comes from the session's snapshots.
/// </summary>
// These tests pump the dispatcher while they wait for snapshots, which would let other UI tests run inside them: they run
// in the non-parallel collection.
[Collection(nameof(AppProcessTests))]
public sealed class BuildRunTests
{
    internal static readonly string Hello = ReadSample("hello");

    internal const string WarningAndError =
        "org 100h\n" +
        "start:\n" +
        "orphan\n" +              // line 3: label without a colon: warning
        "mov ax, 1\n" +
        "mov bx, 2\n" +
        "mov cx, 3\n" +
        "mov ax,\n" +             // line 7: error
        "int 20h\n";

    // A loop that runs line 5 three times, then ends with exit code 0.
    internal const string Loop =
        "org 100h\n" +
        "    mov cx, 3\n" +
        "again:\n" +
        "    ; the loop body\n" +
        "    dec cx\n" +
        "    jnz again\n" +
        "    mov ax, 4C00h\n" +
        "    int 21h\n";

    internal static string ReadSample(string id)
    {
        EmbeddedContentCatalog catalog = EmbeddedContentCatalog.BuiltIn();
        return catalog.ReadText(catalog.Items.Single(i => i.Id == id));
    }

    internal static BuildRunViewModel Create(WorkspaceViewModel workspace, string? configuredAssembler = null) =>
        new(workspace,
            () => new NasmAssembler(new NasmOptions { BundledPath = SettingsTests.BundledNasm, ConfiguredPath = configuredAssembler }),
            build => SessionFactory.Create(build, AppSettings.Default.InstructionBudget));

    internal static WorkspaceViewModel Workspace() => new(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());

    /// <summary>Pumps the dispatcher until <paramref name="condition"/> holds (snapshots arrive through it).</summary>
    internal static void WaitUntil(Func<bool> condition, string what, int seconds = 30)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, $"Timed out waiting for {what}.");
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
            Thread.Sleep(5);
        }
    }

    private static void WithRun(string text, Action<BuildRunViewModel, DocumentViewModel> body, string? configuredAssembler = null)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = Workspace();
            DocumentViewModel document = workspace.New();
            document.Text = text;
            using BuildRunViewModel run = Create(workspace, configuredAssembler);
            body(run, document);
        });
    }

    [Fact]
    public void AC_017_building_the_hello_sample_succeeds_reports_size_duration_and_warnings_and_enables_Run()
    {
        WithRun(Hello, (run, document) =>
        {
            Assert.Equal(RunState.Editing, run.State);
            Assert.True(run.BuildCommand.CanExecute(null));

            run.BuildCommand.Execute(null);
            Assert.Equal(RunState.Assembling, run.State); // the build runs off the UI thread (FR-067)
            Assert.True(run.CancelBuildCommand.CanExecute(null));
            WaitUntil(() => run.State == RunState.Ready, "Ready");

            BuildResult build = run.LastBuild!;
            Assert.Equal(BuildOutcome.Succeeded, build.Outcome);
            Assert.NotNull(build.SourceMap); // FR-028: the mapping data
            Assert.Equal(
                $"Build OK · {build.Image.Length} bytes · 0 warnings · {(long)build.Duration.TotalMilliseconds} ms · Ready",
                run.StatusText);
            Assert.Equal("● Ready", run.BadgeText);
            Assert.True(run.Problems.IsEmpty);
            Assert.Equal(Strings.EmptyProblems, run.Problems.EmptyText);
            Assert.True(run.RunCommand.CanExecute(null));
            Assert.True(run.StepIntoCommand.CanExecute(null));
            Assert.True(document.HasSession);
            Assert.StartsWith("NASM ", run.AssemblerText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AC_013_a_failed_build_lists_both_messages_errors_first_marks_the_lines_and_keeps_Run_disabled()
    {
        WithRun(WarningAndError, (run, document) =>
        {
            run.BuildCommand.Execute(null);
            WaitUntil(() => run.State == RunState.BuildFailed, "Build failed");

            Assert.Equal(
                [(DiagnosticSeverity.Error, 7, "⨯", "Line 7"), (DiagnosticSeverity.Warning, 3, "▲", "Line 3")],
                run.Problems.Items.Select(i => (i.Diagnostic.Severity, i.Diagnostic.Line!.Value, i.Glyph, i.Location)));
            Assert.All(run.Problems.Items, i => Assert.True(i.CanGoTo));
            Assert.Equal([3, 7], document.Diagnostics.Select(d => d.Line!.Value).Order()); // marked in the editor
            Assert.Equal("Build failed · 1 error", run.StatusText);
            Assert.Equal("⨯ Build failed", run.BadgeText);
            Assert.False(run.RunCommand.CanExecute(null)); // AC-013; ui-spec §4 "Build failed": Run only after an edit (D-024)
            Assert.False(run.StepIntoCommand.CanExecute(null));
            Assert.False(document.HasSession);

            ProblemItem? target = null;
            run.Problems.GoToRequested += (_, item) => target = item;
            run.Problems.ActivateCommand.Execute(run.Problems.Items[0]);
            Assert.Equal(7, target!.Diagnostic.Line);
            Assert.Same(document, target.Document);

            document.Text = document.Text.Replace("mov ax,\n", "mov ax, 0\n", StringComparison.Ordinal); // fix the error
            Assert.True(run.Problems.IsOutdated);
            Assert.Equal(RunState.Editing, run.State);
            Assert.True(run.RunCommand.CanExecute(null)); // Run rebuilds now (ui-spec §4, D-024)
        });
    }

    [Fact]
    public void Run_builds_when_needed_runs_to_the_end_and_shows_the_output_and_the_exit_code()
    {
        WithRun(Hello, (run, document) =>
        {
            run.RunCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Terminated, "the program to end");

            Assert.Equal("Hello, World!\n", run.ConsoleText); // as the console shows it: CR LF ends the line
            Assert.Equal(0, run.ExitCode);
            Assert.Equal("■ Ended · exit 0", run.BadgeText);
            Assert.StartsWith("Ended · exit 0 · ", run.StatusText, StringComparison.Ordinal);
            Assert.Equal(ExecutionMarkerKind.Ended, document.ExecutionMarker);
            Assert.NotNull(document.ExecutionLine);
            Assert.True(run.RunCommand.CanExecute(null)); // Run again restarts (ui-spec §4: Terminated → Reset, Build, Run)

            run.RunCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Terminated && run.ConsoleText.Length > 0, "the second run to end");
            Assert.Equal("Hello, World!\n", run.ConsoleText); // the console was cleared for the new run
        });
    }

    [Fact]
    public void FR_049_stepping_moves_the_execution_line_and_Stop_ends_the_session()
    {
        WithRun(Loop, (run, document) =>
        {
            run.BuildCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Ready, "Ready");
            Assert.Equal(2, document.ExecutionLine); // the first instruction, before anything runs
            Assert.Equal(0UL, run.InstructionCount);

            run.StepIntoCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Paused, "Paused after Step Into");
            Assert.Equal(1UL, run.InstructionCount);
            Assert.Equal(5, run.CurrentLine); // the label line and the comment generate no code
            Assert.Equal(5, document.ExecutionLine);
            Assert.Equal(ExecutionMarkerKind.Current, document.ExecutionMarker);
            Assert.Equal("❚❚ Paused · line 5", run.BadgeText);
            Assert.Equal("Paused · line 5 · 1 instruction", run.StatusText);

            run.StepOverCommand.Execute(null); // not a CALL or INT: one instruction
            WaitUntil(() => run.State == RunState.Paused && run.InstructionCount == 2, "Paused after Step Over");
            Assert.Equal(6, document.ExecutionLine);

            Assert.True(run.StopCommand.CanExecute(null));
            run.StopCommand.Execute(null);
            Assert.Equal(RunState.Editing, run.State);
            Assert.False(document.HasSession);
            Assert.Null(document.ExecutionLine);
            Assert.False(run.StepIntoCommand.CanExecute(null));
        });
    }

    [Fact]
    public void FR_052_breakpoints_bind_after_the_build_pause_the_run_and_Reset_reloads()
    {
        WithRun(Loop, (run, document) =>
        {
            document.ToggleBreakpointCommand.Execute(5); // dec cx
            document.ToggleBreakpointCommand.Execute(4); // a comment: no code

            run.BuildCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Ready, "Ready");
            EditorBreakpoint bound = document.Breakpoints.Single(b => b.Line == 5);
            Assert.Equal(BreakpointBinding.Bound, bound.Binding);
            Assert.Equal("0800:0103", bound.Address); // mov cx, 3 is 3 bytes at 0100h
            Assert.Equal(BreakpointBinding.Unbound, document.Breakpoints.Single(b => b.Line == 4).Binding);

            run.RunCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Paused, "the breakpoint");
            Assert.Equal(5, run.CurrentLine);
            Assert.Equal(1UL, run.InstructionCount);

            run.RunCommand.Execute(null); // Continue: the loop comes back to line 5
            WaitUntil(() => run.State == RunState.Paused && run.InstructionCount > 1, "the breakpoint again");
            Assert.Equal(5, run.CurrentLine);
            Assert.Equal(3UL, run.InstructionCount); // mov, dec, jnz: paused again before dec

            document.ToggleBreakpointCommand.Execute(5); // removed in the editor: removed from the session
            run.RunCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Terminated, "the end");

            run.ResetCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Ready, "Ready after Reset");
            Assert.Equal(0UL, run.InstructionCount);
            Assert.Equal(2, document.ExecutionLine);
        });
    }

    [Fact]
    public void Editing_during_a_session_marks_it_stale_and_Reset_rebuilds_the_new_text()
    {
        WithRun(Loop, (run, document) =>
        {
            run.BuildCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Ready, "Ready");
            int size = run.LastBuild!.Image.Length;

            document.Text = document.Text.Replace("    mov cx, 3\n", "    mov cx, 3\n    nop\n", StringComparison.Ordinal);
            Assert.True(document.IsSessionStale);
            Assert.True(run.Problems.IsOutdated);

            run.ResetCommand.Execute(null);
            WaitUntil(() => run.State == RunState.Ready && run.LastBuild!.Image.Length == size + 1, "the rebuilt program");
            Assert.False(document.IsSessionStale);
            Assert.False(run.Problems.IsOutdated);
        });
    }

    [Fact]
    public void A_missing_assembler_fails_the_build_and_offers_Settings()
    {
        WithRun(Hello, (run, _) =>
        {
            run.BuildCommand.Execute(null);
            WaitUntil(() => run.State == RunState.BuildFailed, "Build failed");

            Assert.Equal(BuildOutcome.AssemblerMissing, run.LastBuild!.Outcome);
            Assert.True(run.Problems.OffersSettings);
            Assert.Equal(Strings.StatusAssemblerMissing, run.StatusText);
            Assert.NotEmpty(run.Problems.Items);
            Assert.StartsWith("▲ NASM", run.AssemblerText, StringComparison.Ordinal);
        }, configuredAssembler: @"C:\no\such\folder\nasm.exe");
    }

    [Fact]
    public void FR_067_a_build_can_be_cancelled_while_the_UI_stays_responsive()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = Workspace();
            workspace.New().Text = Hello;
            var assembler = new WaitingAssembler();
            using var run = new BuildRunViewModel(workspace, () => assembler, build => SessionFactory.Create(build, 1000));

            run.BuildCommand.Execute(null);
            WaitUntil(() => assembler.Started, "the build to start");
            Assert.Equal(RunState.Assembling, run.State);
            Assert.Equal(Strings.StatusBuilding, run.StatusText);
            Assert.Equal(Strings.ProblemsBuilding, run.Problems.EmptyText);
            Assert.False(run.RunCommand.CanExecute(null));
            Assert.False(run.BuildCommand.CanExecute(null));

            run.CancelBuildCommand.Execute(null);
            WaitUntil(() => run.State == RunState.BuildFailed, "the cancelled build");
            Assert.Equal(Strings.StatusBuildCancelled, run.StatusText);
            Assert.True(run.BuildCommand.CanExecute(null));
        });
    }

    [Fact]
    public void With_no_document_every_session_command_is_disabled()
    {
        StaTestThread.Run(() =>
        {
            using BuildRunViewModel run = Create(Workspace());
            Assert.Equal(RunState.Empty, run.State);
            Assert.Equal(string.Empty, run.BadgeText);
            Assert.All(
                new CommunityToolkit.Mvvm.Input.IRelayCommand[] { run.BuildCommand, run.RunCommand, run.PauseCommand, run.StopCommand, run.ResetCommand, run.StepIntoCommand, run.StepOverCommand, run.SendCtrlCCommand, run.CancelBuildCommand },
                c => Assert.False(c.CanExecute(null)));
        });
    }

    /// <summary>An assembler that runs until it is cancelled (to hold the Assembling state).</summary>
    internal sealed class WaitingAssembler : IAssembler
    {
        public volatile bool Started;

        public AssemblerInfo Check() => new("nasm.exe", AssemblerOrigin.Bundled, "3.02", AssemblerStatus.Ready, null);

        public async Task<BuildResult> BuildAsync(BuildRequest request, CancellationToken cancellationToken = default)
        {
            Started = true;
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            return new BuildResult(BuildOutcome.Cancelled, ReadOnlyMemory<byte>.Empty, string.Empty, false, null, TimeSpan.Zero, Check());
        }
    }
}
