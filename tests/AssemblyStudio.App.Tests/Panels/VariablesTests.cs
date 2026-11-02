using System.Globalization;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Panels.Machine;
using AssemblyStudio.App.Panels.Memory;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Commands;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.App.Tests.Panels;

/// <summary>
/// TASK-106: the Variables panel (UI-023, FR-097) over the real NASM and the real session: AC-065 with the CS401P VUID
/// program, values equal to the session's memory (NFR-018), change marks, value formats, the watch limit, the empty and
/// running states, "Show in Memory" and the tab shown for a loaded program.
/// </summary>
[Collection(nameof(AppProcessTests))] // pumps the dispatcher while waiting for snapshots
public sealed class VariablesTests
{
    // The user's CS401P assignment program (D-020), each item on its own 16-byte row.
    private const string VuidProgram = """
        [org 0x0100]
                jmp  start
        align 16, db 0
        vuid:    db 'BC123456789', 0
        align 16, db 0
        prefix:  times 2 db 0
                 db 0
        align 16, db 0
        lhs:     times 4 db 0
                 db 0
        align 16, db 0
        rhs:     times 5 db 0
                 db 0
        align 16, db 0
        lhsval:  dw 0
        rhsval:  dw 0
        align 16, db 0
        start:
                mov  si, vuid
                mov  di, prefix
                mov  cx, 2
        copy_prefix:
                mov  al, [si]
                mov  [di], al
                inc  si
                inc  di
                loop copy_prefix
                mov  di, lhs
                mov  cx, 4
        copy_lhs:
                mov  al, [si]
                mov  [di], al
                inc  si
                inc  di
                loop copy_lhs
                mov  di, rhs
                mov  cx, 5
        copy_rhs:
                mov  al, [si]
                mov  [di], al
                inc  si
                inc  di
                loop copy_rhs
                mov  si, lhs
                mov  cx, 4
                call ascii_to_num
                mov  [lhsval], ax
                mov  si, rhs
                mov  cx, 5
                call ascii_to_num
                mov  [rhsval], ax
                mov  ax, [lhsval]
                mov  bx, [rhsval]
                cmp  ax, 0
                je   zero_case
                cmp  bx, 0
                je   zero_case
                cmp  ax, bx
                jae  gcd_loop
                xchg ax, bx
        gcd_loop:
                xor  dx, dx
                div  bx
                mov  ax, bx
                mov  bx, dx
                cmp  bx, 0
                jne  gcd_loop
                mov  bx, ax
                jmp  done
        zero_case:
                mov  bx, 0xFFFF
        done:
                mov  ax, 0x4C00
                int  0x21
        ascii_to_num:
                xor  ax, ax
        .next:
                mov  bx, 10
                mul  bx
                xor  dx, dx
                mov  dl, [si]
                sub  dl, '0'
                add  ax, dx
                inc  si
                loop .next
                ret

        """;

    private static void WithRun(string text, Action<BuildRunViewModel> body)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            workspace.New().Text = text;
            using BuildRunViewModel run = BuildRunTests.Create(workspace);
            body(run);
        });
    }

    private static void BuildAndRunToEnd(BuildRunViewModel run)
    {
        run.BuildCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
        run.RunCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State == RunState.Terminated, "the end of the program");
        BuildRunTests.WaitUntil(() => run.Variables.Rows.All(r => r.Bytes is not null), "the variables' memory");
    }

    /// <summary>NFR-018: each row's bytes are the session's memory at its address.</summary>
    private static void AssertRowsEqualSessionMemory(BuildRunViewModel run)
    {
        IReadOnlyList<MemoryWindow> windows = run.LatestSnapshot!.Value.MemoryWindows!;
        foreach (VariableRow row in run.Variables.Rows)
        {
            MemoryWindow window = windows.First(w => w.Segment == SessionFactory.ProgramSegment && w.Offset <= row.Symbol.Offset
                && row.Symbol.Offset + row.Bytes!.Length <= w.Offset + w.Bytes.Count && w.Bytes.Count != MemoryViewModel.WindowLength);
            Assert.Equal(window.Bytes.Skip(row.Symbol.Offset - window.Offset).Take(row.Bytes!.Length), row.Bytes);
        }
    }

    [Fact]
    public void AC_065_the_VUID_program_shows_its_six_variables_with_their_split_values()
    {
        WithRun(VuidProgram, run =>
        {
            BuildAndRunToEnd(run);

            Assert.Equal(
                [
                    ("vuid", "0800:0110", "\"BC123456789\"", false),
                    ("prefix", "0800:0120", "\"BC\"", true),
                    ("lhs", "0800:0130", "\"1234\"", true),
                    ("rhs", "0800:0140", "\"56789\"", true),
                    ("lhsval", "0800:0150", "04D2h = 1234", true),
                    ("rhsval", "0800:0152", "DDD5h = 56789", true),
                ],
                run.Variables.Rows.Select(r => (r.Name, r.Address, r.Value, r.IsChanged)));
            AssertRowsEqualSessionMemory(run);
            Assert.Equal(CpuPanelState.Current, run.Variables.State);
            Assert.Equal("lhs at 0800:0130, text 1234, changed", run.Variables.Rows[2].AccessibleName);
        });
    }

    [Fact]
    public void Loaded_values_are_the_program_image_and_nothing_is_marked()
    {
        WithRun(VuidProgram, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Variables.Rows.All(r => r.Bytes is not null), "Ready with values");

            Assert.Equal(["\"BC123456789\"", "00 00 00", "00 00 00 00 00", "00 00 00 00 00 00", "0000h = 0", "0000h = 0"], run.Variables.Rows.Select(r => r.Value));
            Assert.DoesNotContain(run.Variables.Rows, r => r.IsChanged);
            AssertRowsEqualSessionMemory(run);
        });
    }

    [Fact]
    public void A_step_marks_only_the_variable_it_wrote()
    {
        const string program = "org 100h\n    jmp start\na: db 1\nb: dw 2\nstart:\n    mov byte [a], 5\n    mov word [b], 2\n    int 20h\n";
        WithRun(program, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Variables.Rows.All(r => r.Bytes is not null), "Ready with values");
            for (int i = 0; i < 2; i++)
            {
                ulong before = run.InstructionCount;
                run.StepIntoCommand.Execute(null); // jmp start, mov byte [a], 5
                BuildRunTests.WaitUntil(() => run.State == RunState.Paused && run.InstructionCount == before + 1, "a step");
            }

            BuildRunTests.WaitUntil(() => run.Variables.Rows[0].Value == "05h = 5", "a = 5");
            Assert.Equal([("a", true), ("b", false)], run.Variables.Rows.Select(r => (r.Name, r.IsChanged)));

            ulong count = run.InstructionCount;
            run.StepIntoCommand.Execute(null); // mov word [b], 2: the same value is written: not a change
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused && run.InstructionCount == count + 1, "a step");
            Assert.Equal([("a", false), ("b", false)], run.Variables.Rows.Select(r => (r.Name, r.IsChanged)));
        });
    }

    [Theory]
    [InlineData(new byte[] { 0x37 }, 1, NumberFormat.Hexadecimal, "byte", "37h = 55")]
    [InlineData(new byte[] { 0xFF }, 1, NumberFormat.SignedDecimal, "byte", "-1")]
    [InlineData(new byte[] { 0xD2, 0x04 }, 2, NumberFormat.Hexadecimal, "word", "04D2h = 1234")]
    [InlineData(new byte[] { 0xD2, 0x04 }, 2, NumberFormat.UnsignedDecimal, "word", "1234")]
    [InlineData(new byte[] { 0xD2, 0x04 }, 2, NumberFormat.Binary, "word", "0000 0100 1101 0010 = 1234")]
    [InlineData(new byte[] { 0x42, 0x43, 0x00 }, 3, NumberFormat.Hexadecimal, "text", "\"BC\"")]
    [InlineData(new byte[] { 0x41, 0x0A, 0x00 }, 3, NumberFormat.Hexadecimal, "bytes", "41 0A 00")]  // a control character: not text
    [InlineData(new byte[] { 0x00, 0x00, 0x00 }, 3, NumberFormat.Hexadecimal, "bytes", "00 00 00")]  // nothing written yet
    [InlineData(new byte[] { 0x41, 0x42, 0x43 }, 70, NumberFormat.Hexadecimal, "bytes", "41 42 43 …")] // more than shown
    public void FR_097_values_are_bytes_words_text_or_hex_bytes(byte[] bytes, int length, NumberFormat format, string kind, string text)
    {
        (string actualKind, string actualText) = VariablesViewModel.Describe(bytes, length, format);
        Assert.Equal((kind, text), (actualKind, actualText));
    }

    [Fact]
    public void The_number_format_applies_to_shown_values()
    {
        WithRun(VuidProgram, run =>
        {
            BuildAndRunToEnd(run);
            run.Cpu.CycleFormatCommand.Execute(null); // hex → unsigned
            Assert.Equal("1234", run.Variables.Rows[4].Value);
        });
    }

    [Fact]
    public void Variables_beyond_two_4_KiB_regions_say_they_are_not_shown()
    {
        const string program = "org 100h\n    jmp start\nfirst: db 'one', 0\n    times 5000 nop\nsecond: db 'two', 0\n    times 5000 nop\nthird: db 'three', 0\nstart:\n    int 20h\n";
        WithRun(program, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Variables.Rows.Take(2).All(r => r.Bytes is not null), "Ready with values");

            Assert.Equal([("first", "\"one\""), ("second", "\"two\""), ("third", Strings.VariableNotWatched)], run.Variables.Rows.Select(r => (r.Name, r.Value)));
            Assert.Null(run.Variables.Rows[2].Bytes);
        });
    }

    [Fact]
    public void Empty_states_before_a_build_after_a_failed_build_and_without_data_labels()
    {
        WithRun("org 100h\n    int 20h\n", run =>
        {
            Assert.Equal(Strings.EmptyVariables, run.Variables.EmptyText);
            Assert.False(run.Variables.HasValues);

            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Assert.Empty(run.Variables.Rows);
            Assert.Equal(Strings.EmptyVariablesNone, run.Variables.EmptyText);
            Assert.False(run.Variables.HasValues);

            run.BuiltDocument!.Text ="org 100h\n    bogus ax\n";
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.BuildFailed, "the failed build");
            Assert.Equal(Strings.EmptyMachineBuildFailed, run.Variables.EmptyText);
        });
    }

    [Fact]
    public void FR_057_while_running_the_values_are_not_current()
    {
        WithRun("org 100h\n    jmp start\ncount: dw 0\nstart:\n    inc word [count]\n    jmp start\n", run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Variables.Rows.All(r => r.Bytes is not null), "Ready with values");
            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Running && run.Variables.IsNotCurrent, "Running, values not current");
            Assert.True(run.Variables.HasValues);

            run.PauseCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "Paused");
            BuildRunTests.WaitUntil(() => run.Variables.Rows[0].IsChanged, "the count marked");
            Assert.False(run.Variables.IsNotCurrent);
            AssertRowsEqualSessionMemory(run);
        });
    }

    [Fact]
    public void UI_023_the_tab_lists_the_variables_with_screen_reader_names()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            using BuildRunViewModel run = BuildRunTests.Create(workspace);
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: run);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                window.Show();
                document.Text = VuidProgram;
                BuildAndRunToEnd(run);
                Assert.Equal(BottomTab.Variables, shell.BottomTab); // ended without printing anything
                window.UpdateLayout();

                var list = (System.Windows.Controls.ListBox)window.FindName("VariableList");
                Assert.True(list.IsVisible);
                Assert.Equal(6, list.Items.Count);
                var item = (System.Windows.Controls.ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(3);
                Assert.Equal("rhs at 0800:0140, text 56789, changed", System.Windows.Automation.AutomationProperties.GetName(item));
                ConsoleTests.Render(window, "variables-panel.png");
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    [Fact]
    public void Show_in_Memory_puts_the_address_in_the_memory_view_and_the_shell_shows_it()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            workspace.New().Text = VuidProgram;
            using BuildRunViewModel run = BuildRunTests.Create(workspace);
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: run);
            shell.UpdateWindowWidth(1000); // compact: memory is a bottom tab
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Variables.Rows.All(r => r.Bytes is not null), "Ready with values");
            Assert.Equal(BottomTab.Variables, shell.BottomTab); // a loaded program with data labels

            run.Variables.ShowInMemoryCommand.Execute(run.Variables.Rows[2]);

            Assert.Equal("0800:0130", run.Memory.AddressText);
            Assert.Equal(BottomTab.Memory, shell.BottomTab);
            BuildRunTests.WaitUntil(() => run.Memory.ShownAddress == "0800:0130" && run.Memory.Rows.Count == 16, "the memory view at lhs");
        });
    }
}
