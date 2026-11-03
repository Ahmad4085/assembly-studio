using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Commands;
using AssemblyStudio.App.Tests.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Panels;

/// <summary>
/// TASK-085 (FR-093, ui-spec §4): one screen-reader announcement per step, pause, halt and end, built from the real state
/// ("Paused at line 3. Changed: AX, IP."), raised through a polite live region; build and run states are announced too.
/// </summary>
[Collection(nameof(AppProcessTests))] // pumps the dispatcher while waiting for snapshots
public sealed class AnnouncementTests
{
    private const string Program =
        "org 100h\n" +
        "    mov ax, 1234h\n" +
        "    add al, 0CCh\n" +
        "    mov bl, 0\n" +
        "    div bl\n";

    private static void WithRun(string text, Action<BuildRunViewModel, List<string>> body)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            workspace.New().Text = text;
            using BuildRunViewModel run = BuildRunTests.Create(workspace);
            var heard = new List<string>();
            run.Announced += (_, t) => heard.Add(t);
            body(run, heard);
        });
    }

    private static void Step(BuildRunViewModel run, List<string> heard)
    {
        int before = heard.Count;
        run.StepIntoCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State != RunState.Stepping && heard.Count > before, "a step's announcement");
    }

    [Fact]
    public void DEF_026_a_refresh_at_the_same_pause_announces_nothing_again()
    {
        WithRun(Program, (run, heard) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && heard.Count >= 2, "Ready");

            run.Memory.ShowAddress(0x0800, 0x0200); // a new memory watch: the session publishes a refresh snapshot at the same pause
            BuildRunTests.WaitUntil(() => run.Memory.ShownAddress == "0800:0200", "the refresh");

            Assert.Equal([Strings.AnnounceBuilding, "Ready at line 2."], heard);
            Step(run, heard); // a step is still announced
            Assert.Equal("Paused at line 3. Changed: AX, IP.", heard[^1]);
        });
    }

    [Fact]
    public void FR_093_each_step_announces_the_line_and_the_changed_registers_and_flags()
    {
        WithRun(Program, (run, heard) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && heard.Count >= 2, "Ready");
            Assert.Equal([Strings.AnnounceBuilding, "Ready at line 2."], heard);

            Step(run, heard); // mov ax, 1234h
            Assert.Equal("Paused at line 3. Changed: AX, IP.", heard[^1]);
            Assert.Equal(heard[^1], run.Announcement);

            Step(run, heard); // add al, 0CCh: 34h + CCh = 00h, CF and ZF set
            Assert.StartsWith("Paused at line 4. Changed: AX, IP, FLAGS", heard[^1], StringComparison.Ordinal);
            Assert.Contains("ZF", heard[^1], StringComparison.Ordinal);
            Assert.Contains("CF", heard[^1], StringComparison.Ordinal);

            Step(run, heard); // mov bl, 0: BX was already 0
            Assert.Equal("Paused at line 5. Changed: IP.", heard[^1]);

            Step(run, heard); // div bl: divide error
            Assert.StartsWith("Halted: RTE001 Divide error", heard[^1], StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Running_waiting_and_the_end_are_announced_once_each()
    {
        WithRun(BuildRunTests.ReadSample("keyboard-input"), (run, heard) =>
        {
            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.WaitingForInput && heard.Contains(Strings.AnnounceWaiting), "waiting");
            Assert.Contains(Strings.AnnounceRunning, heard);
            Assert.Single(heard, Strings.AnnounceWaiting);

            run.Console.SendInput([.. AssemblyStudio.App.Panels.Console.ConsoleKeys.FromText("Ada"), 0x0D]); // Enter is a key, not text
            BuildRunTests.WaitUntil(() => run.State == RunState.WaitingForInput && heard.Count(h => h == Strings.AnnounceWaiting) == 2, "the second prompt");
            run.Console.SendInput(AssemblyStudio.App.Panels.Console.ConsoleKeys.FromText("y"));
            BuildRunTests.WaitUntil(() => run.State == RunState.Terminated && heard[^1] == "Program ended with exit code 0.", "the end");
        });
    }

    [Fact]
    public void The_announcement_region_is_a_polite_live_region_with_the_text()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: BuildRunTests.Create(workspace));
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = Program;
                window.Show();
                window.UpdateLayout();
                shell.BuildRun.BuildCommand.Execute(null);
                BuildRunTests.WaitUntil(() => shell.BuildRun.State == RunState.Ready && shell.BuildRun.Announcement.StartsWith("Ready", StringComparison.Ordinal), "Ready");
                window.UpdateLayout();

                var region = (TextBlock)window.FindName("AnnouncementRegion");
                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(region));
                Assert.Equal("Ready at line 2.", region.Text);
                Assert.True(region.IsVisible); // in the UI Automation tree (transparent, 1 x 1)
                Assert.Equal(AutomationLiveSetting.Off, AutomationProperties.GetLiveSetting((TextBlock)window.FindName("StateBadge"))); // no double announcement
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }
}
