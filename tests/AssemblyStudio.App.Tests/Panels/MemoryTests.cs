using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
/// TASK-082: the memory and stack panels (UI-008, UI-009) over the real NASM and the real session: AC-026, the default
/// program view, address entry, change marking from the write journal (kept when the address changes at the same pause),
/// the shown bytes equal the engine's (NFR-018), and the empty, running and stopped states.
/// </summary>
[Collection(nameof(AppProcessTests))] // pumps the dispatcher while waiting for snapshots
public sealed class MemoryTests
{
    // AC-026: paused before push ax with AX=1234h and SP=FFFEh.
    private const string PushProgram =
        "org 100h\n" +
        "    mov ax, 1234h\n" +
        "    push ax\n" +
        "    int 20h\n";

    private static readonly CpuState Cpu = new(0x1111, 0x2222, 0, 0, 0xFFF0, 0, 0x0040, 0, 0x0800, 0x0900, 0x0A00, 0x0B00, 0x0100, 0x0002);

    [Theory]
    [InlineData("0800:0100", 0x0800, 0x0100)]
    [InlineData("800h:100h", 0x0800, 0x0100)]
    [InlineData("0x800:0x1A", 0x0800, 0x001A)]
    [InlineData("ss:sp", 0x0B00, 0xFFF0)]
    [InlineData("DS:SI", 0x0900, 0x0040)]
    [InlineData("100", 0x0900, 0x0100)] // an offset alone uses DS
    [InlineData("es:bx", 0x0A00, 0x2222)]
    public void FR_054_addresses_are_hex_or_registers(string text, int segment, int offset)
    {
        Assert.True(MemoryAddress.TryParse(text, Cpu, out ushort s, out ushort o));
        Assert.Equal(((ushort)segment, (ushort)offset), (s, o));
    }

    [Theory]
    [InlineData("")]
    [InlineData("zz")]
    [InlineData("1:2:3")]
    [InlineData("12345:0")]
    [InlineData("ax:0")]     // not a segment register
    [InlineData("cs:ds")]    // not an offset register
    public void Other_text_is_not_an_address(string text) => Assert.False(MemoryAddress.TryParse(text, Cpu, out _, out _));

    [Fact]
    public void Registers_need_a_paused_CPU() => Assert.False(MemoryAddress.TryParse("ds:si", null, out _, out _));

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

    private static void Step(BuildRunViewModel run)
    {
        ulong before = run.InstructionCount;
        run.StepIntoCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State == RunState.Paused && run.InstructionCount == before + 1, "a step");
    }

    /// <summary>NFR-018: the shown rows are the snapshot's window, byte for byte.</summary>
    private static void AssertRowsEqualSnapshot(BuildRunViewModel run, ushort segment, ushort offset)
    {
        MemoryWindow window = run.LatestSnapshot!.Value.MemoryWindows!.Single(w => w.Segment == segment && w.Offset == offset);
        Assert.Equal(MemoryViewModel.WindowLength, window.Bytes.Count);
        Assert.Equal(window.Bytes.Select(b => b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)), run.Memory.Rows.SelectMany(r => r.Bytes).Select(b => b.Text));
        Assert.Equal(16, run.Memory.Rows.Count);
        Assert.Equal(MemoryAddress.Format(segment, (ushort)(offset + 16)), run.Memory.Rows[1].Address);
    }

    [Fact]
    public void DEF_025_a_snapshot_without_the_memory_window_shows_nothing_instead_of_throwing()
    {
        WithRun(PushProgram, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Memory.Rows.Count > 0, "the program bytes");
            MachineSnapshot snapshot = run.LatestSnapshot!.Value;

            run.Memory.Show(snapshot with { MemoryWindows = [] }, null, newPause: false, baseline: false); // MemoryWindow is a struct

            Assert.Empty(run.Memory.Rows); // nothing invented for an address without a window
            run.Memory.Show(snapshot, null, newPause: false, baseline: false);
            AssertRowsEqualSnapshot(run, 0x0800, 0x0100); // and shown again once the window is there
        });
    }

    [Fact]
    public void FR_054_the_memory_view_starts_at_the_program_image()
    {
        WithRun(PushProgram, run =>
        {
            Assert.Equal(CpuPanelState.Empty, run.Memory.State);
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Memory.Rows.Count > 0, "the program bytes");

            Assert.Equal("0800:0100", run.Memory.ShownAddress);
            Assert.Equal(["B8", "34", "12", "50", "CD", "20"], run.Memory.Rows[0].Bytes.Take(6).Select(b => b.Text)); // mov ax, 1234h; push ax; int 20h
            Assert.All(run.Memory.Rows.SelectMany(r => r.Bytes), b => Assert.False(b.IsChanged)); // a fresh load marks nothing
            Assert.StartsWith(".4.P. ", run.Memory.Rows[0].Ascii, StringComparison.Ordinal);
            AssertRowsEqualSnapshot(run, 0x0800, 0x0100);
            Assert.Equal(16, run.Memory.StackRows.Count);
            Assert.True(run.Memory.StackRows[0].IsTop);
            Assert.Equal("FFFE", run.Memory.StackRows[0].Offset);
        });
    }

    [Fact]
    public void AC_026_after_push_ax_SP_the_stack_top_and_the_memory_at_SS_FFFC_show_the_word_with_the_changes_marked()
    {
        WithRun(PushProgram, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Step(run); // mov ax, 1234h
            Assert.Equal("1234", run.Cpu.Registers.Single(r => r.Name == "AX").Text);
            Assert.Equal("FFFE", run.Cpu.Registers.Single(r => r.Name == "SP").Text);

            Step(run); // push ax
            Assert.Equal("FFFC", run.Cpu.Registers.Single(r => r.Name == "SP").Text);
            Assert.True(run.Cpu.Registers.Single(r => r.Name == "SP").IsChanged);
            StackRow top = run.Memory.StackRows[0];
            Assert.Equal(("FFFC", "1234", true, true), (top.Offset, top.Value, top.IsTop, top.IsChanged));
            Assert.Equal($"SS:FFFC 1234, {Strings.StackTop}, {Strings.CellChanged}", top.AccessibleName);
            Assert.False(run.Memory.StackRows[1].IsChanged); // FFFE wasn't written

            run.Memory.AddressText = "ss:sp";
            run.Memory.GoCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.Memory.ShownAddress == "0800:FFFC" && run.Memory.Rows.Count > 0, "the bytes at SS:FFFC");
            AssertRowsEqualSnapshot(run, 0x0800, 0xFFFC);
            Assert.Equal([("34", true), ("12", true), ("00", false)], run.Memory.Rows[0].Bytes.Take(3).Select(b => (b.Text, b.IsChanged)));
            Assert.True(run.Cpu.Registers.Single(r => r.Name == "SP").IsChanged); // the refresh for the new address kept the register marks
            Assert.Contains($"{Strings.CellChanged}: +0 34, +1 12", run.Memory.Rows[0].AccessibleName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void An_invalid_address_is_reported_and_nothing_is_shown_for_it()
    {
        WithRun(PushProgram, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready && run.Memory.Rows.Count > 0, "Ready");
            run.Memory.AddressText = "nowhere";
            run.Memory.GoCommand.Execute(null);
            Assert.Equal(Strings.MemoryAddressInvalid, run.Memory.AddressError);
            Assert.Equal("0800:0100", run.Memory.ShownAddress); // unchanged

            run.Memory.AddressText = "0000:0000"; // the interrupt vector table
            run.Memory.GoCommand.Execute(null);
            Assert.Null(run.Memory.AddressError);
            BuildRunTests.WaitUntil(() => run.Memory.ShownAddress == "0000:0000" && run.Memory.Rows.Count > 0, "the vector table");
            AssertRowsEqualSnapshot(run, 0x0000, 0x0000);
        });
    }

    [Fact]
    public void While_running_the_views_are_not_current_and_Stop_empties_them()
    {
        WithRun("org 100h\n    mov ax, 7\nagain:\n    push ax\n    pop ax\n    jmp again\n", run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Step(run); // mov ax, 7
            Step(run); // push ax: 0007 over 0000
            Assert.True(run.Memory.StackRows[0].IsChanged);

            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.Memory.State == CpuPanelState.NotCurrent, "not current");
            Assert.DoesNotContain(run.Memory.StackRows, r => r.IsChanged); // nothing marked while running

            run.PauseCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "Paused");
            Assert.Equal(CpuPanelState.Current, run.Memory.State);

            run.StopCommand.Execute(null);
            Assert.Equal(CpuPanelState.Empty, run.Memory.State);
            Assert.Empty(run.Memory.Rows);
            Assert.Empty(run.Memory.StackRows);
        });
    }

    [Fact]
    public void The_stack_words_follow_the_number_format()
    {
        WithRun(PushProgram, run =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Step(run);
            Step(run);
            run.Cpu.Format = NumberFormat.UnsignedDecimal;
            Assert.Equal("4660", run.Memory.StackRows[0].Value); // 1234h
            Assert.True(run.Memory.StackRows[0].IsChanged);
            run.Memory.CycleFormatCommand.Execute(null);
            Assert.Equal(NumberFormat.SignedDecimal, run.Cpu.Format);
        });
    }

    [Fact]
    public void The_panels_render_in_the_main_window_with_named_rows_and_the_format_key()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: BuildRunTests.Create(workspace));
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = PushProgram;
                window.Show();
                window.UpdateLayout();
                BuildRunViewModel run = shell.BuildRun;
                run.BuildCommand.Execute(null);
                BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
                Step(run);
                Step(run);
                window.UpdateLayout();

                var memory = (ContentControl)window.FindName("MemoryDocked");
                ListBox rows = Find<ListBox>(memory, "MemoryRows");
                Assert.True(rows.IsVisible);
                Assert.Equal(16, rows.Items.Count);
                var first = (ListBoxItem)rows.ItemContainerGenerator.ContainerFromIndex(0);
                Assert.Equal(run.Memory.Rows[0].AccessibleName, System.Windows.Automation.AutomationProperties.GetName(first));
                ListBox stack = Find<ListBox>((ContentControl)window.FindName("StackDocked"), "StackRows");
                var top = (ListBoxItem)stack.ItemContainerGenerator.ContainerFromIndex(0);
                Assert.Equal(run.Memory.StackRows[0].AccessibleName, System.Windows.Automation.AutomationProperties.GetName(top));
                Assert.Contains(Find<DockPanel>(memory, null).InputBindings.OfType<KeyBinding>(), b => b.Key == Key.N && b.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt));
                Render(window, "memory-stack.png");
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    private static T Find<T>(DependencyObject parent, string? name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match && (name is null || match.Name == name))
            {
                return match;
            }

            if (FindOrNull<T>(child, name) is { } found)
            {
                return found;
            }
        }

        throw new InvalidOperationException($"No {typeof(T).Name} named {name}.");
    }

    private static T? FindOrNull<T>(DependencyObject parent, string? name) where T : FrameworkElement
    {
        try
        {
            return Find<T>(parent, name);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
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
