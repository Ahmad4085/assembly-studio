using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Panels.Machine;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Commands;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.App.Tests.Panels;

/// <summary>
/// TASK-081: the registers and flags panels (UI-005, UI-006) over the real NASM and the real session: AC-025 (values,
/// changes with a non-color cue), AC-052 (formats), the view equals the engine's snapshot (NFR-018, AC-037 part), undefined
/// flags as "?", the not-current state while running, and the empty and build-failed states.
/// </summary>
[Collection(nameof(AppProcessTests))] // pumps the dispatcher while waiting for snapshots
public sealed class CpuTests
{
    // AC-025: paused before add al, bl with AL=7Fh and BL=01h; then an AND leaves AF undefined.
    private const string AddProgram =
        "org 100h\n" +
        "    mov al, 7Fh\n" +
        "    mov bl, 01h\n" +
        "    add al, bl\n" +
        "    and al, 0Fh\n" +
        "    int 20h\n";

    [Theory]
    [InlineData(0x7F80, 16, NumberFormat.Hexadecimal, "7F80")]
    [InlineData(0x7F80, 16, NumberFormat.UnsignedDecimal, "32640")]
    [InlineData(0x8000, 16, NumberFormat.SignedDecimal, "-32768")]
    [InlineData(0x7F80, 16, NumberFormat.Binary, "0111 1111 1000 0000")]
    [InlineData(0x0080, 8, NumberFormat.Hexadecimal, "80")]
    [InlineData(0x0080, 8, NumberFormat.UnsignedDecimal, "128")]
    [InlineData(0x0080, 8, NumberFormat.SignedDecimal, "-128")]
    [InlineData(0x0080, 8, NumberFormat.Binary, "1000 0000")]
    [InlineData(0x0005, 16, NumberFormat.SignedDecimal, "5")]
    public void FR_047_values_format_as_hex_unsigned_signed_and_binary(int value, int bits, NumberFormat format, string expected) =>
        Assert.Equal(expected, ValueFormat.Format((ushort)value, bits, format));

    [Fact]
    public void Ctrl_Alt_N_cycles_through_the_four_formats()
    {
        var cpu = new CpuViewModel();
        var seen = new List<NumberFormat>();
        for (int i = 0; i < 4; i++)
        {
            cpu.CycleFormatCommand.Execute(null);
            seen.Add(cpu.Format);
        }

        Assert.Equal([NumberFormat.UnsignedDecimal, NumberFormat.SignedDecimal, NumberFormat.Binary, NumberFormat.Hexadecimal], seen);
    }

    [Fact]
    public void D_017_AX_to_DX_rows_carry_their_8_bit_halves_and_the_other_registers_stand_alone()
    {
        var cpu = new CpuViewModel();
        Assert.Equal(["AX", "BX", "CX", "DX", "SI", "DI", "BP", "SP", "CS", "DS", "ES", "SS", "IP", "FLAGS"], cpu.Rows.Select(r => r.Main.Name));
        Assert.Equal([("AH", "AL"), ("BH", "BL"), ("CH", "CL"), ("DH", "DL")], cpu.Rows.Take(4).Select(r => (r.High!.Name, r.Low!.Name)));
        Assert.All(cpu.Rows.Skip(4), r => Assert.False(r.HasHalves));

        cpu.Show(new CpuState(0x000A, 0x0005, 0, 0, 0xFFFE, 0, 0, 0, 0x0800, 0x0800, 0x0800, 0x0800, 0x0100, 0x0002), FlagBit.None, null, baseline: true);
        Assert.Equal(("000A", "00", "0A"), (cpu.Rows[0].Main.Text, cpu.Rows[0].High!.Text, cpu.Rows[0].Low!.Text));
        Assert.Equal("BX 0005; BH 00; BL 05", cpu.Rows[1].AccessibleName);
    }

    [Fact]
    public void Before_a_program_runs_the_panels_show_no_values()
    {
        var cpu = new CpuViewModel();
        Assert.Equal(CpuPanelState.Empty, cpu.State);
        Assert.False(cpu.HasValues);
        Assert.Equal(Strings.EmptyMachine, cpu.EmptyText);
        Assert.All(cpu.Registers, r => Assert.Equal(string.Empty, r.Text)); // NFR-018: nothing made up
        cpu.Clear(buildFailed: true);
        Assert.Equal(Strings.EmptyMachineBuildFailed, cpu.EmptyText);
    }

    private static void WithRun(string text, Action<BuildRunViewModel, DocumentViewModel> body)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            DocumentViewModel document = workspace.New();
            document.Text = text;
            using BuildRunViewModel run = BuildRunTests.Create(workspace);
            body(run, document);
        });
    }

    private static void Step(BuildRunViewModel run)
    {
        ulong before = run.InstructionCount;
        run.StepIntoCommand.Execute(null);
        BuildRunTests.WaitUntil(() => run.State == RunState.Paused && run.InstructionCount == before + 1, "a step");
    }

    /// <summary>NFR-018: every shown value equals the engine's snapshot.</summary>
    private static void AssertEqualsSnapshot(BuildRunViewModel run)
    {
        MachineSnapshot snapshot = run.LatestSnapshot!.Value;
        CpuState c = snapshot.Cpu;
        ushort[] expected = [c.AX, c.BX, c.CX, c.DX, c.SI, c.DI, c.BP, c.SP, c.CS, c.DS, c.ES, c.SS, c.IP, c.Flags];
        Assert.Equal(expected, run.Cpu.Registers.Select(r => r.Value));
        Assert.Equal(
            [(byte)(c.AX >> 8), (byte)c.AX, (byte)(c.BX >> 8), (byte)c.BX, (byte)(c.CX >> 8), (byte)c.CX, (byte)(c.DX >> 8), (byte)c.DX],
            run.Cpu.Halves.Select(r => (byte)r.Value));
        foreach (FlagCell flag in run.Cpu.Flags)
        {
            string value = (snapshot.UndefinedFlags & flag.Bit) != 0 ? "?" : (c.Flags & (ushort)flag.Bit) != 0 ? "1" : "0";
            Assert.Equal(value, flag.Text);
        }
    }

    [Fact]
    public void AC_025_after_add_al_bl_AL_and_the_flags_show_the_engine_state_and_the_changes_are_marked()
    {
        WithRun(AddProgram, (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Assert.Equal(CpuPanelState.Current, run.Cpu.State);
            Assert.Empty(run.Cpu.ChangedNames); // a fresh load marks nothing
            AssertEqualsSnapshot(run);

            Step(run); // mov al, 7Fh
            Step(run); // mov bl, 01h
            RegisterCell al = run.Cpu.Halves.Single(r => r.Name == "AL");
            Assert.Equal("7F", al.Text);
            Assert.Equal("01", run.Cpu.Halves.Single(r => r.Name == "BL").Text);
            ushort flagsBefore = run.LatestSnapshot!.Value.Cpu.Flags;

            Step(run); // add al, bl
            AssertEqualsSnapshot(run);
            Assert.Equal("80", al.Text);
            Assert.True(al.IsChanged);
            Assert.True(run.Cpu.Registers.Single(r => r.Name == "AX").IsChanged);
            Assert.False(run.Cpu.Halves.Single(r => r.Name == "AH").IsChanged);
            Assert.False(run.Cpu.Registers.Single(r => r.Name == "BX").IsChanged);
            Assert.True(run.Cpu.Registers.Single(r => r.Name == "IP").IsChanged);
            Dictionary<string, FlagCell> flags = run.Cpu.Flags.ToDictionary(f => f.Bit.ToString());
            Assert.Equal(("1", "1", "0", "1", "0", "0"), (flags["OF"].Text, flags["SF"].Text, flags["ZF"].Text, flags["AF"].Text, flags["PF"].Text, flags["CF"].Text));
            foreach (FlagCell flag in run.Cpu.Flags) // marked exactly when the bit changed
            {
                bool changed = ((flagsBefore ^ run.LatestSnapshot!.Value.Cpu.Flags) & (ushort)flag.Bit) != 0;
                Assert.Equal(changed, flag.IsChanged);
            }

            Assert.True(flags["OF"].IsChanged);
            Assert.Equal($"{Strings.FlagOF} 1, {Strings.CellChanged}", flags["OF"].AccessibleName); // the non-color cue for screen readers
            Assert.Equal($"AL 80, {Strings.CellChanged}", al.AccessibleName);

            Step(run); // and al, 0Fh: AF undefined (DEF-008)
            AssertEqualsSnapshot(run);
            Assert.Equal("?", flags["AF"].Text);
            Assert.True(flags["AF"].IsUndefined);
            Assert.True(flags["AF"].IsChanged);
            Assert.Contains("and al", flags["AF"].Tooltip, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(Strings.FlagUndefined + ", " + Strings.CellChanged, flags["AF"].AccessibleName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AC_052_switching_formats_re_renders_every_register_with_the_same_value()
    {
        WithRun(AddProgram, (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Step(run);
            Step(run);
            Step(run);

            foreach (NumberFormat format in Enum.GetValues<NumberFormat>())
            {
                run.Cpu.Format = format;
                foreach (RegisterCell cell in run.Cpu.Registers.Concat(run.Cpu.Halves))
                {
                    Assert.Equal(ValueFormat.Format(cell.Value, cell.Bits, format), cell.Text);
                    int parsed = format switch
                    {
                        NumberFormat.Hexadecimal => int.Parse(cell.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        NumberFormat.Binary => Convert.ToInt32(cell.Text.Replace(" ", string.Empty, StringComparison.Ordinal), 2),
                        _ => int.Parse(cell.Text, CultureInfo.InvariantCulture) & (cell.Bits == 8 ? 0xFF : 0xFFFF),
                    };
                    Assert.Equal(cell.Value & (cell.Bits == 8 ? 0xFF : 0xFFFF), parsed); // the same value in every format
                }
            }

            run.Cpu.Format = NumberFormat.SignedDecimal;
            Assert.Equal("-128", run.Cpu.Halves.Single(r => r.Name == "AL").Text); // AL = 80h after the add
            AssertEqualsSnapshot(run);
        });
    }

    [Fact]
    public void FR_057_while_running_the_values_are_not_current_and_nothing_is_marked_and_Stop_clears_them()
    {
        WithRun("org 100h\nagain:\n    inc ax\n    jmp again\n", (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
            Step(run);
            Assert.NotEmpty(run.Cpu.ChangedNames);

            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.Cpu.State == CpuPanelState.NotCurrent, "not current");
            Assert.True(run.Cpu.IsNotCurrent);
            Assert.Empty(run.Cpu.ChangedNames); // nothing is highlighted while running

            run.PauseCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "Paused");
            Assert.Equal(CpuPanelState.Current, run.Cpu.State);
            AssertEqualsSnapshot(run); // refreshed on pause
            Assert.Contains("AX", run.Cpu.ChangedNames); // compared with the previous pause

            run.StopCommand.Execute(null);
            Assert.Equal(CpuPanelState.Empty, run.Cpu.State);
        });
    }

    [Fact]
    public void A_failed_build_shows_the_build_failed_state()
    {
        WithRun("org 100h\n    movx ax, 1\n", (run, _) =>
        {
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.BuildFailed, "Build failed");
            Assert.Equal(CpuPanelState.BuildFailed, run.Cpu.State);
            Assert.Equal(Strings.EmptyMachineBuildFailed, run.Cpu.EmptyText);
        });
    }

    [Fact]
    public void The_panels_in_the_main_window_name_each_cell_for_screen_readers_and_render_for_review()
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: BuildRunTests.Create(workspace));
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                document.Text = AddProgram;
                window.Show();
                window.UpdateLayout();
                Assert.True(((FrameworkElement)window.FindName("RegistersEmpty")).IsVisible);

                BuildRunViewModel run = shell.BuildRun;
                run.BuildCommand.Execute(null);
                BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready");
                Step(run);
                Step(run);
                Step(run);
                window.UpdateLayout();

                var registers = (ListBox)window.FindName("RegisterList");
                var flags = (ListBox)window.FindName("FlagList");
                Assert.True(registers.IsVisible);
                Assert.Equal(14, registers.Items.Count);
                Assert.Equal(9, flags.Items.Count);
                var ax = (ListBoxItem)registers.ItemContainerGenerator.ContainerFromIndex(0); // "AX 0080, changed; AH 00; AL 80, changed" (D-017)
                Assert.Equal(run.Cpu.Rows[0].AccessibleName, System.Windows.Automation.AutomationProperties.GetName(ax));
                Assert.Equal($"AX 0080, {Strings.CellChanged}; AH 00; AL 80, {Strings.CellChanged}", run.Cpu.Rows[0].AccessibleName);
                var of = (ListBoxItem)flags.ItemContainerGenerator.ContainerFromIndex(0);
                Assert.Equal(run.Cpu.Flags[0].AccessibleName, System.Windows.Automation.AutomationProperties.GetName(of));

                // Ctrl+Alt+N in the machine region
                var machine = (FrameworkElement)window.FindName("MachineRegion");
                KeyBinding cycle = machine.InputBindings.OfType<KeyBinding>().Single(b => b.Key == Key.N);
                cycle.Command.Execute(null);
                Assert.Equal(NumberFormat.UnsignedDecimal, run.Cpu.Format);
                cycle.Command.Execute(null);
                cycle.Command.Execute(null);
                cycle.Command.Execute(null);
                Assert.Equal(NumberFormat.Hexadecimal, run.Cpu.Format);
                window.UpdateLayout();
                Render(window, "registers-flags.png");
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
